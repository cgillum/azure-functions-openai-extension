﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Builders;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.SharedModels;

namespace WebJobs.Extensions.OpenAI.Agents;

public interface IAssistantSkillInvoker
{
    IList<FunctionDefinition>? GetFunctionsDefinitions();
    Task<string?> InvokeAsync(FunctionCall call);
}

public class AssistantSkillManager : IAssistantSkillInvoker
{
    record Skill(
        string Name,
        AssistantSkillTriggerAttribute Attribute,
        ParameterInfo Parameter,
        ITriggeredFunctionExecutor Executor);

    readonly IApplicationLifetime hostLifetime;
    readonly ILogger logger;

    readonly Dictionary<string, Skill> skills = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantSkillManager"/> class.
    /// </summary>
    /// <remarks>
    /// This constructor is called by the dependency management container in the Functions (WebJobs) runtime.
    /// </remarks>
    public AssistantSkillManager(IApplicationLifetime hostLifetime, ILoggerFactory loggerFactory)
    {
        this.hostLifetime = hostLifetime;
        this.logger = loggerFactory.CreateLogger<AssistantSkillManager>();
    }

    internal void RegisterSkill(
        string name,
        AssistantSkillTriggerAttribute attribute,
        ParameterInfo parameter,
        ITriggeredFunctionExecutor executor)
    {
        this.logger.LogInformation("Registering skill '{Name}'", name);
        this.skills.Add(name, new Skill(name, attribute, parameter, executor));
    }

    internal void UnregisterSkill(string name)
    {
        this.logger.LogInformation("Unregistering skill '{Name}'", name);
        this.skills.Remove(name);
    }

    IList<FunctionDefinition>? IAssistantSkillInvoker.GetFunctionsDefinitions()
    {
        if (this.skills.Count == 0)
        {
            return null;
        }

        List<FunctionDefinition> functions = new(capacity: this.skills.Count);
        foreach (Skill skill in this.skills.Values)
        {
            FunctionDefinition definition = new FunctionDefinitionBuilder(skill.Name, skill.Attribute.FunctionDescription)
                .AddParameter(skill.Parameter.Name, GetParameterDefinition(skill))
                .Validate()
                .Build();
            functions.Add(definition);
        }

        return functions;
    }

    static PropertyDefinition GetParameterDefinition(Skill skill)
    {
        ////// First, consider the declared type of the skill, if any
        ////if (!string.IsNullOrEmpty(skill.Attribute.ParameterType))
        ////{

        ////}

        // Next, try to infer from the .NET parameter type (only works with in-proc WebJobs)
        switch (skill.Parameter.ParameterType)
        {
            case Type type when type == typeof(string):
                return PropertyDefinition.DefineString(skill.Attribute.ParameterDescription);
            case Type type when type == typeof(int):
                return PropertyDefinition.DefineInteger(skill.Attribute.ParameterDescription);
            case Type type when type == typeof(bool):
                return PropertyDefinition.DefineBoolean(skill.Attribute.ParameterDescription);
            case Type type when type == typeof(float):
                return PropertyDefinition.DefineNumber(skill.Attribute.ParameterDescription);
            case Type type when type == typeof(double):
                return PropertyDefinition.DefineNumber(skill.Attribute.ParameterDescription);
            case Type type when type == typeof(decimal):
                return PropertyDefinition.DefineNumber(skill.Attribute.ParameterDescription);
            case Type _ when typeof(System.Collections.IEnumerable).IsAssignableFrom(skill.Parameter.ParameterType):
                return PropertyDefinition.DefineArray();
            default:
                return PropertyDefinition.DefineString(skill.Attribute.ParameterDescription);
        }
    }

    async Task<string?> IAssistantSkillInvoker.InvokeAsync(FunctionCall call)
    {
        if (call is null)
        {
            throw new ArgumentNullException(nameof(call));
        }

        if (call.Name is null)
        {
            throw new ArgumentException("The function call must have a name", nameof(call));
        }

        if (!this.skills.TryGetValue(call.Name, out Skill? skill))
        {
            throw new InvalidOperationException($"No skill registered with name '{call.Name}'");
        }

        // This call may throw if the Functions host is shutting down or if there is an internal error
        // in the Functions runtime. We don't currently try to handle these exceptions.
        object? skillOutput = null;
        FunctionResult result = await skill.Executor.TryExecuteAsync(
            new TriggeredFunctionData
            {
                TriggerValue = call.Arguments,
#pragma warning disable CS0618 // Approved for use by this extension
                InvokeHandler = async userCodeInvoker =>
                {
                    // We yield control to ensure this code is executed asynchronously relative to WebJobs.
                    // This ensures WebJobs is able to correctly cancel the invocation in the case of a timeout.
                    await Task.Yield();

                    // Invoke the function and attempt to get the result.
                    this.logger.LogInformation("Invoking user-code function '{Name}'", call.Name);
                    Task invokeTask = userCodeInvoker.Invoke();
                    if (invokeTask is not Task<object> resultTask)
                    {
                        throw new InvalidOperationException(
                            "The WebJobs runtime returned a invocation task that does not support return values!");
                    }

                    skillOutput = await resultTask;
                }
#pragma warning restore CS0618
            },
            this.hostLifetime.ApplicationStopping);

        // If the function threw an exception, rethrow it here. This will cause the caller (e.g., the
        // chat bot entity) to receive an error response, which it should be prepared to catch and handle.
        if (result.Exception is not null)
        {
            ExceptionDispatchInfo.Throw(result.Exception);
        }

        if (skillOutput is null)
        {
            return null;
        }

        // Convert the output to JSON
        string jsonResult = JsonConvert.SerializeObject(skillOutput);
        this.logger.LogInformation(
            "Returning output of user-code function '{Name}' as JSON: {Json}", call.Name, jsonResult);
        return jsonResult;
    }
}