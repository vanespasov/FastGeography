namespace FastGeography.Client.Services;

using System.Globalization;

using FastGeography.Shared;

using Microsoft.JSInterop;

/// <summary>
/// Holds the current game language for the session. The nav picker writes here;
/// game pages read it when starting a round so a language change takes effect
/// immediately without a page reload.
/// Also sets <see cref="CultureInfo.DefaultThreadCurrentUICulture"/> so
/// <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/> returns
/// the correct language in all components.
/// </summary>
public sealed class GameLanguageState
{
    private readonly IJSRuntime _js;
    private bool _loaded;

    public GameLanguageState(IJSRuntime js) => _js = js;

    public string Code { get; private set; } = "en";

    public event Action? Changed;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        var stored = await _js.InvokeAsync<string?>("localStorage.getItem", "fg_lang");
        Code = GameLanguageExtensions.Parse(stored).ToCode();
        ApplyCulture();
        _loaded = true;
    }

    public async Task SetAsync(string? code)
    {
        var parsed = GameLanguageExtensions.Parse(code).ToCode();
        Code = parsed;
        _loaded = true;
        ApplyCulture();
        await _js.InvokeVoidAsync("localStorage.setItem", "fg_lang", parsed);
        Changed?.Invoke();
    }

    private void ApplyCulture()
    {
        var culture = new CultureInfo(Code);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
