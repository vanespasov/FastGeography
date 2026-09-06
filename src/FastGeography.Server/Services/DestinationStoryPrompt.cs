namespace FastGeography.Server.Services;

using FastGeography.Shared;

/// <summary>
/// Closed list of story angles used to keep the same place feeling fresh.
/// </summary>
internal enum StoryAngle
{
    History,
    Food,
    Nature,
    SurprisingNumber,
    LocalNickname,
    GeographyShape
}

/// <summary>
/// System and user prompt templates for the destination story AI.
/// The story language always follows the UI selection (English or Macedonian).
/// </summary>
internal static class DestinationStoryPrompt
{
    public static IReadOnlyList<StoryAngle> AllAngles { get; } = Enum.GetValues<StoryAngle>();

    public static string ToKey(this StoryAngle angle) => angle switch
    {
        StoryAngle.History => "history",
        StoryAngle.Food => "food",
        StoryAngle.Nature => "nature",
        StoryAngle.SurprisingNumber => "surprising_number",
        StoryAngle.LocalNickname => "local_nickname",
        StoryAngle.GeographyShape => "geography_shape",
        _ => "history"
    };

    public static bool TryParseAngle(string? key, out StoryAngle angle)
    {
        foreach (var candidate in AllAngles)
        {
            if (string.Equals(candidate.ToKey(), key, StringComparison.OrdinalIgnoreCase))
            {
                angle = candidate;
                return true;
            }
        }

        angle = StoryAngle.History;
        return false;
    }

    public static string System(GameLanguage language) => language switch
    {
        GameLanguage.Mk =>
            "Ти си духовит и привлечен патеписец за географска игра. " +
            "Пишувај живи, фактички микро-приказни за вистински географски места. " +
            "Вклучи еден конкретен вистинит факт и лесен духовит засврт за да биде поучно и забавно. " +
            "Секоја приказна мора да биде целосно на македонски јазик, со кирилица. " +
            "Не пишувај на англиски. Не мешај јазици. " +
            "Должина: 40–70 зборови. " +
            "Врати само го пасусот — без наслов, без markdown, без коментар.",
        _ =>
            "You are a witty, engaging travel writer for a geography game. " +
            "Write vivid, factual micro-stories about real geographical places. " +
            "Include one concrete true fact and a light playful hook so the story is educational and fun. " +
            "Write the entire story in English. " +
            "Keep each story between 40 and 70 words. " +
            "Return only the story paragraph — no title, no markdown, no commentary."
    };

    public static string BuildUserPrompt(
        string placeName,
        LocationType placeType,
        string countryOrRegion,
        GameLanguage language) =>
        BuildUserPrompt(placeName, placeType, countryOrRegion, language, StoryAngle.History, []);

    public static string BuildUserPrompt(
        string placeName,
        LocationType placeType,
        string countryOrRegion,
        GameLanguage language,
        StoryAngle angle,
        IReadOnlyList<string> existingStories) => language switch
    {
        GameLanguage.Mk =>
            $"Напиши патничка микро-приказна од 40–70 зборови, исклучиво на македонски (кирилица), " +
            $"за {TypeMk(placeType)} \"{placeName}\" во / близу {countryOrRegion}. " +
            $"Фокусирај ја приказната на овој агол: {AngleMk(angle)}. " +
            "Вклучи еден конкретен вистинит факт и лесен духовит засврт. " +
            AvoidRepeatsMk(existingStories) +
            "Врати само го пасусот. Не користи англиски.",
        _ =>
            $"Write an English travel micro-story (40–70 words) about the {placeType} \"{placeName}\" " +
            $"located in / near {countryOrRegion}. " +
            $"Focus the story on this angle: {AngleEn(angle)}. " +
            "Include one concrete true fact and a light witty hook. " +
            AvoidRepeatsEn(existingStories) +
            "Return only the paragraph."
    };

    private static string AngleEn(StoryAngle angle) => angle switch
    {
        StoryAngle.History => "history",
        StoryAngle.Food => "food",
        StoryAngle.Nature => "nature",
        StoryAngle.SurprisingNumber => "a surprising number or statistic",
        StoryAngle.LocalNickname => "a local nickname or lore",
        StoryAngle.GeographyShape => "geography or shape of the place",
        _ => "history"
    };

    private static string AngleMk(StoryAngle angle) => angle switch
    {
        StoryAngle.History => "историја",
        StoryAngle.Food => "храна",
        StoryAngle.Nature => "природа",
        StoryAngle.SurprisingNumber => "изненадувачки број или статистика",
        StoryAngle.LocalNickname => "локален прекар или предание",
        StoryAngle.GeographyShape => "географија или облик на местото",
        _ => "историја"
    };

    private static string AvoidRepeatsEn(IReadOnlyList<string> existing)
    {
        if (existing.Count == 0)
            return string.Empty;

        return "Do not repeat these openings or facts: " +
               string.Join(" | ", existing) +
               " ";
    }

    private static string AvoidRepeatsMk(IReadOnlyList<string> existing)
    {
        if (existing.Count == 0)
            return string.Empty;

        return "Не ги повторувај овие воведи или факти: " +
               string.Join(" | ", existing) +
               " ";
    }

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
