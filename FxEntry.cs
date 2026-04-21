using System.Text.RegularExpressions;

namespace FxTranslator;

/// <summary>One extracted ui_label or ui_tooltip from a .fx file.</summary>
public class FxEntry
{
    public string Key { get; init; } = "";        // unique: "FileName::VarName::label"
    public string FileName { get; init; } = "";
    public string FilePath { get; init; } = "";
    public string Variable { get; init; } = "";
    public string Type { get; init; } = "";       // "label" or "tooltip"
    public string English { get; init; } = "";
}

public static class FxParser
{
    // Matches: uniform <type> <varName> < ...annotation... >
    private static readonly Regex BlockRx = new(
        @"uniform\s+\S+\s+(\w+)\s*<([^>]+)>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LabelRx = new(
        @"ui_label\s*=\s*""((?:[^""\\]|\\.)*)""",
        RegexOptions.Compiled);

    private static readonly Regex TooltipRx = new(
        @"ui_tooltip\s*=\s*""((?:[^""\\]|\\.)*)""",
        RegexOptions.Compiled);

    public static List<FxEntry> Parse(string filePath)
    {
        var entries = new List<FxEntry>();
        string text;
        try { text = File.ReadAllText(filePath); }
        catch { return entries; }

        string fileName = Path.GetFileName(filePath);
        string stem = Path.GetFileNameWithoutExtension(filePath);

        foreach (Match m in BlockRx.Matches(text))
        {
            string varName = m.Groups[1].Value;
            string annotation = m.Groups[2].Value;

            var labelM = LabelRx.Match(annotation);
            var tooltipM = TooltipRx.Match(annotation);

            if (labelM.Success)
            {
                string label = Unescape(labelM.Groups[1].Value);
                entries.Add(new FxEntry
                {
                    Key = $"{stem}::{varName}::label",
                    FileName = fileName,
                    FilePath = filePath,
                    Variable = varName,
                    Type = "label",
                    English = label,
                });
            }

            if (tooltipM.Success)
            {
                string tooltip = Unescape(tooltipM.Groups[1].Value);
                tooltip = Regex.Replace(tooltip, @"\s*\r?\n\s*", " ").Trim();
                entries.Add(new FxEntry
                {
                    Key = $"{stem}::{varName}::tooltip",
                    FileName = fileName,
                    FilePath = filePath,
                    Variable = varName,
                    Type = "tooltip",
                    English = tooltip,
                });
            }
        }
        return entries;
    }

    private static string Unescape(string s) =>
        s.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n");

    // ── Translation writer ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the full text of <paramref name="filePath"/> with every
    /// ui_label / ui_tooltip value replaced by the stored translation for
    /// <paramref name="language"/>. Falls back to the original English text
    /// when a translation is missing.
    /// </summary>
    public static string TranslateFile(string filePath, string language, TranslationStore store)
    {
        string text = File.ReadAllText(filePath);
        string stem = Path.GetFileNameWithoutExtension(filePath);

        // Replace each uniform block's annotation section
        text = BlockRx.Replace(text, m =>
        {
            string varName    = m.Groups[1].Value;
            string annotation = m.Groups[2].Value;
            string fullMatch  = m.Value;

            string labelKey   = $"{stem}::{varName}::label";
            string tooltipKey = $"{stem}::{varName}::tooltip";

            string newAnnotation = annotation;

            // Replace ui_label value
            newAnnotation = LabelRx.Replace(newAnnotation, lm =>
            {
                string translation = store.Get(labelKey, language);
                string replacement = string.IsNullOrWhiteSpace(translation)
                    ? lm.Groups[1].Value          // keep original
                    : EscapeFxString(translation);
                return "ui_label = \"" + replacement + "\"";
            });

            // Replace ui_tooltip value
            newAnnotation = TooltipRx.Replace(newAnnotation, tm =>
            {
                string translation = store.Get(tooltipKey, language);
                string replacement = string.IsNullOrWhiteSpace(translation)
                    ? tm.Groups[1].Value          // keep original
                    : EscapeFxString(translation);
                return "ui_tooltip = \"" + replacement + "\"";
            });

            // Reconstruct block with replaced annotation
            return fullMatch.Replace(annotation, newAnnotation);
        });

        return text;
    }

    private static string EscapeFxString(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
