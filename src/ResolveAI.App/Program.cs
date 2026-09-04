// ==============================================================================
// File Name: Program.cs
// Description: Day 3 Multi-Turn State Manager implementation. Maintains 
//              conversation history across multiple conversational turns using 
//              Microsoft.Extensions.AI message collections.
// Purpose:     Fulfills AI-103 objectives for conversational state and agent memory.
// ==============================================================================

namespace ResolveAI.App;

using System;
using System.Collections.Generic;
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
    /// Main asynchronous entry point for the application runtime.
    /// </summary>
    /// <param name="args">Command-line arguments passed during startup.</param>
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== ResolveAI: Day 3 - Multi-Turn Conversation State Manager ===");

        try
        {
            // Local Ollama instance setup (using Llama 3.1 for full compatibility)
            string localEndpoint = "http://localhost:11434/v1";
            string localModelName = "llama3.1";

            var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(localEndpoint) };
            var localClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential("ollama-local"), clientOptions);
            IChatClient chatClient = localClient.GetChatClient(localModelName).AsIChatClient();

            // Initialize conversation history collection with a system prompt
            var conversationHistory = new List<ChatMessage>
            {
                new ChatMessage(ChatRole.System, "You are ResolveAI, an enterprise IT support assistant. Maintain context across turns and keep answers structured and professional.")
            };

            Console.WriteLine("\n[System] Multi-turn session initialized. Type 'exit' to quit.\n");

            while (true)
            {
                Console.Write("User > ");
                string? userInput = Console.ReadLine();

                // Exit condition
                if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                // 1. Append the new user message to the conversation history collection
                conversationHistory.Add(new ChatMessage(ChatRole.User, userInput));

                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Assistant > ");

                var responseStopwatch = Stopwatch.StartNew();
                string fullAssistantResponse = "";

                // 2. Stream responses while passing the cumulative conversation history
                await foreach (var update in chatClient.GetStreamingResponseAsync(conversationHistory))
                {
                    if (!string.IsNullOrEmpty(update.Text))
                    {
                        Console.Write(update.Text);
                        fullAssistantResponse += update.Text;
                    }
                }

                responseStopwatch.Stop();
                Console.ResetColor();
                Console.WriteLine($"\n  [Metrics: Generated in {responseStopwatch.ElapsedMilliseconds}ms over {conversationHistory.Count} history items]\n");

                // 3. Append the assistant's complete response back into history for future turn context
                conversationHistory.Add(new ChatMessage(ChatRole.Assistant, fullAssistantResponse));
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error during execution]: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\n=== Day 3 Multi-Turn Session Complete ===");
    }
}