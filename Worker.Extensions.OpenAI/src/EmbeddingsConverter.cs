// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenAI.Interfaces;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using Microsoft.Azure.Functions.Worker.Converters;
using Microsoft.Azure.Functions.Worker.Core;
using Microsoft.Azure.Functions.Worker.Extensions;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Microsoft.Azure.Functions.Worker.Extensions.OpenAI;

class EmbeddingsConverter : IInputConverter
{
    readonly IOpenAIServiceProvider serviceProvider;
    readonly ILogger logger;

    public EmbeddingsConverter(IOpenAIServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = loggerFactory?.CreateLogger<EmbeddingsConverter>() ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public async ValueTask<ConversionResult> ConvertAsync(ConverterContext context)
    {
        return context?.Source switch
        {
            ModelBindingData binding => await ConvertFromBindingDataAsync(context, binding),
            _ => ConversionResult.Unhandled(),
        };
    }

    Task<EmbeddingsContext> IAsyncConverter<EmbeddingsInputAttribute, EmbeddingsContext>.ConvertAsync(
        EmbeddingsInputAttribute attribute,
        CancellationToken cancellationToken)
    {
        return this.ConvertCoreAsync(attribute, cancellationToken);
    }

    async Task<string> IAsyncConverter<EmbeddingsInputAttribute, string>.ConvertAsync(
        EmbeddingsInputAttribute input,
        CancellationToken cancellationToken)
    {
        EmbeddingsContext response = await this.ConvertCoreAsync(input, cancellationToken);
        return JsonSerializer.Serialize(response);
    }

    async Task<EmbeddingsContext> ConvertCoreAsync(
        EmbeddingsInputAttribute attribute,
        CancellationToken cancellationToken)
    {
        IOpenAIService service = this.serviceProvider.GetService(attribute.Model);

        EmbeddingCreateRequest request = attribute.BuildRequest();
        this.logger.LogInformation("Sending OpenAI embeddings request: {request}", request);
        EmbeddingCreateResponse response = await service.Embeddings.CreateEmbedding(
            request,
            cancellationToken);
        this.logger.LogInformation("Received OpenAI embeddings response: {response}", response);

        if (attribute.ThrowOnError && response.Error is not null)
        {
            throw new InvalidOperationException(
                $"OpenAI returned an error of type '{response.Error.Type}': {response.Error.Message}");
        }

        return new EmbeddingsContext(request, response);
    }
}
