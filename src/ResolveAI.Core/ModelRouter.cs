namespace ResolveAI.Core;

using System;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>
/// Manages dual-mode model connectivity and routing between Azure OpenAI and local Ollama.
/// Fulfills FR-002/003.
/// </summary>
public static class ModelRouter
{
    public enum ExecutionMode
    {
        CloudAzure,
        LocalOllama
    }

    /// <summary>
    /// Creates and returns an IChatClient configured for the specified execution mode.
    /// </summary>
    public static IChatClient CreateClient(ExecutionMode mode)
    {
        if (mode == ExecutionMode.CloudAzure)
        {
            string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                                  ?? "https://ai-resolveai-certification-lab.openai.azure.com/";
            string deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT")
                                  ?? "resolveai-phi4-mini";

            var credential = new DefaultAzureCredential();
            var azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), credential);

            return azureClient.GetChatClient(deploymentName).AsIChatClient();
        }
        else
        {
            string localEndpoint = "http://localhost:11434/v1";
            string localModelName = "llama3.1";

            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(localEndpoint) };
            var localClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama-local"), clientOptions);

            return localClient.GetChatClient(localModelName).AsIChatClient();
        }
    }
}