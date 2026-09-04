// ==============================================================================
// File Name: Program.cs
// Description: Entry point for the ResolveAI application. Configured to connect 
//              to a local Ollama inference server running on the developer's laptop 
//              (bypassing cloud rate limits and leveraging local zero-cost models).
// Purpose:     Fulfills offline/local prototyping requirements for rapid iteration.
// ==============================================================================

namespace ResolveAI.App;

using System;
using System.ClientModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>
/// Static entry point class for the ResolveAI console application.
/// Marked as static (Roslynator RCS1102) because it contains only static entry members.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main asynchronous entry point for the application runtime.
    /// </summary>
    /// <param name="args">Command-line arguments passed during startup.</param>
    public static async Task Main(string[] args)
    {
        // 1. Initialize overall execution stopwatch.
        var totalStopwatch = Stopwatch.StartNew();

        Console.WriteLine("=== ResolveAI: Local Ollama Prototyping Mode ===");

        try
        {
            // 2. Define local Ollama endpoint and model name.
            // WHY: Ollama exposes an OpenAI-compatible API at port 11434 by default.
            string localEndpoint = "http://localhost:11434/v1";
            string localModelName = "phi3";

            Console.WriteLine($"[Trace] Connecting to local Ollama endpoint: {localEndpoint}");
            Console.WriteLine($"[Trace] Target local model: {localModelName}");

            // 3. Initialize the OpenAIClient with local endpoint and dummy credentials.
            // WHY: Local execution does not require Entra ID or cloud API keys, but the 
            // SDK requires an ApiKeyCredential instance (e.g., "ollama") to satisfy client wiring.
            var clientOptions = new OpenAIClientOptions
            {
                Endpoint = new Uri(localEndpoint)
            };

            var localClient = new OpenAIClient(new ApiKeyCredential("ollama-local"), clientOptions);

            // 4. Wrap the local client in Microsoft.Extensions.AI's IChatClient abstraction.
            // WHY: Because IChatClient is provider-agnostic, our code interacts with local 
            // models using the exact same interface used for Azure OpenAI.
            IChatClient chatClient = localClient.GetChatClient(localModelName).AsIChatClient();

            // 5. Execute Streaming Inference against the local model
            var inferenceStopwatch = Stopwatch.StartNew();
            Console.WriteLine("\n[Trace] Sending streaming prompt to local Ollama model...\n");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[Local Model Response]: ");

            long timeToFirstTokenMs = 0;
            bool isFirstToken = true;

            // Stream chunks directly from your laptop's CPU/GPU with zero rate limits
            await foreach (var update in chatClient.GetStreamingResponseAsync("Hello local Ollama! Confirm you are running offline and ready for AI-103."))
            {
                if (isFirstToken && !string.IsNullOrEmpty(update.Text))
                {
                    timeToFirstTokenMs = inferenceStopwatch.ElapsedMilliseconds;
                    isFirstToken = false;
                }

                Console.Write(update.Text);
            }

            inferenceStopwatch.Stop();
            Console.ResetColor();
            Console.WriteLine("\n");

            totalStopwatch.Stop();

            // 6. Output local performance metrics
            Console.WriteLine("--------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[Local Execution Observability Report]");
            Console.WriteLine($" - Time to 1st Token:  {timeToFirstTokenMs} ms");
            Console.WriteLine($" - Total Generation:   {inferenceStopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($" - Total Execution:    {totalStopwatch.ElapsedMilliseconds} ms");
            Console.ResetColor();
            Console.WriteLine("----------------------------------------------------");
        }
        catch (Exception ex)
        {
            totalStopwatch.Stop();
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[Error connecting to local Ollama]: {ex.Message}");
            Console.ResetColor();
            Console.WriteLine("Hint: Ensure Ollama is running in the background (run 'ollama serve' if needed).");
        }

        Console.WriteLine("\n=== Local Ollama Test Complete ===");
    }
}




//// ==============================================================================
//// File Name: Program.cs
//// Description: Entry point for the ResolveAI application. Implements response 
////              streaming via IChatClient.GetStreamingResponseAsync to optimize 
////              perceived latency (Time to First Token / TTFT) and provide 
////              incremental telemetry.
//// Purpose:     Fulfills AI-103 objectives FR-002 (Foundry Connectivity), 
////              FR-022 (Observability), and performance optimization principles.
//// ==============================================================================

//namespace ResolveAI.App;

//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Azure.AI.OpenAI;
//using Azure.Identity;
//using Microsoft.Extensions.AI;

///// <summary>
///// Static entry point class for the ResolveAI console application.
///// Marked as static (Roslynator RCS1102) because it contains only static entry members.
///// </summary>
//public static class Program
//{
//    /// <summary>
//    /// Main asynchronous entry point for the application runtime.
//    /// </summary>
//    /// <param name="args">Command-line arguments passed during startup.</param>
//    public static async Task Main(string[] args)
//    {
//        // 1. Initialize overall execution stopwatch.
//        var totalStopwatch = Stopwatch.StartNew();

