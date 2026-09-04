namespace ResolveAI.App;

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using ResolveAI.Core;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== ResolveAI: Dual-Mode Model Connectivity & Routing (FR-002/003) ===");

        // Determine mode from command line or environment variable (default to Local for offline safety)
        var mode = ModelRouter.ExecutionMode.LocalOllama;

        if (args.Length > 0 && args[0].Equals("--cloud", StringComparison.OrdinalIgnoreCase))
        {
            mode = ModelRouter.ExecutionMode.CloudAzure;
        }

        Console.WriteLine($"[Active Routing Mode]: {mode}");

        try
        {
            // Instantiate the unified IChatClient via the ModelRouter abstraction
            IChatClient chatClient = ModelRouter.CreateClient(mode);

            string prompt = "State your active execution mode and confirm dual-mode routing connectivity.";
            Console.WriteLine($"\n[Prompt]: \"{prompt}\"\n");

            Console.WriteLine("[Trace] Sending request through router...");
            var response = await chatClient.GetResponseAsync(prompt);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n[Model Response]:");
            Console.WriteLine(response.Text);
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Routing Error]: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n=== Dual-Mode Routing Test Complete ===");
    }
}