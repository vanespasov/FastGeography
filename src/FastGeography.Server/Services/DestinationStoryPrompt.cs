namespace FastGeography.Server.Services;

/// <summary>
/// System and user prompt templates for the destination story AI.
/// </summary>
internal static class DestinationStoryPrompt
{
    public const string System =
        "You are a witty, engaging travel writer. " +
        "Write vivid, factual micro-stories about real geographical places. " +
        "Keep each story between 40 and 70 words. " +
        "Return only the story paragraph — no title, no markdown, no commentary.";

    /// <summary>
    /// Fills the user-turn template for the given place details.
    /// </summary>
    /// <param name="placeName">Player answer (e.g. "Skopje").</param>
    /// <param name="placeType">Category in English (e.g. "City").</param>
    /// <param name="countryOrRegion">
    ///   Reverse-geocoded region if known; otherwise
    ///   "unknown; coordinates {lat},{lon}" so the model stays geographically grounded.
    /// </param>
    /// <param name="language">
    ///   "English" or "Macedonian" — the language the story should be written in.
    /// </param>
    public static string BuildUserPrompt(
        string placeName,
        string placeType,
        string countryOrRegion,
        string language) =>
        $"Write a {language} travel micro-story (40–70 words) about the {placeType} \"{placeName}\" " +
        $"located in / near {countryOrRegion}. " +
        "Return only the paragraph.";
}
