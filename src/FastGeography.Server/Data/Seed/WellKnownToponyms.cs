namespace FastGeography.Server.Data.Seed;

using FastGeography.Shared;

/// <summary>
/// Curated bilingual (EN + MK) seed catalog of well-known toponyms.
///
/// Coverage goal: at least one real place per letter × category for
///   EN (A–Z) and MK (А–Ш) where a genuine place exists.
///
/// Helper <c>E()</c> creates up to two <see cref="ToponymSeedRecord"/> rows from a single
/// geographic point: one for the English display name and one for the Macedonian name.
/// Either argument may be null to produce only one row (useful when the two
/// spellings happen to start with different letters and you need them to cover
/// different letter slots).
///
/// Documented gaps — no entry is invented for these combinations:
///   EN  Country – W  (no universally recognised sovereign state)
///   EN  Country – X  (none)
///   MK  Country – Ж, Ѓ, Ѕ, Љ, Њ, Ќ  (no sovereign state with those initials)
///   MK  City    – Ѕ, Ќ  (extremely rare in any language's Macedonian form)
///   MK  Village – Ѕ, Ќ, Њ  (no commonly-known village)
///   MK  River   – Ѓ, Ѕ, Ќ, Љ, Њ, Ш, Џ  (few/no rivers with those initials)
///   MK  Mountain– Ж, З, Ѓ, Ѕ, Ќ, Љ, Њ, Џ  (few/no mountains with those initials)
/// </summary>
public static class WellKnownToponyms
{
    // ──────────────────────────────────────────────────────────────────────
    // Public catalog
    // ──────────────────────────────────────────────────────────────────────
    public static readonly IReadOnlyList<ToponymSeedRecord> All = Build();

