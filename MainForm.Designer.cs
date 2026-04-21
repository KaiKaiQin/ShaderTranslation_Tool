namespace FxTranslator;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    private MenuStrip menuStrip;
    private ToolStrip toolStrip;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel lblStatus;
    private Panel filterPanel;
    private Label lblFilter;
    private TextBox txtFilter;
    private Label lblShow;
    private ComboBox cboShow;
    private DataGridView grid;
    private ToolStripProgressBar progBar;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        SuspendLayout();

        // ── Form ──────────────────────────────────────────────────────────
        Text = "FxTranslator";
        Size = new Size(1280, 760);
        MinimumSize = new Size(900, 600);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);

        // ── MenuStrip ────────────────────────────────────────────────────
        menuStrip = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("File");
        var miOpen    = new ToolStripMenuItem("Open Folder…",  null, (s, e) => OpenFolder())  { ShortcutKeys = Keys.Control | Keys.O };
        var miSave    = new ToolStripMenuItem("Save",          null, (s, e) => Save())        { ShortcutKeys = Keys.Control | Keys.S };
        var miExport  = new ToolStripMenuItem("Export .fx files…", null, (s, e) => ExportFx());
        var miSep     = new ToolStripSeparator();
        var miExit    = new ToolStripMenuItem("Exit",          null, (s, e) => Close());
        fileMenu.DropDownItems.AddRange([miOpen, miSave, miExport, miSep, miExit]);

        var langMenu   = new ToolStripMenuItem("Languages");
        var miAddLang  = new ToolStripMenuItem("Add Language…",    null, (s, e) => AddLanguage());
        var miDelLang  = new ToolStripMenuItem("Remove Language…", null, (s, e) => RemoveLanguage());
        langMenu.DropDownItems.AddRange([miAddLang, miDelLang]);

        var viewMenu    = new ToolStripMenuItem("View");
        var miRefresh   = new ToolStripMenuItem("Refresh Shaders", null, (s, e) => Refresh()) { ShortcutKeys = Keys.F5 };
        var miStats     = new ToolStripMenuItem("Statistics…",     null, (s, e) => ShowStats());
        viewMenu.DropDownItems.AddRange([miRefresh, miStats]);

        menuStrip.Items.AddRange([fileMenu, langMenu, viewMenu]);

        // ── ToolStrip ────────────────────────────────────────────────────
        toolStrip = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden };

        ToolStripButton Btn(string text, string tip, Action act)
        {
            var b = new ToolStripButton(text) { ToolTipText = tip, DisplayStyle = ToolStripItemDisplayStyle.Text };
            b.Click += (_, _) => act();
            return b;
        }

        toolStrip.Items.AddRange([
            Btn("📂 Open Folder",    "Open a folder of .fx shader files",  OpenFolder),
            Btn("💾 Save",           "Save translations (Ctrl+S)",          Save),
            new ToolStripSeparator(),
            Btn("＋ Add Language",   "Add a target translation language",   AddLanguage),
            Btn("－ Remove Language","Remove a translation language",        RemoveLanguage),
            new ToolStripSeparator(),
            Btn("🔄 Refresh",        "Re-scan .fx files (F5)",              Refresh),
            Btn("📊 Statistics",     "Show translation progress",           ShowStats),
            new ToolStripSeparator(),
            Btn("📤 Export .fx",     "Export translated .fx files per language", ExportFx),
        ]);

        // ── Filter panel ─────────────────────────────────────────────────
        filterPanel = new Panel { Height = 32, Dock = DockStyle.Top, Padding = new Padding(4, 4, 4, 0) };

        lblFilter = new Label { Text = "Filter:", AutoSize = true, Top = 8, Left = 4 };
        txtFilter = new TextBox { Top = 4, Left = 48, Width = 220, Height = 22 };
        txtFilter.TextChanged += (_, _) => ApplyFilter();

        lblShow = new Label { Text = "Show:", AutoSize = true, Top = 8, Left = 282 };
        cboShow = new ComboBox
        {
            Top = 4, Left = 318, Width = 160, DropDownStyle = ComboBoxStyle.DropDownList,
            Items = { "All", "Labels only", "Tooltips only", "Untranslated only" },
            SelectedIndex = 0
        };
        cboShow.SelectedIndexChanged += (_, _) => ApplyFilter();

        filterPanel.Controls.AddRange([lblFilter, txtFilter, lblShow, cboShow]);

        // ── DataGridView ─────────────────────────────────────────────────
        grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.CellSelect,
            EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            RowHeadersWidth = 36,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            ColumnHeadersHeight = 26,
            RowTemplate = { Height = 22 },
            BorderStyle = BorderStyle.None,
            BackgroundColor = SystemColors.Window,
            GridColor = Color.FromArgb(220, 220, 220),
        };
        grid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(60, 60, 60);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        grid.EnableHeadersVisualStyles = false;
        grid.CellEndEdit += Grid_CellEndEdit;
        grid.CellFormatting += Grid_CellFormatting;
        grid.ColumnHeaderMouseClick += Grid_ColumnHeaderMouseClick;

        // ── StatusStrip ──────────────────────────────────────────────────
        statusStrip = new StatusStrip();
        lblStatus = new ToolStripStatusLabel("Ready. Open a folder containing .fx shader files.") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        progBar = new ToolStripProgressBar { Visible = false, Width = 150 };
        statusStrip.Items.AddRange([lblStatus, progBar]);

        // ── Layout ───────────────────────────────────────────────────────
        Controls.AddRange([grid, filterPanel, toolStrip, menuStrip, statusStrip]);
        MainMenuStrip = menuStrip;

        ResumeLayout(false);
        PerformLayout();
    }
}
