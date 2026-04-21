using System.Text.Json;
using System.Text.Json.Serialization;

namespace FxTranslator;

/// <summary>Persisted translation data (saved as .fxtrans.json next to the .fx folder).</summary>
public class TranslationStore
{
    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = [];

    /// <summary>key -> { language -> translated text }</summary>
    [JsonPropertyName("translations")]
    public Dictionary<string, Dictionary<string, string>> Translations { get; set; } = [];

    // ── serialization ────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions SerOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static TranslationStore Load(string path)
    {
        if (!File.Exists(path)) return new();
        try
        {
            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<TranslationStore>(json, SerOpts) ?? new();
        }
        catch { return new(); }
    }

    public void Save(string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(this, SerOpts));
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    public string Get(string key, string language) =>
        Translations.TryGetValue(key, out var d) && d.TryGetValue(language, out var v) ? v : "";

    public void Set(string key, string language, string value)
    {
        if (!Translations.ContainsKey(key))
            Translations[key] = [];
        Translations[key][language] = value;
    }

    public void RemoveLanguage(string language)
    {
        Languages.Remove(language);
        foreach (var d in Translations.Values)
            d.Remove(language);
    }

    public bool IsFullyTranslated(string key) =>
        Languages.Count == 0 ||
        Languages.All(l => !string.IsNullOrWhiteSpace(Get(key, l)));
}