//        Console.WriteLine("=== ResolveAI: Day 1 - Response Streaming & Latency Optimization ===");

//        try
//        {
//            // 2. Configure endpoint and model deployment parameters.
//            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
//                              ?? "https://ai-resolveai-certification-lab.openai.azure.com/";
//            string deploymentName = "resolveai-phi4-mini";

//            Console.WriteLine($"[Trace] Connecting to Azure OpenAI endpoint: {endpoint}");

//            // 3. Establish keyless authentication via DefaultAzureCredential (NFR-001 / FR-023).
//            var authStopwatch = Stopwatch.StartNew();
//            var credential = new DefaultAzureCredential();
//            authStopwatch.Stop();

//            // 4. Initialize client wrappers via Microsoft.Extensions.AI abstractions.
//            var initStopwatch = Stopwatch.StartNew();
//            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
//            IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();
//            initStopwatch.Stop();

//            // 5. Execute Streaming Inference (Optimizing Time to First Token)
//            // WHY: GetStreamingResponseAsync returns an IAsyncEnumerable<ChatResponseUpdate>,
//            // allowing us to process and print chunks as soon as they arrive from the server.
//            var inferenceStopwatch = Stopwatch.StartNew();
//            Console.WriteLine("\n[Trace] Sending streaming prompt to model...\n");

//            Console.ForegroundColor = ConsoleColor.Green;
//            Console.Write("[Model Response Stream]: ");

//            long timeToFirstTokenMs = 0;
//            bool isFirstToken = true;

//            // Use await foreach to consume chunks asynchronously as they stream over the network
//            await foreach (var update in chatClient.GetStreamingResponseAsync("Hello from ResolveAI! Confirm you are online and ready for AI-103 with streaming."))
//            {
//                // Capture the exact millisecond timestamp when the very first token arrives
//                if (isFirstToken && !string.IsNullOrEmpty(update.Text))
//                {
//                    timeToFirstTokenMs = inferenceStopwatch.ElapsedMilliseconds;
//                    isFirstToken = false;
//                }

//                // Write each text fragment immediately to the console without waiting for completion
//                Console.Write(update.Text);
//            }

//            inferenceStopwatch.Stop();
//            Console.ResetColor();
//            Console.WriteLine("\n");

//            // Stop total timer
//            totalStopwatch.Stop();

//            // 6. Output advanced observability metrics (FR-022) including TTFT
//            Console.WriteLine("--------------------------------------------------");
//            Console.ForegroundColor = ConsoleColor.Cyan;
//            Console.WriteLine($"[Streaming Observability Report]");
//            Console.WriteLine($" - Auth Latency:       {authStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Client Setup:       {initStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Time to 1st Token:  {timeToFirstTokenMs} ms (Perceived Latency)");
//            Console.WriteLine($" - Total Generation:   {inferenceStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Total Execution:    {totalStopwatch.ElapsedMilliseconds} ms");
//            Console.ResetColor();
//            Console.WriteLine("----------------------------------------------------");
//        }
//        catch (Exception ex)
//        {
//            totalStopwatch.Stop();
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine($"\n[Error during execution]: {ex.Message}");
//            Console.ResetColor();
//        }

//        Console.WriteLine("\n=== Day 1 Streaming Test Complete ===");
//    }
//}


//using Azure.Identity;
//using OpenAI;
//using OpenAI.Chat;
//using System.ClientModel.Primitives;

//#pragma warning disable OPENAI001

//const string deploymentName = "resolveai-phi4-mini";
//const string endpoint = "https://ai-resolveai-certification-lab.services.ai.azure.com/openai/v1";

//BearerTokenPolicy tokenPolicy = new(
//    new DefaultAzureCredential(),
//    "https://ai.azure.com/.default");

//ChatClient client = new(
//    authenticationPolicy: tokenPolicy,
//    model: deploymentName,
//    options: new OpenAIClientOptions()
//    {
//        Endpoint = new($"{endpoint}"),
//    });


//ChatCompletion completion = client.CompleteChat(
//     [
//         new SystemChatMessage("You are a helpful assistant that talks like a pirate."),
//         new UserChatMessage("Hi, can you help me?"),
//         new AssistantChatMessage("Arrr! Of course, me hearty! What can I do for ye?"),
//         new UserChatMessage("What's the best way to train a parrot?"),
//     ]);

//Console.WriteLine($"Model={completion.Model}");
//foreach (ChatMessageContentPart contentPart in completion.Content)
//{
//    string message = contentPart.Text;
//    Console.WriteLine($"Chat Role: {completion.Role}");
//    Console.WriteLine("Message:");
//    Console.WriteLine(message);
//}



