using System.Text;
using System.Text.RegularExpressions;

namespace FxTranslator;

public partial class MainForm : Form
{
    // ── State ────────────────────────────────────────────────────────────────
    private string? _fxFolder;
    private string TransFile => _fxFolder is null ? "" : Path.Combine(_fxFolder, "fx_translations.fxtrans.json");

    private List<FxEntry> _allEntries = [];
    private List<FxEntry> _visibleEntries = [];
    private TranslationStore _store = new();
    private bool _dirtyFlag;

    private bool _dirty
    {
        get => _dirtyFlag;
        set { _dirtyFlag = value; UpdateTitle(); }
    }

    // Fixed read-only column indices
    private const int ColFile = 0;
    private const int ColVar = 1;
    private const int ColType = 2;
    private const int ColEnglish = 3;
    private const int FixedCols = 4;

    // ── Constructor ──────────────────────────────────────────────────────────
    public MainForm()
    {
        InitializeComponent();
        FormClosing += OnFormClosing;
        KeyPreview = true;
    }

    // ── Grid setup ───────────────────────────────────────────────────────────

    private void RebuildColumns()
    {
        grid.Columns.Clear();

        void AddFixed(string name, int width, bool wrap = false)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = name, HeaderText = name, Width = width,
                ReadOnly = true, SortMode = DataGridViewColumnSortMode.Programmatic,
            };
            col.DefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            col.DefaultCellStyle.ForeColor = Color.FromArgb(80, 80, 80);
            if (wrap) col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.Columns.Add(col);
        }

        AddFixed("File",     140);
        AddFixed("Variable", 130);
        AddFixed("Type",      60);
        AddFixed("English",  300, wrap: true);

        foreach (string lang in _store.Languages)
        {
            var col = new DataGridViewTextBoxColumn
            {
                Name = lang, HeaderText = lang, Width = 260,
                SortMode = DataGridViewColumnSortMode.Programmatic,
            };
            col.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.Columns.Add(col);
        }
    }

    private void PopulateGrid(List<FxEntry> entries)
    {
        grid.SuspendLayout();
        grid.Rows.Clear();
        _visibleEntries = entries;

        foreach (var e in entries)
        {
            var values = new object[FixedCols + _store.Languages.Count];
            values[ColFile]    = e.FileName;
            values[ColVar]     = e.Variable;
            values[ColType]    = e.Type;
            values[ColEnglish] = e.English;
            for (int i = 0; i < _store.Languages.Count; i++)
                values[FixedCols + i] = _store.Get(e.Key, _store.Languages[i]);

            grid.Rows.Add(values);
            grid.Rows[^1].Tag = e.Key;
        }

        grid.ResumeLayout();
    }

    // ── Filtering ────────────────────────────────────────────────────────────

    private void ApplyFilter()
    {
        string text = txtFilter.Text.Trim().ToLowerInvariant();
        string show = cboShow.SelectedItem?.ToString() ?? "All";

        var filtered = _allEntries.Where(e =>
        {
            if (show == "Labels only"     && e.Type != "label")   return false;
            if (show == "Tooltips only"   && e.Type != "tooltip") return false;
            if (show == "Untranslated only" && _store.IsFullyTranslated(e.Key)) return false;
            if (text.Length > 0 &&
                !e.FileName.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                !e.Variable.Contains(text, StringComparison.OrdinalIgnoreCase) &&
                !e.English.Contains(text, StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }).ToList();

        PopulateGrid(filtered);
        SetStatus($"Showing {filtered.Count} / {_allEntries.Count} entries.");
    }

    // ── Grid events ──────────────────────────────────────────────────────────

    private void Grid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.ColumnIndex < FixedCols) return;

        var row = grid.Rows[e.RowIndex];
        string key  = row.Tag?.ToString() ?? "";
        string lang = grid.Columns[e.ColumnIndex].Name;
        string val  = row.Cells[e.ColumnIndex].Value?.ToString() ?? "";

        _store.Set(key, lang, val);
        _dirty = true;
        SetStatus($"Modified [{key}] {lang} = \"{val}\"");
    }

    private void Grid_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < FixedCols) return;
        var row = grid.Rows[e.RowIndex];
        string key = row.Tag?.ToString() ?? "";

        bool fullyTranslated = _store.IsFullyTranslated(key);

        // Highlight untranslated rows
        if (!fullyTranslated && e.ColumnIndex >= FixedCols)
        {
            string cellVal = e.Value?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(cellVal))
                e.CellStyle.BackColor = Color.FromArgb(255, 240, 240);
        }
    }

    private int _sortColumn = -1;
    private SortOrder _sortOrder = SortOrder.None;

    private void Grid_ColumnHeaderMouseClick(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.ColumnIndex == _sortColumn)
        {
            _sortOrder = _sortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
        }
        else
        {
            _sortColumn = e.ColumnIndex;
            _sortOrder  = SortOrder.Ascending;
        }

        string colName = grid.Columns[e.ColumnIndex].Name;
        bool asc = _sortOrder == SortOrder.Ascending;

        _visibleEntries = colName switch
        {
            "File"     => asc ? [.. _visibleEntries.OrderBy(x => x.FileName)]  : [.. _visibleEntries.OrderByDescending(x => x.FileName)],
            "Variable" => asc ? [.. _visibleEntries.OrderBy(x => x.Variable)]  : [.. _visibleEntries.OrderByDescending(x => x.Variable)],
            "Type"     => asc ? [.. _visibleEntries.OrderBy(x => x.Type)]      : [.. _visibleEntries.OrderByDescending(x => x.Type)],
            "English"  => asc ? [.. _visibleEntries.OrderBy(x => x.English)]   : [.. _visibleEntries.OrderByDescending(x => x.English)],
            _ => asc
                ? [.. _visibleEntries.OrderBy(x => _store.Get(x.Key, colName))]
                : [.. _visibleEntries.OrderByDescending(x => _store.Get(x.Key, colName))],
        };

        foreach (DataGridViewColumn col in grid.Columns)
            col.HeaderCell.SortGlyphDirection = SortOrder.None;
        grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = _sortOrder;

        PopulateGrid(_visibleEntries);
    }

    // ── File operations ──────────────────────────────────────────────────────

    private void OpenFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select folder containing .fx shader files",
            UseDescriptionForTitle = true,
        };
        if (_fxFolder is not null) dlg.InitialDirectory = _fxFolder;
        if (dlg.ShowDialog() != DialogResult.OK) return;

        if (_dirty)
        {
            var r = MessageBox.Show("You have unsaved changes. Save before opening a new folder?",
                "FxTranslator", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (r == DialogResult.Cancel) return;
            if (r == DialogResult.Yes) Save();
        }

        _fxFolder = dlg.SelectedPath;

        _store = TranslationStore.Load(TransFile);
        _dirty = false;

        DoRefresh();
    }

    private new void Refresh()
    {
        if (_fxFolder is null) { MessageBox.Show("Please open a folder first.", "FxTranslator"); return; }
        DoRefresh();
    }

    private void DoRefresh()
    {
        if (_fxFolder is null) return;

        BeginBusy("Scanning .fx files…");
        try
        {
            _allEntries = Directory
                .EnumerateFiles(_fxFolder, "*.fx", SearchOption.AllDirectories)
                .OrderBy(f => f)
                .SelectMany(FxParser.Parse)
                .ToList();

            RebuildColumns();
            ApplyFilter();

            UpdateTitle();
            SetStatus($"Loaded {_allEntries.Count} strings from {_fxFolder}");
        }
        finally { EndBusy(); }
    }

    private void Save()
    {
        if (_fxFolder is null) { MessageBox.Show("Please open a folder first.", "FxTranslator"); return; }
        _store.Save(TransFile);
        _dirty = false;
        SetStatus($"Saved → {TransFile}");
    }

    // ── Export .fx ───────────────────────────────────────────────────────────

    private void ExportFx()
    {
        if (_fxFolder is null) { MessageBox.Show("Please open a folder first.", "FxTranslator"); return; }
        if (_store.Languages.Count == 0) { MessageBox.Show("No languages defined. Add a language first.", "FxTranslator"); return; }

        // Auto-save before exporting so translations are up to date
        _store.Save(TransFile);
        _dirty = false;

        var fxFiles = Directory
            .EnumerateFiles(_fxFolder, "*.fx", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        if (fxFiles.Count == 0) { MessageBox.Show("No .fx files found in the open folder.", "FxTranslator"); return; }

        int totalWritten = 0;

        BeginBusy("Exporting translated .fx files…");
        try
        {
            foreach (string lang in _store.Languages)
            {
                // Create subfolder next to (sibling of) the source folder
                string parentDir = Path.GetDirectoryName(_fxFolder) ?? _fxFolder;
                string sourceName = Path.GetFileName(_fxFolder);
                string outDir = Path.Combine(parentDir, $"{sourceName}_{lang}");
                Directory.CreateDirectory(outDir);

                foreach (string fxPath in fxFiles)
                {
                    string translated = FxParser.TranslateFile(fxPath, lang, _store);
                    // preserve subfolder structure relative to the source folder
                    string relPath = Path.GetRelativePath(_fxFolder, fxPath);
                    string destPath = Path.Combine(outDir, relPath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.WriteAllText(destPath, translated, Encoding.UTF8);
                    totalWritten++;
                }
            }
        }
        finally { EndBusy(); }

        string summary = string.Join("\n", _store.Languages.Select(l =>
        {
            string parentDir = Path.GetDirectoryName(_fxFolder) ?? _fxFolder;
            string outDir = Path.Combine(parentDir, $"{Path.GetFileName(_fxFolder)}_{l}");
            return $"  [{l}] → {outDir}";
        }));

        SetStatus($"Exported {totalWritten} file(s) across {_store.Languages.Count} language(s).");
        MessageBox.Show($"Exported {totalWritten} .fx file(s):\n\n{summary}", "Export Complete");
    }

    // ── Language management ──────────────────────────────────────────────────

    private void AddLanguage()
    {
        string? lang = Prompt("Add Language", "Enter language name (e.g. Chinese, Japanese, French):");
        if (lang is null || lang.Trim().Length == 0) return;
        lang = lang.Trim();

        if (_store.Languages.Contains(lang, StringComparer.OrdinalIgnoreCase))
        {
            MessageBox.Show($"Language \"{lang}\" already exists.", "FxTranslator");
            return;
        }

        _store.Languages.Add(lang);
        _dirty = true;
        RebuildColumns();
        ApplyFilter();
        SetStatus($"Language \"{lang}\" added. Double-click a cell to start translating.");
    }

    private void RemoveLanguage()
    {
        if (_store.Languages.Count == 0) { MessageBox.Show("No languages to remove.", "FxTranslator"); return; }

        string options = string.Join(", ", _store.Languages);
        string? input = Prompt("Remove Language", $"Language to remove ({options}):");
        if (input is null) return;
        input = input.Trim();

        string? lang = _store.Languages.FirstOrDefault(l => string.Equals(l, input, StringComparison.OrdinalIgnoreCase));
        if (lang is null)
        {
            MessageBox.Show($"Language \"{input}\" not found.", "FxTranslator");
            return;
        }

        if (MessageBox.Show($"Remove \"{lang}\" and all its translations?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        _store.RemoveLanguage(lang);
        _dirty = true;
        RebuildColumns();
        ApplyFilter();
    }

    // ── Statistics ───────────────────────────────────────────────────────────

    private void ShowStats()
    {
        if (_allEntries.Count == 0) { MessageBox.Show("No entries loaded.", "Statistics"); return; }

        var sb = new StringBuilder();
        sb.AppendLine($"Total strings : {_allEntries.Count}");
        sb.AppendLine($"  Labels      : {_allEntries.Count(e => e.Type == "label")}");
        sb.AppendLine($"  Tooltips    : {_allEntries.Count(e => e.Type == "tooltip")}");
        sb.AppendLine();

        if (_store.Languages.Count == 0)
        {
            sb.AppendLine("No translation languages defined yet.");
        }
        else
        {
            sb.AppendLine("Translation progress:");
            foreach (string lang in _store.Languages)
            {
                int done = _allEntries.Count(e => !string.IsNullOrWhiteSpace(_store.Get(e.Key, lang)));
                double pct = 100.0 * done / _allEntries.Count;
                sb.AppendLine($"  {lang,-20} {done,4} / {_allEntries.Count}  ({pct:F1}%)");
            }
        }

        MessageBox.Show(sb.ToString(), "Translation Statistics", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void SetStatus(string msg) => lblStatus.Text = msg;

    private void UpdateTitle() =>
        Text = _fxFolder is null ? "FxTranslator" : $"FxTranslator — {Path.GetFileName(_fxFolder)}{(_dirty ? " *" : "")}";

    private void BeginBusy(string status)
    {
        UseWaitCursor = true;
        progBar.Style = ProgressBarStyle.Marquee;
        progBar.Visible = true;
        SetStatus(status);
        Application.DoEvents();
    }

    private void EndBusy()
    {
        progBar.Visible = false;
        UseWaitCursor = false;
    }

    private static string? Prompt(string title, string message)
    {
        using var form = new Form
        {
            Text = title, Size = new Size(420, 140),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
        };
        var lbl = new Label { Text = message, Left = 12, Top = 12, Width = 380, AutoSize = false, Height = 24 };
        var txt = new TextBox   { Left = 12, Top = 38, Width = 380 };
        var ok  = new Button    { Text = "OK",     Left = 218, Top = 68, Width = 80, DialogResult = DialogResult.OK };
        var no  = new Button    { Text = "Cancel", Left = 308, Top = 68, Width = 80, DialogResult = DialogResult.Cancel };
        form.Controls.AddRange([lbl, txt, ok, no]);
        form.AcceptButton = ok;
        form.CancelButton = no;
        return form.ShowDialog() == DialogResult.OK ? txt.Text : null;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_dirty) return;
        var r = MessageBox.Show("You have unsaved changes. Save before closing?",
            "FxTranslator", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        if (r == DialogResult.Yes) Save();
        else if (r == DialogResult.Cancel) e.Cancel = true;
    }
}
