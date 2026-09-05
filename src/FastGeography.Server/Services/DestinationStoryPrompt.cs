namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// System and user prompt templates for the destination story AI.
/// The story language always follows the UI selection (English or Macedonian).
/// </summary>
internal static class DestinationStoryPrompt
{
    public static string System(GameLanguage language) => language switch
    {
        GameLanguage.Mk =>
            "Ти си духовит и привлечен патеписец. " +
            "Пишувај живи, фактички микро-приказни за вистински географски места. " +
            "Секоја приказна мора да биде целосно на македонски јазик, со кирилица. " +
            "Не пишувај на англиски. Не мешај јазици. " +
            "Должина: 40–70 зборови. " +
            "Врати само го пасусот — без наслов, без markdown, без коментар.",
        _ =>
            "You are a witty, engaging travel writer. " +
            "Write vivid, factual micro-stories about real geographical places. " +
            "Write the entire story in English. " +
            "Keep each story between 40 and 70 words. " +
            "Return only the story paragraph — no title, no markdown, no commentary."
    };

    public static string BuildUserPrompt(
        string placeName,
        LocationType placeType,
        string countryOrRegion,
        GameLanguage language) => language switch
    {
        GameLanguage.Mk =>
            $"Напиши патничка микро-приказна од 40–70 зборови, исклучиво на македонски (кирилица), " +
            $"за {TypeMk(placeType)} \"{placeName}\" во / близу {countryOrRegion}. " +
            "Врати само го пасусот. Не користи англиски.",
        _ =>
            $"Write an English travel micro-story (40–70 words) about the {placeType} \"{placeName}\" " +
            $"located in / near {countryOrRegion}. " +
            "Return only the paragraph."
    };

    private static string TypeMk(LocationType type) => type switch
    {
        LocationType.City => "градот",
        LocationType.Village => "селото",
        LocationType.Country => "државата",
        LocationType.River => "реката",
        LocationType.Mountain => "планината",
        _ => "местото"
    };
}
