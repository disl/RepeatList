using System.Globalization;

namespace RepeatList.Services;

/// <summary>
/// Immutable AI provider configuration. Read/write via <see cref="AiSettingsService"/>.
/// </summary>
public sealed record AiSettings(string Provider, string ApiKey, string BaseUrl, string Model)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(BaseUrl);
}

/// <summary>
/// Stores and loads the AI provider configuration.
/// The API key is kept in SecureStorage (device-encrypted), everything else in Preferences.
/// A singleton instance is used because DeepSeekClient is created via <c>new</c> in several places.
/// </summary>
public class AiSettingsService
{
    private static readonly Lazy<AiSettingsService> LazyInstance = new(() => new AiSettingsService());

    /// <summary>Shared instance, mirroring the existing LocalizationResourceManager.Instance pattern.</summary>
    public static AiSettingsService Instance => LazyInstance.Value;

    public const string ProviderDeepSeek = "DeepSeek";
    public const string ProviderOpenRouter = "OpenRouter";
    public const string ProviderCustom = "Custom";
    public const string DeepSeekBaseUrl = "https://api.deepseek.com/v1";
    public const string DeepSeekModel = "deepseek-chat";
    public const string DeepSeekKeysUrl = "https://platform.deepseek.com/api_keys";
    public const string OpenRouterBaseUrl = "https://openrouter.ai/api/v1";
    public const string OpenRouterDefaultModel = "deepseek/deepseek-chat";
    public const string OpenRouterKeysUrl = "https://openrouter.ai/keys";
    public const string CustomKeysUrl = "https://platform.openai.com/api-keys";

    private const string PrefProvider = "ai_provider";
    private const string PrefBaseUrl = "ai_baseurl";
    private const string PrefModel = "ai_model";
    private const string PrefSaved = "ai_settings_saved";
    private const string SecureApiKey = "ai_apikey";

    /// <summary>Preference key for the AI unlock dialog "don't show again" checkbox.</summary>
    public const string PrefAiDialogDontShowAgain = "ai_dialog_dont_show_again";

    /// <summary>True once a connection test succeeded, so callers skip the settings prompt.</summary>
    public bool HasSavedSettings => Preferences.Default.Get(PrefSaved, false);

    /// <summary>True once the user checked "don't show again" on the AI unlock dialog.</summary>
    public bool AiDialogDontShowAgain => Preferences.Default.Get(PrefAiDialogDontShowAgain, false);

    public async Task<AiSettings> LoadAsync()
    {
        var provider = Preferences.Default.Get(PrefProvider, ProviderDeepSeek);
        var baseUrl = Preferences.Default.Get(PrefBaseUrl, DeepSeekBaseUrl);
        var model = Preferences.Default.Get(PrefModel, DeepSeekModel);
        var apiKey = await SecureStorage.Default.GetAsync(SecureApiKey) ?? "";

        return new AiSettings(provider, apiKey, baseUrl, model);
    }

    public async Task SaveAsync(AiSettings settings)
    {
        Preferences.Default.Set(PrefProvider, settings.Provider);
        Preferences.Default.Set(PrefBaseUrl, settings.BaseUrl);
        Preferences.Default.Set(PrefModel, settings.Model);
        Preferences.Default.Set(PrefSaved, true);

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            await SecureStorage.Default.SetAsync(SecureApiKey, settings.ApiKey);
        else
            SecureStorage.Default.Remove(SecureApiKey);
    }

    /// <summary>
    /// Localized resource lookup without depending on the (IDE-generated) Designer class.
    /// Falls back to the key itself when the resource is missing.
    /// </summary>
    public static string T(string key)
    {
        var text = global::RepeatList.Properties.Resources.ResourceManager.GetString(key, CultureInfo.CurrentCulture);
        return string.IsNullOrEmpty(text) ? key : text;
    }
}
