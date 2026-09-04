// ==============================================================================
// File Name: Program.cs
// Description: Day 4 Quota Throttling & Rate Limit Resiliency implementation. 
//              Attempts primary Azure OpenAI call and automatically falls back 
//              to local Ollama upon encountering rate limits or transient errors.
// Purpose:     Fulfills AI-103 requirement FR-024 (Quota Throttling & Resiliency).
// ==============================================================================

namespace ResolveAI.App;

using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>
/// Static entry point class for the ResolveAI console application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main asynchronous entry point for the application runtime.
    /// </summary>
    /// <param name="args">Command-line arguments passed during startup.</param>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== ResolveAI: Quota Throttling & Rate Limit Resiliency ===");

        string prompt = "Explain how resilience patterns protect enterprise cloud workflows from rate-limiting disruptions.";
        Console.WriteLine($"\n[Prompt]: \"{prompt}\"\n");

        IChatClient? activeClient = null;
        string activeProviderName = string.Empty;

        // 1. Attempt Primary Connection: Azure OpenAI (Cloud) with Keyless Auth
        try
        {
            Console.WriteLine("[Trace] Attempting connection to Primary Provider: Azure OpenAI...");
            string azureEndpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                                  ?? "https://ai-resolveai-certification-lab.openai.azure.com/";
            string deploymentName = "resolveai-phi4-mini";

            var credential = new DefaultAzureCredential();
            var azureClient = new AzureOpenAIClient(new Uri(azureEndpoint), credential);
            activeClient = azureClient.GetChatClient(deploymentName).AsIChatClient();
            activeProviderName = "Azure OpenAI (Primary Cloud)";

            // Test reachability or send request
            var response = await activeClient.GetResponseAsync(prompt);
            OutputResponse(activeProviderName, response.Text);
        }
        catch (Exception ex)
        {
            // 2. Fallback Mechanism triggered on HTTP 429 or connectivity failure
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"\n[Warning]: Primary cloud endpoint failed or was throttled: {ex.Message}");
            Console.WriteLine("[Trace] Initiating automated failover to Secondary Provider: Local Ollama...");
            Console.ResetColor();

            try
            {
                string localEndpoint = "http://localhost:11434/v1";
                string localModelName = "llama3.1";

                var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(localEndpoint) };
                var localClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama-local"), clientOptions);
                activeClient = localClient.GetChatClient(localModelName).AsIChatClient();
                activeProviderName = "Local Ollama / Llama 3.1 (Resiliency Fallback)";

                var fallbackResponse = await activeClient.GetResponseAsync(prompt);
                OutputResponse(activeProviderName, fallbackResponse.Text);
            }
            catch (Exception fallbackEx)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[Critical Error]: Both primary and fallback providers failed. {fallbackEx.Message}");
                Console.ResetColor();
            }
        }

        Console.WriteLine("\n=== Resiliency & Failover Test Complete ===");
    }

    private static void OutputResponse(string providerName, string? content)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[Success via Provider: {providerName}]");
        Console.WriteLine(content);
        Console.ResetColor();
    }
}