//// ==============================================================================
//// File Name: Program.cs
//// Description: Entry point for the ResolveAI application. Implements granular
////              phase timing using Stopwatch to measure authentication, client setup,
////              and model invocation latency (Fulfilling FR-022 Observability).
//// Purpose:     Fulfills AI-103 objectives FR-002 (Foundry Connectivity), 
////              FR-022 (Observability & Latency Tracking), and FR-023 (Security).
//// ==============================================================================

//namespace ResolveAI.App;

//using System;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using Azure.AI.OpenAI;
//using Azure.Identity;
//using Microsoft.Extensions.AI;

///// <summary>
///// Static entry point class for the ResolveAI console application.
///// Marked as static (Roslynator RCS1102) because it contains only static entry members.
///// </summary>
//public static class Program
//{
//    /// <summary>
//    /// Main asynchronous entry point for the application runtime.
//    /// </summary>
//    /// <param name="args">Command-line arguments passed during startup.</param>
//    public static async Task Main(string[] args)
//    {
//        // 1. Initialize overall application timer to track total execution duration.
//        // WHY: Essential for establishing end-to-end performance baselines.
//        var totalStopwatch = Stopwatch.StartNew();

//        Console.WriteLine("=== ResolveAI: Day 1 - Observability & Tracing Test ===");

//        try
//        {
//            // 2. Define configuration endpoints.
//            string endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
//                              ?? "https://ai-resolveai-certification-lab.openai.azure.com/";
//            string deploymentName = "resolveai-phi4-mini";

//            Console.WriteLine($"[Trace] Connecting to Azure OpenAI endpoint: {endpoint}");

//            // 3. Measure Authentication Phase Duration (DefaultAzureCredential / Entra ID token exchange)
//            // WHY: Token acquisition can introduce latency on cold starts. Measuring it separately 
//            // helps distinguish network/auth overhead from model generation latency.
//            var authStopwatch = Stopwatch.StartNew();
//            Console.WriteLine("[Trace] Acquiring token credential via DefaultAzureCredential...");

//            var credential = new DefaultAzureCredential();

//            authStopwatch.Stop();
//            Console.ForegroundColor = ConsoleColor.DarkYellow;
//            Console.WriteLine($"[Metrics] Authentication credential initialized in {authStopwatch.ElapsedMilliseconds} ms.");
//            Console.ResetColor();

//            // 4. Measure Client Initialization Phase
//            var initStopwatch = Stopwatch.StartNew();

//            var azureClient = new AzureOpenAIClient(new Uri(endpoint), credential);
//            IChatClient chatClient = azureClient.GetChatClient(deploymentName).AsIChatClient();

//            initStopwatch.Stop();
//            Console.ForegroundColor = ConsoleColor.DarkYellow;
//            Console.WriteLine($"[Metrics] AzureOpenAIClient and IChatClient wrapped in {initStopwatch.ElapsedMilliseconds} ms.");
//            Console.ResetColor();

//            // 5. Measure Model Round-Trip Latency (The actual inference call)
//            // WHY: Separating inference latency from setup time is critical for diagnosing performance bottlenecks.
//            var inferenceStopwatch = Stopwatch.StartNew();
//            Console.WriteLine("\n[Trace] Sending test prompt to model...");

//            var response = await chatClient.GetResponseAsync("Hello from ResolveAI! Confirm you are online and ready for AI-103.");

//            inferenceStopwatch.Stop();
//            Console.ForegroundColor = ConsoleColor.Green;
//            Console.WriteLine($"[Metrics] Model inference round-trip completed in {inferenceStopwatch.ElapsedMilliseconds} ms.");
//            Console.ResetColor();

//            // 6. Output the successful model response content
//            Console.WriteLine("\n[Model Response Received]:");
//            Console.WriteLine(response.Text);

//            // Stop total execution timer
//            totalStopwatch.Stop();

//            // 7. Output summary performance telemetry (FR-022)
//            Console.WriteLine("\n--------------------------------------------------育");
//            Console.ForegroundColor = ConsoleColor.Cyan;
//            Console.WriteLine($"[Observability Summary Report]");
//            Console.WriteLine($" - Auth Latency:      {authStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Client Init Setup: {initStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Model Inference:   {inferenceStopwatch.ElapsedMilliseconds} ms");
//            Console.WriteLine($" - Total Execution:   {totalStopwatch.ElapsedMilliseconds} ms");
//            Console.ResetColor();
//            Console.WriteLine("----------------------------------------------------");
//        }
//        catch (Exception ex)
//        {
//            totalStopwatch.Stop();
//            Console.ForegroundColor = ConsoleColor.Red;
//            Console.WriteLine($"\n[Error during execution after {totalStopwatch.ElapsedMilliseconds} ms]: {ex.Message}");
//            Console.ResetColor();
//        }

//        Console.WriteLine("\n=== Day 1 Tracing & Observability Test Complete ===");
//    }
//}