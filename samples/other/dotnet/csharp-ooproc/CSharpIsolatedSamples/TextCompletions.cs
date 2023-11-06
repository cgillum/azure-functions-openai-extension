using System.Net;
using System.Text.Json.Serialization;
using System.Web;
using Azure;
using Functions.Worker.Extensions.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels.ResponseModels;

namespace CSharpIsolatedSamples;

/// <summary>
/// These samples show how to use the OpenAI Completions APIs. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/completion.
/// </summary>
public static class TextCompletions
{
    /// <summary>
    /// This sample demonstrates the "templating" pattern, where the function takes a parameter
    /// and embeds it into a text prompt, which is then sent to the OpenAI completions API.
    /// </summary>
    [Function(nameof(WhoIs))]
    public static string WhoIs(
        [HttpTrigger(AuthorizationLevel.Anonymous, Route = "whois/{name}")] HttpRequestData req,
        [TextCompletionInput("Who is {name}?", Model = "%AZURE_OPENAI_CHATGPT_DEPLOYMENT%")] CompletionCreateResponse response)
    {
        return response.Choices[0].Text;
    }


    /// <summary>
    /// This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
    /// response as the output.
    /// </summary>
    [Function(nameof(GenericCompletion))]
    public static HttpResponseData GenericCompletion(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext executionContext,
        [TextCompletionInput("{Prompt}", Model = "%AZURE_OPENAI_CHATGPT_DEPLOYMENT%")] CompletionCreateResponse completionCreateResponse,
        ILogger _logger)
    {
        if (!completionCreateResponse.Successful)
        {
            Error error = completionCreateResponse.Error ?? new Error() { MessageObject = "OpenAI returned an unspecified error" };
            _logger.LogError(error.Message);
            return req.CreateResponse(HttpStatusCode.BadRequest);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
        string completionText = completionCreateResponse.Choices[0].Text;
        //_logger.LogInformation(completionText);
        response.WriteString(completionText);

        return response;

    }

    public record RequestPayload(string Prompt);
    //* Use the following to validate Prompt payload in the request explicitly or to do more prompting
    //if (req.Body.Length > 0)
    //{
    //    RequestPayload? payload = await req.ReadFromJsonAsync<RequestPayload>();
    //    string prompt = payload?.Prompt ?? "";
    //} else
    //{
    //    _logger.LogError("Missing parameter Prompt must be set.");
    //    var errResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
    //    await errResponse.WriteStringAsync("Missing parameter Prompt must be set.");
    //    return errResponse;
    //}

}