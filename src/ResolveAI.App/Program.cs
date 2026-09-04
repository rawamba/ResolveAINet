// ==============================================================================
// File Name: Program.cs
// Description: Day 2 Tool Calling implementation. Registers a custom C# inventory 
//              lookup method as an AI tool using Microsoft.Extensions.AI and 
//              enables automatic function invocation middleware.
// Purpose:     Fulfills AI-103 objectives FR-031 (Custom Tool Function Registration).
// ==============================================================================

namespace ResolveAI.App;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>
/// Static entry point class for the ResolveAI console application.
/// </summary>
public static class Program
{
    /// <summary>
    /// Custom backend inventory and cluster status tool function.
    /// Annotated so the LLM understands when and how to invoke it.
    /// </summary>
    [Description("Checks the current deployment health, status, and inventory metrics for a given infrastructure cluster.")]
    public static string CheckSystemInventory(
        [Description("The precise name of the system component or cluster, e.g., 'East-US-Database' or 'Auth-Service'.")]
        string componentName)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n[Tool Executed by Agent] Querying backend inventory store for: '{componentName}'");
        Console.ResetColor();

        // Simulated backend lookup logic
        return componentName.ToLowerInvariant() switch
        {
            var name when name.Contains("east-us") => "Status: 503 Gateway Error. Root cause identified: Connection pool exhaustion. Active mitigation in progress.",
            var name when name.Contains("auth") => "Status: Fully Operational. Latency: 12ms. Capacity: 42% utilized.",
            _ => $"Status: Component '{componentName}' not found in active inventory registry."
        };
    }

    /// <summary>
    /// Main asynchronous entry point for the application runtime.
    /// </summary>
    /// <param name="args">Command-line arguments passed during startup.</param>
    public static async Task Main(string[] args)
    {
        var totalStopwatch = Stopwatch.StartNew();

        Console.WriteLine("=== ResolveAI: Day 2 - Custom Tool Calling (Inventory Lookup) ===");

        try
        {
            // Local Ollama instance setup
            string localEndpoint = "http://localhost:11434/v1";
            //string localModelName = "phi3";
            string localModelName = "llama3.1";

            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(localEndpoint) };
            var localClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama-local"), clientOptions);

            // CRITICAL: Wrap IChatClient with UseFunctionInvocation middleware to handle tool loops automatically
            IChatClient chatClient = localClient.GetChatClient(localModelName).AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation()
                .Build();

            // Convert our C# method into an AI-callable function definition via AIFunctionFactory
            AIFunction inventoryTool = AIFunctionFactory.Create(CheckSystemInventory);

            // Configure chat options and provide the registered tool array
            var chatOptions = new ChatOptions
            {
                Tools = [inventoryTool]
            };

            string userQuery = "Can you check the current health and status of our East-US-Database cluster?";
            Console.WriteLine($"\n[User Prompt]: \"{userQuery}\"\n");

            Console.WriteLine("[Trace] Sending prompt with tool definitions to local model...");

            // When executed, the model will recognize it needs data it doesn't possess,
            // issue a tool call request, trigger our C# method, and synthesize the final reply.
            var response = await chatClient.GetResponseAsync(userQuery, chatOptions);

            totalStopwatch.Stop();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[Final Agent Response]:");
            Console.WriteLine(response.Text);
            Console.ResetColor();

            Console.WriteLine("\n--------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Execution Metrics]");
            Console.WriteLine($" - Total Execution Time: {totalStopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
            Console.WriteLine("----------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error during execution]: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n=== Day 2 Tool Calling Test Complete ===");
    }
}