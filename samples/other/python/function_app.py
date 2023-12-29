import json
import azure.functions as func

app = func.FunctionApp(http_auth_level=func.AuthLevel.FUNCTION)


@app.route(route="whois/{name}", methods=["GET"])
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="Who is {name}?", maxTokens="100")
def whois(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    return func.HttpResponse(response_json["Choices"][0]["Text"], status_code=200)


@app.route(route="generic_completion", methods=["POST"])
@app.generic_input_binding(arg_name="response", type="textCompletion", data_type=func.DataType.STRING, prompt="{Prompt}")
def generic_completion(req: func.HttpRequest, response: str) -> func.HttpResponse:
    response_json = json.loads(response)
    if not response_json["Successful"]:
        error = response_json["Error"] or { "MessageObject": "OpenAI returned an unspecified error" }
        return func.HttpResponse(json.dumps(error), status_code=500)
    return func.HttpResponse(response_json["Choices"][0]["Text"], status_code=200)
