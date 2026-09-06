using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: System.Reflection.AssemblyTitle("Extended Hotbar Helper")]
[assembly: System.Reflection.AssemblyProduct("Extended Hotbar Helper")]
[assembly: System.Reflection.AssemblyVersion("0.1.2.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.1.2.0")]

namespace ExtendedHotbar.Helper
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            // Preview is off-screen, read-only, with action callbacks disabled.
            if (args.Length == 2 && args[0] == "--preview-all")
            {
                Directory.CreateDirectory(args[1]);
                foreach (var code in UiText.Codes) Preview(Path.Combine(args[1], code + ".png"), code);
                return;
            }
            if (args.Length >= 2 && args.Length <= 3 && args[0] == "--preview")
            { Preview(args[1], args.Length == 3 ? args[2] : "de"); return; }
            var text = new UiText(UiText.FromCulture(CultureInfo.CurrentUICulture.Name));
            try
            {
                if (args.Length != 0) throw new ArgumentException("Start the helper without command-line arguments.");
                Application.Run(new MainForm(false));
            }
            catch (Exception ex) { MessageBox.Show(text.Format("notDone", text.Error(ex)) + "\n\n" + text["diagnostics"] + "\n" + ex.Message, "Extended Hotbar", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        private static void Preview(string path, string code)
        {
            using (var form = new MainForm(true, code))
            {
                form.ShowInTaskbar = false; form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-20000, -20000); form.Show(); Application.DoEvents();
                // Exercise repeated live switches, not just initial-language construction.
                foreach (var language in UiText.Codes.Reverse()) form.SelectLanguage(language);
                form.SelectLanguage(code); Application.DoEvents(); form.PerformLayout(); form.ValidatePreview();
                form.PreviewStates(path);
            }
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly TextBox game = new TextBox(), profile = new TextBox(), report = new TextBox();
        private readonly ComboBox history = new ComboBox(), languages = new ComboBox();
        private readonly Label loadStatus = new Label();
        private readonly Timer statusTimer = new Timer { Interval = 2000 };
        private readonly Dictionary<Control, string> captions = new Dictionary<Control, string>();
        private readonly List<Button> actions = new List<Button>();
        private readonly List<Label> wrappingLabels = new List<Label>();
        private readonly Operations operations;
        private readonly bool preview;
        private UiText text;
        private readonly Dictionary<string, Font> uiFonts = new Dictionary<string, Font>();
        private readonly Font pathFont = new Font("Segoe UI", 10);
        private HotbarStatus? lastStatus;
        private bool inactiveNotice, offeredNativeKeys;
        private string inactiveGame, inactiveProfile, diagnostic, lastBackup;
        private string reportKey = "ready";
        private object[] reportArgs = new object[0];
        private Exception reportError;
        private Button details, undoButton, advancedToggle;
        private TableLayoutPanel advanced;
        private bool advancedOpen;
        private TableLayoutPanel layout;

        internal MainForm(bool preview, string language = null)
        {
            this.preview = preview;
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userFolder)) userFolder = Environment.GetEnvironmentVariable("USERPROFILE");
            if (string.IsNullOrWhiteSpace(userFolder)) throw new IOException("Cannot determine Windows user folder.");
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local)) local = Path.Combine(userFolder, "AppData", "Local");
            operations = new Operations(Path.Combine(local, "ExtendedHotbarHelper", "Backups"), new GamePolicy());
            profile.Text = Path.Combine(userFolder, "AppData", "LocalLow", "CanOpener", "Dungeon Settlers");
            game.Font = pathFont; profile.Font = pathFont;
            game.Text = preview ? @"H:\Steam\steamapps\common\Dungeon Settlers" : FindGames().FirstOrDefault() ?? "";
            text = new UiText(language ?? (preview ? "de" : UiText.Detect(profile.Text)));
            AutoScaleMode = AutoScaleMode.Dpi; ClientSize = new Size(840, 850); MinimumSize = new Size(760, 650);
            StartPosition = FormStartPosition.CenterScreen; BackColor = Color.FromArgb(246, 247, 249);
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(24), ColumnCount = 1 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); scroll.Controls.Add(layout); Controls.Add(scroll);
            Add(new Label { Text = "Extended Hotbar", AutoSize = true, Font = new Font("Segoe UI", 23, FontStyle.Bold), Margin = new Padding(0, 0, 0, 6) });
            Add(new Label { Text = "Mod 0.3.7 · DS_B.0.4.19 · MelonLoader 0.7.3", AutoSize = true, Margin = new Padding(0, 0, 0, 8) });
            var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0, 0, 0, 8) };
            toolbar.Controls.Add(Bind(new Label { AutoSize = true, Margin = new Padding(0, 7, 8, 0) }, "language"));
            languages.DropDownStyle = ComboBoxStyle.DropDownList; languages.Width = 180;
            languages.Items.AddRange(UiText.Names); languages.SelectedIndex = Array.IndexOf(UiText.Codes, text.Code); toolbar.Controls.Add(languages);
            var help = Bind(new Button { AutoSize = true }, "help");
            help.Click += (s, e) => ShowText(text["helpText"], text["help"]); toolbar.Controls.Add(help);
            details = Bind(new Button { AutoSize = true, Enabled = false }, "diagnostics");
            details.Click += (s, e) => ShowText(diagnostic ?? "", text["diagnostics"]); toolbar.Controls.Add(details); Add(toolbar);
            Add(PathRow("game", game, true)); Add(Wrap("mainQuestion"));
            var install = ActionButton("install", Install);
            install.BackColor = Color.FromArgb(32, 92, 144); install.ForeColor = Color.White; Add(install);
            Add(Wrap("embedded"));
            var remove = ActionButton("export", Disable); remove.MinimumSize = new Size(0, 64); Add(remove);
            var warning = Wrap("exportHint"); warning.ForeColor = Color.FromArgb(140, 75, 0); Add(warning);
            loadStatus.AutoSize = true; loadStatus.Margin = new Padding(0, 8, 0, 8); wrappingLabels.Add(loadStatus); Add(loadStatus);
            advancedToggle = Bind(new Button { AutoSize = true, Dock = DockStyle.Fill, MinimumSize = new Size(0, 34) }, "advancedOpen");
            advancedToggle.Click += (s, e) => SetAdvanced(!advancedOpen); Add(advancedToggle);
            advanced = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Padding = new Padding(0, 10, 0, 10), Visible = false };
            advanced.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Add(advanced);
            AddAdvanced(PathRow("profile", profile, false));
            AddAdvanced(Wrap("historyLabel")); history.DropDownStyle = ComboBoxStyle.DropDownList; history.Dock = DockStyle.Fill; AddAdvanced(history);
            undoButton = ActionButton("undo", Restore); AddAdvanced(undoButton);
            AddAdvanced(ActionButton("native", () =>
            {
                if (!Confirm(text["nativeAsk"])) return;
                Run(() => Success("nativeDone", operations.PrepareNativeKeys(game.Text, profile.Text)));
            }));
            report.Multiline = true; report.ReadOnly = true; report.ScrollBars = ScrollBars.Vertical;
            report.Dock = DockStyle.Fill; report.Height = 132; report.BackColor = Color.White; Add(report);
            languages.SelectedIndexChanged += (s, e) => { if (languages.SelectedIndex >= 0 && UiText.Codes[languages.SelectedIndex] != text.Code) SelectLanguage(UiText.Codes[languages.SelectedIndex]); };
            layout.SizeChanged += (s, e) => UpdateWrap();
            Shown += (s, e) => RefreshHistory();
            game.Leave += (s, e) => { lastStatus = null; RefreshHistory(); RenderReport(); };
            profile.Leave += (s, e) => RefreshHistory();
            if (!preview) { statusTimer.Tick += (s, e) => CheckLoad(); statusTimer.Start(); }
            FormClosed += (s, e) => statusTimer.Dispose();
            SelectLanguage(text.Code);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) statusTimer.Dispose();
            base.Dispose(disposing);
            if (disposing) { foreach (var font in uiFonts.Values) font.Dispose(); uiFonts.Clear(); pathFont.Dispose(); }
        }
        private void Add(Control control) { layout.Controls.Add(control, 0, layout.RowCount++); }
        private void AddAdvanced(Control control) { advanced.Controls.Add(control, 0, advanced.RowCount++); }
        private void SetAdvanced(bool open)
        {
            advancedOpen = open; advanced.Visible = open;
            captions[advancedToggle] = open ? "advancedClose" : "advancedOpen";
            advancedToggle.Text = text[captions[advancedToggle]]; UpdateWrap(); PerformLayout();
        }
        private T Bind<T>(T control, string key) where T : Control { captions.Add(control, key); control.Text = text[key]; return control; }
        private Label Wrap(string key)
        {
            var label = Bind(new Label { AutoSize = true, Margin = new Padding(0, 5, 0, 10) }, key);
            wrappingLabels.Add(label); return label;
        }
        private void UpdateWrap()
        {
            int width = Math.Max(180, layout.ClientSize.Width - layout.Padding.Horizontal - 8);
            foreach (var label in wrappingLabels) label.MaximumSize = new Size(width, 0);
        }
        internal void SelectLanguage(string code)
        {
            text = new UiText(code);
            // Explicit CJK UI fonts; Windows provides fallback if a family is unavailable.
            string family = code == "ko" ? "Malgun Gothic" : code == "ja" ? "Yu Gothic UI" : code == "zh-Hans" ? "Microsoft YaHei UI" : code == "zh-Hant" ? "Microsoft JhengHei UI" : "Segoe UI";
            // WinForms may keep an equal Font instead of assigning the new instance.
            // Keep each family alive until the form closes, including repeated switches.
            Font selectedFont;
            if (!uiFonts.TryGetValue(family, out selectedFont)) uiFonts.Add(family, selectedFont = new Font(family, 10));
            Font = selectedFont;
            Text = text["title"];
            foreach (var pair in captions) pair.Key.Text = (pair.Key.Tag as string ?? "") + text[pair.Value];
            var selected = history.SelectedItem as HistoryItem;
            history.BeginUpdate();
            var records = history.Items.Cast<HistoryItem>().Select(x => x.Record).ToArray(); history.Items.Clear();
            foreach (var record in records) history.Items.Add(new HistoryItem(record, text));
            if (selected != null) history.SelectedIndex = Array.FindIndex(records, r => r.Id == selected.Record.Id);
            history.EndUpdate();
            var index = Array.IndexOf(UiText.Codes, text.Code);
            if (languages.SelectedIndex != index) languages.SelectedIndex = index;
            RenderReport(); UpdateWrap(); PerformLayout();
        }
        internal void ValidatePreview()
        {
            if (Font.Height <= 0) throw new InvalidOperationException("The active UI font must remain usable after language switching.");
            if (actions.Count(x => x.Parent == layout) != 2) throw new InvalidOperationException("Only install and removal belong on the main screen.");
            foreach (var pair in captions)
            {
                if (pair.Key.Text != (pair.Key.Tag as string ?? "") + text[pair.Value]) throw new InvalidOperationException("Stale UI translation: " + pair.Value);
                if (pair.Key is Button && pair.Key.Visible)
                {
                    var needed = TextRenderer.MeasureText(pair.Key.Text, pair.Key.Font);
                    if (needed.Width + pair.Key.Padding.Horizontal > pair.Key.ClientSize.Width + 4) throw new InvalidOperationException("Clipped button: " + text.Code + "/" + pair.Value);
                }
            }
            foreach (var label in wrappingLabels)
                if (label.Visible && label.Height < label.GetPreferredSize(new Size(label.MaximumSize.Width, 0)).Height - 2)
                    throw new InvalidOperationException("Clipped label: " + text.Code + "/" + label.Text);
        }
        private Control PathRow(string key, TextBox input, bool isGame)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 2, RowCount = 2, Margin = new Padding(0, 0, 0, 8) };
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 44)); row.RowStyles.Add(new RowStyle(SizeType.Percent, 56));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            row.Controls.Add(Bind(new Label { AutoSize = true }, key), 0, 0);
            input.Dock = DockStyle.Fill; row.Controls.Add(input, 0, 1);
            var browse = Bind(new Button { AutoSize = true, Dock = DockStyle.Fill }, "browse");
            browse.Click += (s, e) =>
            {
                if (preview) return;
                if (isGame)
                {
                    using (var dialog = new OpenFileDialog { Filter = "Dungeon Settlers|DungeonSettlers.exe", Title = text["gamePicker"] })
                        if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = Path.GetDirectoryName(dialog.FileName);
                }
                else using (var dialog = new FolderBrowserDialog { Description = text["profilePicker"], SelectedPath = input.Text })
                    if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.SelectedPath;
                lastStatus = null; RefreshHistory(); RenderReport();
            };
            row.Controls.Add(browse, 1, 1); return row;
        }
        private Button ActionButton(string key, Action action)
        {
            var button = Bind(new Button { AutoSize = true, MinimumSize = new Size(0, 42), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 3, 10, 3), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 3, 0, 3) }, key);
            button.Click += (s, e) => { if (!preview) action(); }; actions.Add(button); return button;
        }
        private DialogResult Prompt(string body, string title, params KeyValuePair<DialogResult, string>[] choices)
        {
            using (var dialog = new Form { Text = title, Font = Font, AutoScaleMode = AutoScaleMode.Dpi, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, ClientSize = new Size(690, 460), MinimumSize = new Size(500, 340) })
            {
                dialog.Size = new Size(Math.Min(dialog.Width, Screen.FromControl(this).WorkingArea.Width), Math.Min(dialog.Height, Screen.FromControl(this).WorkingArea.Height));
                var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(12), WrapContents = true };
                foreach (var choice in choices.Reverse())
                {
                    var button = new Button { Text = text[choice.Value], AutoSize = true, MinimumSize = new Size(90, 34), DialogResult = choice.Key };
                    buttons.Controls.Add(button);
                    if (choice.Key == DialogResult.Cancel) dialog.CancelButton = button;
                    if (choice.Key == DialogResult.OK && choices.Length == 1) dialog.AcceptButton = button;
                }
                // Destructive/restore confirmations do not assign an Enter-key default.
                var content = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Text = body.Replace("\r\n", "\n").Replace("\n", "\r\n"), BackColor = Color.White };
                var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) }; panel.Controls.Add(content); dialog.Controls.Add(panel); dialog.Controls.Add(buttons);
                return dialog.ShowDialog(this);
            }
        }
        private static KeyValuePair<DialogResult, string> Choice(DialogResult result, string key) { return new KeyValuePair<DialogResult, string>(result, key); }
        private bool Confirm(string body) { return Prompt(body, text["confirm"], Choice(DialogResult.OK, "ok"), Choice(DialogResult.Cancel, "cancel")) == DialogResult.OK; }
        private void ShowText(string body, string title) { Prompt(body, title, Choice(DialogResult.OK, "ok")); }
        private void SetReport(string key, params object[] args) { reportKey = key; reportArgs = args; reportError = null; diagnostic = null; lastBackup = null; RenderReport(); }
        private void Success(string key, string backup, params object[] args) { SetReport(key, args); lastBackup = backup; RenderReport(); }
        private void Error(Exception ex, string key = "notDone")
        {
            reportError = ex; reportKey = key; diagnostic = ex.ToString(); lastBackup = null; RenderReport();
        }
        private void RenderReport()
        {
            report.Text = (reportError == null ? text.Format(reportKey, reportArgs) : text.Format(reportKey, text.Error(reportError))).Replace("\n", "\r\n");
            if (lastBackup != null) report.AppendText("\r\n" + text.Format("backup", lastBackup));
            details.Enabled = diagnostic != null;
            loadStatus.Text = text.Format("status", text[lastStatus.HasValue ? lastStatus.Value.ToString() : "unchecked"]);
        }
        private void Run(Action action)
        {
            foreach (var button in actions) button.Enabled = false; languages.Enabled = false; UseWaitCursor = true;
            try { action(); }
            catch (Exception ex) { Error(ex); ShowText(report.Text, text["title"]); }
            finally { UseWaitCursor = false; foreach (var button in actions) button.Enabled = true; languages.Enabled = true; RefreshHistory(); }
        }
        private void Install()
        {
            if (Confirm(text.Format("installAsk", game.Text)))
                Run(() => { var backup = operations.Install(game.Text, profile.Text, ReleaseInfo.Package()); Success(backup == null ? "alreadyCurrent" : "installDone", backup); });
        }
        private void Restore()
        {
            var selected = history.SelectedItem as HistoryItem;
            if (selected == null) { ShowText(text["noHistory"], text["confirm"]); return; }
            if (Confirm(text.Format("undoAsk", selected)))
                Run(() => { var backup = operations.Restore(game.Text, profile.Text, selected.Record.Id); Success(backup == null ? "nothingToUndo" : "undoDone", backup); });
        }
        private void RefreshHistory()
        {
            if (preview || string.IsNullOrWhiteSpace(game.Text) || string.IsNullOrWhiteSpace(profile.Text)) return;
            try
            {
                history.Items.Clear();
                foreach (var record in operations.History(game.Text, profile.Text).Where(Operations.CanUndo)) history.Items.Add(new HistoryItem(record, text));
                if (history.Items.Count > 0) history.SelectedIndex = 0;
                undoButton.Enabled = history.Items.Count > 0;
                if (history.Items.Cast<HistoryItem>().Any(x => x.Record.Status == "prepared" || x.Record.Status == "recovery-needed")) SetAdvanced(true);
            }
            catch (Exception ex) { if (reportError == null) Error(ex, "historyError"); }
        }
        private void CheckLoad()
        {
            if (string.IsNullOrWhiteSpace(game.Text)) return;
            var state = LoadStatus.Read(game.Text); lastStatus = state;
            loadStatus.Text = text.Format("status", text[state.ToString()]);
            if (state == HotbarStatus.Active) { inactiveNotice = false; offeredNativeKeys = false; }
            if (state == HotbarStatus.Inactive && !inactiveNotice)
            {
                inactiveNotice = true; offeredNativeKeys = false; inactiveGame = game.Text; inactiveProfile = profile.Text; SetReport("inactive");
            }
            if (state == HotbarStatus.Stopped && inactiveNotice && !offeredNativeKeys && game.Text == inactiveGame && profile.Text == inactiveProfile && ActiveForm == this)
            {
                offeredNativeKeys = true;
                if (Confirm(text["offerNative"])) Run(() => Success("nativeDone", operations.PrepareNativeKeys(game.Text, profile.Text)));
            }
        }
        private void Disable()
        {
            using (var dialog = new RemovalDialog(text, Font, profile.Text))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Run(() => { string target; var backup = operations.Disable(game.Text, profile.Text, dialog.SavePath, dialog.Bindings, out target, text["saveSuffix"]); Success("exportDone", backup, target); });
            }
        }
        internal void PreviewStates(string path)
        {
            if (!preview) throw new InvalidOperationException("Preview mode required.");
            if (advancedOpen) throw new InvalidOperationException("Advanced tools must start collapsed.");
            ValidatePreview(); Snapshot(this, path);
            SetAdvanced(true); Application.DoEvents(); ValidatePreview();
            Snapshot(this, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-advanced.png"));
            var originalSize = Size; Size = MinimumSize; Application.DoEvents(); ValidatePreview();
            Size = originalSize;
            SetAdvanced(false); Application.DoEvents();
            using (var dialog = new RemovalDialog(text, Font, profile.Text, true))
            {
                dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-20000, -20000);
                dialog.Show(); Application.DoEvents(); dialog.ValidatePreview(); Application.DoEvents();
                Snapshot(dialog, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-remove.png"));
                dialog.Size = dialog.MinimumSize; Application.DoEvents(); dialog.ValidatePreview();
            }
        }
        private static void Snapshot(Form window, string path)
        {
            using (var bitmap = new Bitmap(window.Width, window.Height))
            { window.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(path); }
        }
        private sealed class HistoryItem
        {
            internal readonly TransactionRecord Record;
            private readonly UiText text;
            internal HistoryItem(TransactionRecord record, UiText text) { Record = record; this.text = text; }
            public override string ToString()
            {
                DateTime time; return (DateTime.TryParse(Record.Time, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out time) ? time.ToLocalTime().ToString("g", new CultureInfo(text.Code)) : Record.Time)
                    + " · " + text.HistoryAction(Record.Action) + (Record.Status == "complete" ? "" : " · " + text["recovery"]);
            }
        }
        private static IEnumerable<string> FindGames()
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var steam = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
                if (string.IsNullOrEmpty(steam)) steam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
                var libraries = new List<string> { steam };
                var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdf)) foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"((?:\\\\.|[^\"])*)\"")) libraries.Add(match.Groups[1].Value.Replace(@"\\", @"\"));
                foreach (var library in libraries)
                {
                    var candidate = Path.Combine(library, "steamapps", "common", "Dungeon Settlers");
                    if (File.Exists(Path.Combine(candidate, "DungeonSettlers.exe"))) result.Add(candidate);
                }
            }
            catch { /* Manual selection remains available if Steam metadata is inaccessible. */ }
            return result;
        }
    }
}
