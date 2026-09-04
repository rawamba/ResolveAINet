// ==============================================================================
// File Name: SupportTicket.cs
// Description: Strongly-typed record schema for structured AI ticket classification.
// Purpose:     Fulfills AI-103 requirement FR-030 (Structured Outputs / Triage).
// ==============================================================================

namespace ResolveAI.Core;

/// <summary>
/// Represents a structured triage classification for an incoming user support request.
/// </summary>
/// <param name="Category">The classified domain (e.g., Billing, Technical, Hardware, Account).</param>
/// <param name="UrgencyLevel">Evaluated urgency (Low, Medium, High, Critical).</param>
/// <param name="Summary">A concise one-sentence summary of the core issue.</param>
/// <param name="SuggestedAction">The recommended next step for support staff.</param>
public record SupportTicketClassification(
    string Category,
    string UrgencyLevel,
    string Summary,
    string SuggestedAction
);