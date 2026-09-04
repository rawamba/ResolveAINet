using System;
using System.Collections.Generic;
using System.Text;

namespace ResolveAI.Core;

/// <summary>
/// Captures performance telemetry metrics for chat completions and streaming.
/// </summary>
public record StreamingMetrics(
    long TimeToFirstTokenMs,
    long TotalGenerationTimeMs,
    int TotalCharactersGenerated
);
