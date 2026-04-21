# ShaderTranslation Tool (FxTranslator)

A Windows GUI tool for translating ReShade `.fx` shader files. It scans a folder of `.fx` files, extracts all `ui_label` and `ui_tooltip` annotation strings, and lets you manage translations for multiple languages. Translated files are exported as new `.fx` files ready to use.

---

## Features

- Scans all `.fx` files in a folder and extracts every `ui_label` / `ui_tooltip` string
- Manage translations for multiple languages in a spreadsheet-like grid
- Filter by type (Labels / Tooltips / Untranslated), or search by keyword
- Highlights untranslated cells in red
- Saves translations to a `.fxtrans.json` file next to your shaders
- Exports translated `.fx` files into per-language subfolders
- Shows translation progress statistics per language

---

## Download & Run (Release)

> **Requirements:** The tool is a framework-dependent build and requires the [.NET 9 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) to be installed on your machine.

1. Go to the [Releases page](https://github.com/KaiKaiQin/ShaderTranslation_Tool/releases)
2. Download **ShaderTranslation_Tool.zip**
3. Extract the zip to any folder
4. Run **FxTranslator.exe**

---

## Usage

### 1. Open a shader folder
Click **Open Folder** and select the folder containing your `.fx` shader files. The tool will scan all `.fx` files recursively and load every `ui_label` and `ui_tooltip` string into the grid.

### 2. Add a language
Click **Add Language** and enter a language name (e.g. `Chinese`, `Japanese`, `French`). A new column will appear in the grid for that language.

### 3. Enter translations
Double-click any cell in a language column to type the translation. Cells with missing translations are highlighted in red. Press **Enter** or click away to confirm.

### 4. Save
Click **Save** (or press `Ctrl+S`) to save all translations to `fx_translations.fxtrans.json` in the opened folder. This file preserves your work between sessions.

### 5. Export translated .fx files
Click **Export .fx** to generate translated shader files. For each language, a new subfolder is created next to the source folder (e.g. `Shaders_Chinese/`, `Shaders_Japanese/`), containing copies of the `.fx` files with the translated strings injected.

### Filtering
Use the search box to filter entries by file name, variable name, or English text. Use the dropdown to show only Labels, only Tooltips, or only Untranslated entries.

### Statistics
Click **Statistics** to see the total string count and translation completion percentage for each language.

---

## Build from Source

**Requirements:** .NET 9 SDK, Windows

```bash
git clone https://github.com/KaiKaiQin/ShaderTranslation_Tool.git
cd ShaderTranslation_Tool
dotnet build
dotnet run
```

To produce a self-contained single-file executable:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## File Format

Translations are stored as a JSON file (`fx_translations.fxtrans.json`) alongside your shaders:

```json
{
  "languages": ["Chinese", "Japanese"],
  "translations": {
    "ShaderName::VariableName::label": {
      "Chinese": "...",
      "Japanese": "..."
    }
  }
}
```