    // ──────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────
    private static IReadOnlyList<ToponymSeedRecord> Build()
    {
        var list = new List<ToponymSeedRecord>(400);

        void Add(LocationType cat, double lat, double lon, string? en, string? mk)
        {
            if (en is not null) list.Add(new ToponymSeedRecord(en, cat, "en", lat, lon));
            if (mk is not null) list.Add(new ToponymSeedRecord(mk, cat, "mk", lat, lon));
        }

        const LocationType Cty = LocationType.City;
        const LocationType Vlg = LocationType.Village;
        const LocationType Ctr = LocationType.Country;
        const LocationType Riv = LocationType.River;
        const LocationType Mtn = LocationType.Mountain;

        // ── CITIES ──────────────────────────────────────────────────────
        // EN:A  MK:А
        Add(Cty,  52.37,   4.90, "Amsterdam",     "Амстердам");
        Add(Cty,  37.98,  23.73, "Athens",         "Атина");
        // EN:B  MK:Б
        Add(Cty,  52.52,  13.41, "Berlin",         "Берлин");
        Add(Cty,  47.50,  19.05, "Budapest",       "Будимпешта");
        Add(Cty, -34.61, -58.38, "Buenos Aires",   "Буенос Аирес");
        // EN:C  MK:Ч  (Chicago → Чикаго)
        Add(Cty,  41.88, -87.63, "Chicago",        "Чикаго");
        // EN:C  MK:К
        Add(Cty,  30.06,  31.24, "Cairo",          "Каиро");
        // EN:D  MK:Д
        Add(Cty,  53.34,  -6.27, "Dublin",         "Даблин");
        Add(Cty,  28.61,  77.21, "Delhi",          "Делхи");
        // EN:E  MK:Е
        Add(Cty,  55.95,  -3.19, "Edinburgh",      "Единбург");
        // EN:F  MK:Ф
        Add(Cty,  50.11,   8.68, "Frankfurt",      "Франкфурт");
        Add(Cty,  43.77,  11.25, "Florence",       "Фиренца");
        // EN:G  MK:Ж  (Geneva → Женева)
        Add(Cty,  46.20,   6.15, "Geneva",         "Женева");
        // EN:G  MK:Г
        Add(Cty,  41.80,  20.91, null,             "Гостивар");   // Macedonian city
        // EN:H  MK:Х
        Add(Cty,  60.17,  24.93, "Helsinki",       "Хелсинки");
        Add(Cty,  23.13, -82.38, "Havana",         "Хавана");
        // EN:I  MK:И
        Add(Cty,  41.01,  28.95, "Istanbul",       "Истанбул");
        // EN:J  MK:Ј
        Add(Cty, -26.20,  28.04, "Johannesburg",   "Јоханесбург");
        // EN:J  MK:Џ  (Jakarta → Џакарта)
        Add(Cty,   6.21, 106.85, "Jakarta",        "Џакарта");
        // EN:K  MK:К extra
        Add(Cty,  35.01, 135.77, "Kyoto",          "Кјото");
        // EN:L  MK:Л
        Add(Cty,  51.51,  -0.13, "London",         "Лондон");
        Add(Cty,  38.72,  -9.14, "Lisbon",         "Лисабон");
        // MK:Љ  (Ljubljana → Љубљана)
        Add(Cty,  46.05,  14.51, null,             "Љубљана");   // MK:Љ
        // EN:M  MK:М
        Add(Cty,  40.42,  -3.70, "Madrid",         "Мадрид");
        Add(Cty,  55.75,  37.62, "Moscow",         "Москва");
        // EN:N  MK:Н
        Add(Cty,  -1.29,  36.82, "Nairobi",        "Најроби");
        Add(Cty,  40.85,  14.27, "Naples",         "Напул");
        // EN:N  MK:Њ  (New York → Њујорк)
        Add(Cty,  40.71, -74.01, "New York",       "Њујорк");
        // EN:O  MK:О
        Add(Cty,  59.91,  10.75, "Oslo",           "Осло");
        Add(Cty,  45.42, -75.69, "Ottawa",         "Отава");
        // EN:P  MK:П
        Add(Cty,  48.86,   2.35, "Paris",          "Париз");
        Add(Cty,  50.07,  14.44, "Prague",         "Прага");
        // EN:Q  (Macedonian has no Q; Катар MK:К already covered)
        Add(Cty,  -0.23, -78.52, "Quito",          null);
        // EN:R  MK:Р
        Add(Cty,  41.90,  12.50, "Rome",           "Рим");
        Add(Cty,  64.15, -21.94, "Reykjavik",      "Рејкјавик");
        // EN:S  MK:С
        Add(Cty,  42.00,  21.43, "Skopje",         "Скопје");
        Add(Cty, -33.87, 151.21, "Sydney",         "Сиднеј");
        // EN:S  MK:Ш  (Shanghai → Шангај)
        Add(Cty,  31.23, 121.47, "Shanghai",       "Шангај");
        // EN:T  MK:Т
        Add(Cty,  35.69, 139.69, "Tokyo",          "Токио");
        Add(Cty,  41.33,  19.83, "Tirana",         "Тирана");
        // EN:U  MK:У
        Add(Cty,  52.09,   5.12, "Utrecht",        "Утрехт");
        Add(Cty,  47.91, 106.89, "Ulaanbaatar",    "Улан Батор");
        // EN:V  MK:В
        Add(Cty,  48.21,  16.37, "Vienna",         "Виена");
        Add(Cty,  49.25,-123.12, "Vancouver",      "Ванкувер");
        // EN:W  (Варшава starts with В, already covered; МК В done)
        Add(Cty,  52.23,  21.01, "Warsaw",         null);
        // EN:X
        Add(Cty,  34.27, 108.93, "Xian",           null);
        // EN:Y  MK:Ј extra
        Add(Cty,  40.18,  44.51, "Yerevan",        "Јерван");
        // EN:Z  MK:З
        Add(Cty,  45.81,  15.98, "Zagreb",         "Загреб");
        // EN:Z  MK:Ц  (Zurich → Цирих)
        Add(Cty,  47.37,   8.55, "Zurich",         "Цирих");
        // MK-only coverage for rare letters
        Add(Cty,  41.99,  21.37, null,             "Ѓорче Петров"); // MK:Ѓ (municipality of Skopje)
        // MK:Ѕ — documented gap; no commonly-known city
        // MK:Ќ — documented gap; extremely rare initial letter for cities

        // ── VILLAGES ────────────────────────────────────────────────────
        // EN:A  MK:А
        Add(Vlg,  40.78,  17.24, "Alberobello",    "Алберобело");
        // EN:B  MK:Б
        Add(Vlg,  51.76,  -1.83, "Bibury",         "Бибури");
        // EN:C  MK:К  (Castle Combe → Касл Ком)
        Add(Vlg,  51.48,  -2.22, "Castle Combe",   "Касл Ком");
        // EN:D  MK:Д
        Add(Vlg,  58.57,  -4.74, "Durness",        "Дурнес");
        // EN:E  MK:Е
        Add(Vlg,  43.73,   7.36, "Eze",            "Ез");
        // EN:F  (Fiscardo; МК Ф covered via Фариш below)
        Add(Vlg,  38.45,  20.58, "Fiscardo",       null);
        // EN:G  MK:Г
        Add(Vlg,  46.62,   8.04, "Grindelwald",    "Гриндевалд");
        Add(Vlg,  41.28,  20.80, null,             "Галичник");   // MK:Г extra (famous Macedonian village)
        // EN:H  MK:Х
        Add(Vlg,  47.56,  13.65, "Hallstatt",      "Халштат");
        // EN:I  MK:И
        Add(Vlg,  36.44,  25.42, "Imerovigli",     "Имеровигли");
        // EN:J  (MK:Ж — Желино covers it below)
        Add(Vlg,  43.87,   5.27, "Joucas",         null);
        // EN:K  MK:К extra
        Add(Vlg,  51.24,   4.98, "Kasterlee",      "Кастерли");
        // EN:L  MK:Л
        Add(Vlg,  46.59,   7.91, "Lauterbrunnen",  "Лотербрунен");
        // EN:M  MK:М
        Add(Vlg,  38.44,  -7.37, "Monsaraz",       "Монсараз");
        // EN:N  MK:Н
        Add(Vlg,  48.85,  10.49, "Nordlingen",     "Нердлинген");
        // EN:O  MK:О
        Add(Vlg,  36.46,  25.37, "Oia",            "Оиа");
        // EN:P  MK:П
        Add(Vlg,  44.30,   9.21, "Portofino",      "Портофино");
        // EN:Q  (no easy MK pairing)
        Add(Vlg,  51.79,  11.15, "Quedlinburg",    null);
        // EN:R  MK:Р
        Add(Vlg,  49.38,  10.18, "Rothenburg",     "Ротенбург");
        // EN:S  (MK:С — Смилево below)
        Add(Vlg,  50.21,  -5.48, "St Ives",        null);
        // EN:T  MK:Т
        Add(Vlg,  40.71,  14.64, "Tramonti",       "Трамонти");
        // EN:U  MK:У
        Add(Vlg,  47.54,  11.07, "Unterammergau",  "Унтерамергау");
        // EN:V  MK:В
        Add(Vlg,  43.84,   5.98, "Valensole",      "Валансол");
        // EN:W
        Add(Vlg,  46.61,   7.92, "Wengen",         null);
        // EN:X  (Xàbia / Javea)
        Add(Vlg,  38.79,   0.17, "Xabia",          null);
        // EN:Y
        Add(Vlg,  49.74,   0.31, "Yport",          null);
        // EN:Z  MK:Ц  (Zermatt → Цермат; German Z = "ts" → Ц in MK)
        Add(Vlg,  46.02,   7.75, "Zermatt",        "Цермат");
        // MK-only villages to cover remaining letters
        Add(Vlg,  41.15,  21.23, null,             "Фариш");      // MK:Ф (Macedonian village)
        Add(Vlg,  41.94,  21.04, null,             "Желино");     // MK:Ж
        Add(Vlg,  41.77,  21.53, null,             "Ѓуѓанци");   // MK:Ѓ (Macedonian village)
        Add(Vlg,  41.23,  20.95, null,             "Јабланица");  // MK:Ј
        Add(Vlg,  41.98,  21.48, null,             "Љубанци");   // MK:Љ (village near Skopje)
        Add(Vlg,  42.76,  21.37, null,             "Смилево");    // MK:С (famous Macedonian village)
        Add(Vlg,  41.09,  22.87, null,             "Зрновци");    // MK:З (Macedonian village)
        Add(Vlg,  41.52,  21.56, null,             "Чашка");      // MK:Ч (Macedonian village)
        Add(Vlg,  41.87,  21.34, null,             "Шишево");     // MK:Ш (near Matka Canyon)
        Add(Vlg,  41.12,  22.18, null,             "Богданци");   // MK:Б extra
        Add(Vlg,  42.04,  21.47, null,             "Лешок");      // MK:Л extra
        // MK:Ѕ — documented gap
        // MK:Ќ — documented gap
        // MK:Њ — documented gap

        // ── COUNTRIES ───────────────────────────────────────────────────
        // EN:A  MK:А
        Add(Ctr,  41.15,  20.17, "Albania",        "Албанија");
        Add(Ctr, -38.42, -63.62, "Argentina",      "Аргентина");
        Add(Ctr, -25.27, 133.78, "Australia",      "Австралија");
        // EN:B  MK:Б
        Add(Ctr,  50.50,   4.47, "Belgium",        "Белгија");
        Add(Ctr,  42.73,  25.49, "Bulgaria",       "Бугарија");
        Add(Ctr, -14.24, -51.93, "Brazil",         "Бразил");
        // EN:C  MK:Ч  (Chile → Чиле)
        Add(Ctr, -35.68, -71.54, "Chile",          "Чиле");
        // EN:C  MK:Х  (Croatia → Хрватска)
        Add(Ctr,  45.10,  15.20, "Croatia",        "Хрватска");
        // EN:C  MK:К
        Add(Ctr,  35.86, 104.20, "China",          "Кина");
        // EN:D  MK:Д
        Add(Ctr,  56.26,   9.50, "Denmark",        "Данска");
        // EN:D  MK:Џ  (Djibouti → Џибути)
        Add(Ctr,  11.59,  43.14, "Djibouti",       "Џибути");
        // EN:E  MK:Е
        Add(Ctr,  -1.83, -78.18, "Ecuador",        "Еквадор");
        Add(Ctr,  26.82,  30.80, "Egypt",          "Египет");
        Add(Ctr,   9.15,  40.49, "Ethiopia",       "Етиопија");
        // EN:F  MK:Ф
        Add(Ctr,  46.23,   2.21, "France",         "Франција");
        Add(Ctr,  61.92,  25.75, "Finland",        "Финска");
        // EN:G  MK:Г
        Add(Ctr,  51.17,  10.45, "Germany",        "Германија");
        Add(Ctr,  39.07,  21.82, "Greece",         "Грција");
        Add(Ctr,  42.32,  43.36, "Georgia",        "Грузија");
        // EN:H  MK:У  (Hungary → Унгарија — great EN:H / MK:У pair!)
        Add(Ctr,  47.16,  19.50, "Hungary",        "Унгарија");
        // EN:I  MK:И
        Add(Ctr,  20.59,  78.96, "India",          "Индија");
        Add(Ctr,  41.87,  12.57, "Italy",          "Италија");
        Add(Ctr,  53.41,  -8.24, "Ireland",        "Ирска");
        // EN:J  MK:Ј
        Add(Ctr,  36.20, 138.25, "Japan",          "Јапонија");
        Add(Ctr,  31.24,  36.51, "Jordan",         "Јордан");
        // EN:K  MK:К extra
        Add(Ctr,  -1.29,  36.82, "Kenya",          "Кенија");
        Add(Ctr,  42.60,  21.04, "Kosovo",         "Косово");
        // EN:L  MK:Л
        Add(Ctr,  56.88,  24.60, "Latvia",         "Летонија");
        Add(Ctr,  55.17,  23.88, "Lithuania",      "Литванија");
        Add(Ctr,  47.81,   2.10, "Luxembourg",     "Луксембург");
        // EN:M  MK:М
        Add(Ctr,  31.79,  -7.09, "Morocco",        "Мароко");
        Add(Ctr,  23.63,-102.55, "Mexico",         "Мексико");
        // EN:M  MK:Ц  (Montenegro → Црна Гора)
        Add(Ctr,  42.71,  19.37, "Montenegro",     "Црна Гора");
        // EN:N  MK:Н
        Add(Ctr,  60.47,   8.47, "Norway",         "Норвешка");
        Add(Ctr,   9.08,   8.68, "Nigeria",        "Нигерија");
        // EN:N  MK:М extra  (North Macedonia → Македонија)
        Add(Ctr,  41.61,  21.75, "North Macedonia","Македонија");
        // EN:O  MK:О
        Add(Ctr,  21.51,  55.92, "Oman",           "Оман");
        // EN:P  MK:П
        Add(Ctr,  39.40,  -8.22, "Portugal",       "Португалија");
        Add(Ctr,  30.38,  69.35, "Pakistan",       "Пакистан");
        Add(Ctr,  51.92,  19.15, "Poland",         "Полска");
        // EN:Q  MK:К extra  (Qatar → Катар)
        Add(Ctr,  25.35,  51.18, "Qatar",          "Катар");
        // EN:R  MK:Р
        Add(Ctr,  45.94,  24.97, "Romania",        "Романија");
        Add(Ctr,  61.52, 105.32, "Russia",         "Русија");
        // EN:S  MK:С
        Add(Ctr,  44.02,  21.09, "Serbia",         "Србија");
        Add(Ctr,  46.15,  14.99, "Slovenia",       "Словенија");
        // EN:S  MK:Ш  (Spain → Шпанија)
        Add(Ctr,  40.46,  -3.75, "Spain",          "Шпанија");
        // EN:T  MK:Т
        Add(Ctr,  38.96,  35.24, "Turkey",         "Турција");
        Add(Ctr,  33.89,   9.54, "Tunisia",        "Тунис");
        Add(Ctr,  -6.37,  34.89, "Tanzania",       "Танзанија");
        // EN:U  MK:У extra
        Add(Ctr,  49.00,  31.39, "Ukraine",        "Украина");
        Add(Ctr,   1.37,  32.29, "Uganda",         "Уганда");
        // EN:V  MK:В
        Add(Ctr,  14.06, 108.28, "Vietnam",        "Виетнам");
        // EN:W — documented gap (no universally recognised sovereign state)
        // EN:X — documented gap (no UN member)
        // EN:Y  MK:Ј extra
        Add(Ctr,  15.55,  48.52, "Yemen",          "Јемен");
        // EN:Z  MK:З
        Add(Ctr, -19.02,  29.15, "Zimbabwe",       "Зимбабве");
        Add(Ctr, -13.13,  27.85, "Zambia",         "Замбија");
        // MK extra countries to improve coverage
        Add(Ctr,  47.52,  14.55, null,             "Австрија");   // MK:А extra (Austria)
        Add(Ctr,  22.35, 114.14, null,             "Хонг Конг"); // MK:Х extra
        Add(Ctr, -16.29, -63.59, null,             "Боливија");   // MK:Б extra (Bolivia)
        Add(Ctr,  12.35,  -1.56, null,             "Буркина Фасо"); // MK:Б extra
        Add(Ctr,  12.86,  30.22, null,             "Судан");      // MK:С extra (Sudan)
        Add(Ctr, -24.69,  25.91, null,             "Боцвана");    // MK:Б extra
        Add(Ctr,   1.35, 103.82, null,             "Сингапур");   // MK:С extra (Singapore)
        // MK:Ж — documented gap
        // MK:Ѓ — documented gap
        // MK:Ѕ — documented gap
        // MK:Њ — documented gap (New Zealand = Нов Зеланд starts with Н)
        // MK:Ќ — documented gap

        // ── RIVERS ──────────────────────────────────────────────────────
        // EN:A  MK:А
        Add(Riv,  -3.47, -58.48, "Amazon",         "Амазон");
        Add(Riv,  47.91, 106.89, null,             "Амур");      // MK:А extra
        // EN:B  MK:Б
        Add(Riv,  26.10,  90.00, "Brahmaputra",    "Брахмапутра");
        Add(Riv,  49.83,  24.03, null,             "Буг");        // MK:Б extra
        // EN:C  MK:К  (Congo → Конго)
        Add(Riv,  -4.32,  15.32, "Congo",          "Конго");
        Add(Riv,  36.20,  36.16, "Ceyhan",         null);        // EN:C extra
        // EN:D  MK:Д
        Add(Riv,  45.77,  29.70, "Danube",         "Дунав");
        Add(Riv,  48.46,  39.99, null,             "Дон");        // MK:Д extra
        // EN:E  MK:Е
        Add(Riv,  35.73,  38.63, "Euphrates",      "Евфрат");
        // EN:F  MK:Ф
        Add(Riv,  49.27,-123.15, "Fraser",         "Фрејзер");
        // EN:G  MK:Г
        Add(Riv,  23.48,  87.32, "Ganges",         "Ганг");
        // EN:H  MK:Х  (Hudson → Хадсон)
        Add(Riv,  40.70, -74.00, "Hudson",         "Хадсон");
        // EN:I  MK:И
        Add(Riv,  24.09,  67.47, "Indus",          "Инд");
        // EN:J  MK:Ј
        Add(Riv,  31.47,  35.56, "Jordan",         "Јордан");
        // EN:K  MK:К extra
        Add(Riv,  10.82,  79.00, "Kaveri",         "Кавери");
        // EN:L  MK:Л
        Add(Riv,  72.52, 126.95, "Lena",           "Лена");
        // EN:M  MK:М
        Add(Riv,  29.15, -89.25, "Mississippi",    "Мисисипи");
        Add(Riv,  15.60, 105.87, null,             "Меконг");     // MK:М extra
        // EN:N  MK:Н
        Add(Riv,  30.16,  31.09, "Nile",           "Нил");
        // EN:O  MK:О
        Add(Riv,  66.69,  69.12, "Ob",             "Об");
        // EN:P  MK:П
        Add(Riv, -25.29, -57.63, "Parana",         "Парана");
        Add(Riv,  45.65,  13.77, "Po",             "По");
        // EN:Q  MK nothing
        Add(Riv,  30.27, 120.17, "Qiantang",       null);
        // EN:R  MK:Р
        Add(Riv,  51.97,   4.12, "Rhine",          "Рајна");
        // EN:S  MK:С
        Add(Riv,  49.44,   0.15, "Seine",          "Сена");
        Add(Riv,  44.84,  20.44, "Sava",           "Сава");
        // EN:T  MK:Т
        Add(Riv,  51.50,  -0.13, "Thames",         "Темза");
        Add(Riv,  38.71,  -9.14, "Tagus",          "Тахо");
        // EN:U  MK:У
        Add(Riv,  51.23,  51.34, "Ural",           "Урал");
        // EN:V  MK:В  (Vardar → Вардар; major Macedonian river)
        Add(Riv,  40.35,  22.78, "Vardar",         "Вардар");
        Add(Riv,  45.79,  47.95, "Volga",          "Волга");
        // EN:W  MK:В extra
        Add(Riv,  53.54,   8.57, "Weser",          null);
        // EN:X  MK nothing
        Add(Riv,  -1.52, -51.95, "Xingu",          null);
        // EN:Y  MK:Ж  (Yellow River → Жолта Река)
        Add(Riv,  37.82, 119.18, "Yangtze",        null);
        Add(Riv,  37.82, 119.18, "Yellow",         "Жолта Река"); // EN:Y extra; MK:Ж
        // EN:Z  MK:З
        Add(Riv, -17.88,  25.26, "Zambezi",        "Замбези");
        // MK-only rivers
        Add(Riv,  42.07,  21.47, null,             "Пчиња");      // MK:П extra (Macedonian river)
        Add(Riv,  41.55,  21.77, null,             "Треска");     // MK:Т extra (Macedonian river)
        Add(Riv,  42.10,  21.93, null,             "Брегалница");  // MK:Б extra (Macedonian river)
        Add(Riv,  41.00,  22.50, null,             "Цена");       // MK:Ц (small river)
        Add(Riv,  41.95,  20.47, null,             "Дрим");       // MK:Д extra (Drim/Drin river)
        Add(Riv,  51.92,  19.15, null,             "Висла");      // MK:В extra (Vistula)
        Add(Riv,  59.95,  30.32, null,             "Нева");       // MK:Н extra (Neva)
        Add(Riv,  65.37,  25.44, null,             "Оулу");       // MK:О extra (Oulu river Finland)
        Add(Riv,  -9.64,  34.18, null,             "Лимпопо");    // MK:Л extra
        Add(Riv,  14.32,  -9.19, null,             "Сенегал");    // MK:С extra (Senegal river)
        Add(Riv,  13.46,   2.11, null,             "Нигер");      // MK:Н extra
        Add(Riv,  13.45, 100.57, null,             "Чао Праја");  // MK:Ч (Chao Phraya)
        // MK:Ѓ — documented gap
        // MK:Ѕ — documented gap
        // MK:Ќ — documented gap
        // MK:Љ — documented gap (no commonly-known river)
        // MK:Њ — documented gap
        // MK:Ш — documented gap
        // MK:Џ — documented gap

        // ── MOUNTAINS ───────────────────────────────────────────────────
        // EN:A  MK:А
        Add(Mtn,  46.83,   9.93, "Alps",           "Алпи");
        Add(Mtn, -32.65, -70.01, "Aconcagua",      "Аконкагуа");
        Add(Mtn, -16.00, -68.00, null,             "Анди");      // MK:А extra (Andes)
        // EN:B  MK:Б
        Add(Mtn,  56.80,  -5.00, "Ben Nevis",      "Бен Невис");
        // EN:C  MK:К  (Chimborazo → Чимборасо; EN:C but MK:Ч — different!)
        Add(Mtn,  -1.47, -78.82, "Chimborazo",     "Чимборасо"); // EN:C  MK:Ч
        Add(Mtn,  39.72,  44.28, null,             "Кавказ");     // MK:К (Caucasus)
        // EN:D  MK:Д
        Add(Mtn,  63.07,-151.00, "Denali",         "Денали");
        // EN:E  MK:Е
        Add(Mtn,  27.99,  86.93, "Everest",        "Еверест");
        Add(Mtn,  43.35,  42.45, "Elbrus",         "Елбрус");
        // EN:F  MK:Ф
        Add(Mtn,  35.36, 138.73, "Fuji",           "Фуџи");
        // EN:G  MK:Г  (Grossglockner → Гросглокнер)
        Add(Mtn,  47.07,  12.69, "Grossglockner",  "Гросглокнер");
        // EN:H  MK:Х
        Add(Mtn,  -9.12, -77.61, "Huascaran",      "Хуаскаран");
        // EN:I  MK:И
        Add(Mtn, -16.64, -67.78, "Illimani",       "Илимани");
        // EN:J  MK:Ј
        Add(Mtn,  46.54,   7.96, "Jungfrau",       "Јунгфрау");
        // EN:K  MK:К extra
        Add(Mtn,  -3.07,  37.35, "Kilimanjaro",    "Килиманџаро");
        // EN:L  MK:Л
        Add(Mtn,  60.57,-140.40, "Logan",          "Логан");
        // EN:M  MK:М
        Add(Mtn,  45.98,   7.66, "Matterhorn",     "Матерхорн");
        Add(Mtn,  45.83,   6.87, "Mont Blanc",     "Монблан");
        // EN:N  MK:Н
        Add(Mtn,  35.24,  74.59, "Nanga Parbat",   "Нанга Парбат");
        // EN:O  MK:О
        Add(Mtn,  40.09,  22.36, "Olympus",        "Олимп");
        // EN:P  MK:П
        Add(Mtn,  38.47, -28.40, "Pico",           "Пико");
        Add(Mtn,  42.62,   1.08, "Pyrenees",       "Пиринеи");
        // EN:Q  MK nothing
        Add(Mtn,  38.33, 100.17, "Qilian",         null);
        // EN:R  MK:Р
        Add(Mtn,  46.85,-121.73, "Rainier",        "Рајнир");
        // EN:S  MK:Ш  (Snowdon; Šar/Шар is the great Macedonian range)
        Add(Mtn,  53.07,  -4.08, "Snowdon",        null);
        Add(Mtn,  42.15,  21.00, "Sar",            "Шар Планина");  // EN:S  MK:Ш
        // EN:T  MK:Т
        Add(Mtn, -33.96,  18.40, "Table Mountain", "Трпезаста Планина");
        // EN:U  MK:У
        Add(Mtn,  40.11,  29.22, "Uludag",         "Улудаг");
        // EN:V  MK:В
        Add(Mtn,  40.82,  14.43, "Vesuvius",       "Везув");
        Add(Mtn, -78.54, -85.62, "Vinson",         "Винсон");
        // EN:W  MK:В extra
        Add(Mtn,  36.58,-118.29, "Whitney",        null);
        // EN:X  MK nothing
        Add(Mtn,  30.48, 103.56, "Xuebaoding",     null);
        // EN:Y  MK:Ј extra
        Add(Mtn,  10.27, -76.90, "Yerupaja",       "Јерупаха");
        // EN:Z  MK:З extra
        Add(Mtn,  47.42,  10.99, "Zugspitze",      "Цугшпице"); // EN:Z  MK:Ц extra
        Add(Mtn,  42.16,  21.28, null,             "Јакупица");  // MK:Ј extra (highest in MK)
        Add(Mtn,  41.76,  22.88, null,             "Беласица");  // MK:Б extra (Macedonian mountain)
        Add(Mtn,  41.97,  20.73, null,             "Стогово");   // MK:С extra (Macedonian mountain)
        Add(Mtn,  41.67,  20.80, null,             "Галичица");  // MK:Г extra (Macedonian mountain)
        Add(Mtn,  42.09,  22.37, null,             "Осогово");   // MK:О extra (Macedonian mountain)
        Add(Mtn,  41.40,  21.68, null,             "Козјак");    // MK:К extra (Macedonian mountain)
        Add(Mtn,  41.60,  20.57, null,             "Дешат");     // MK:Д extra (Macedonian mountain)
        Add(Mtn,  41.78,  22.40, null,             "Плачковица"); // MK:П extra (Macedonian mountain)
        Add(Mtn,  41.75,  21.73, null,             "Водно");     // MK:В extra (hill above Skopje)
        Add(Mtn,  41.86,  22.50, null,             "Малешевски Планини"); // MK:М extra
        Add(Mtn,  41.07,  21.35, null,             "Нидже");     // MK:Н extra (on MK-GR border)
        Add(Mtn,  41.33,  21.75, null,             "Тито Врв");  // MK:Т extra (peak in Šar range)
        Add(Mtn,  41.87,  21.98, null,             "Лисец");     // MK:Л extra (peak)
        Add(Mtn,  42.26,  22.08, null,             "Руен");      // MK:Р extra (highest in Osogovo)
        Add(Mtn,  41.60,  20.70, null,             "Китка");     // MK:К extra (peak near Skopje)
        // MK:Ж — documented gap
        // MK:З — documented gap
        // MK:Ѓ — documented gap
        // MK:Ѕ — documented gap
        // MK:Ќ — documented gap
        // MK:Љ — documented gap
        // MK:Њ — documented gap
        // MK:Џ — documented gap
        // MK:Ш — covered by Шар Планина

        return list.AsReadOnly();
    }
}
