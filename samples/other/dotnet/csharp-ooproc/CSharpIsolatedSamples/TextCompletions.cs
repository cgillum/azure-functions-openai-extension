using Functions.Worker.Extensions.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OpenAI.ObjectModels.ResponseModels;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace CSharpIsolatedSamples;

/// <summary>
/// These samples show how to use the OpenAI Completions APIs. For more details on the Completions APIs, see
/// https://platform.openai.com/docs/guides/completion.
/// </summary>
public class TextCompletions
{
    readonly ILogger logger;

    public TextCompletions(ILogger<TextCompletions> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// This sample demonstrates the "templating" pattern, where the function takes a parameter
    /// and embeds it into a text prompt, which is then sent to the OpenAI completions API.
    /// </summary>
    [Function(nameof(WhoIs))]
    public IActionResult WhoIs(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "whois/{name}")] HttpRequest req,
        [TextCompletionInput("Who is {name}?")] CompletionCreateResponse response)
    {
        return new ContentResult
        {
            Content = response.Choices[0].Text.Trim(),
            ContentType = "text/plain; charset=utf-8",
        };
    }

    /// <summary>
    /// This sample takes a prompt as input, sends it directly to the OpenAI completions API, and results the 
    /// response as the output.
    /// </summary>
    [Function(nameof(GenericCompletion))]
    public IActionResult GenericCompletion(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest request,
        [FromBody] PromptPayload payload,
        [TextCompletionInput("{Prompt}", Model = "text-davinci-003")] CompletionCreateResponse response)
    {
        if (!response.Successful)
        {
            Error error = response.Error ?? new Error() { MessageObject = "OpenAI returned an unspecified error" };
            return new ObjectResult(error) { StatusCode = 500 };
        }

        this.logger.LogInformation("Prompt = {prompt}, Response = {response}", payload.Prompt, response);
        string text = response.Choices[0].Text.Trim();
        return new OkObjectResult(text);
    }

    public record PromptPayload(string Prompt);
}