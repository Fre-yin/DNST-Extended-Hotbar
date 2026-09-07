using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Extended Hotbar Helper")]
[assembly: System.Reflection.AssemblyProduct("Extended Hotbar Helper")]
[assembly: System.Reflection.AssemblyVersion("0.1.11.0")]
[assembly: System.Reflection.AssemblyFileVersion("0.1.11.0")]

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
                string[] handoff = null;
                if (args.Length == 2 && args[0] == "--update-install") handoff = Updates.ParseHandoff(args[1]);
                else if (args.Length != 0) throw new ArgumentException("Start the helper without command-line arguments.");
                Application.Run(new MainForm(false, null, handoff));
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
        private readonly ComboBox game = new ComboBox { DropDownStyle = ComboBoxStyle.DropDown };
        private readonly TextBox profile = new TextBox(), report = new TextBox();
        private readonly ComboBox history = new ComboBox(), languages = new ComboBox();
        private readonly Label loadStatus = new Label();
        private readonly System.Windows.Forms.Timer statusTimer = new System.Windows.Forms.Timer { Interval = 2000 };
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
        private readonly Label packageStatus = new Label { AutoSize = true, Margin = new Padding(0, 5, 0, 10) };
        private readonly CheckBox automaticUpdates = new CheckBox { AutoSize = true };
        private readonly string preferencesPath;
        private Button checkUpdates;
        private CancellationTokenSource updateCancellation;
        private LocalModPackage updateOffer;
        private LocalHelperPackage helperOffer;
        private bool preparingHelper;
        private string updateState;
        private Button findGame;
        private CancellationTokenSource gameSearch;
        private int gameRevision;

        internal MainForm(bool preview, string language = null, string[] handoff = null)
        {
            this.preview = preview;
            var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrWhiteSpace(userFolder)) userFolder = Environment.GetEnvironmentVariable("USERPROFILE");
            if (string.IsNullOrWhiteSpace(userFolder)) throw new IOException("Cannot determine Windows user folder.");
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(local)) local = Path.Combine(userFolder, "AppData", "Local");
            operations = new Operations(Path.Combine(local, "ExtendedHotbarHelper", "Backups"), new GamePolicy());
            preferencesPath = Path.Combine(local, "ExtendedHotbarHelper", "local-update-preferences.json");
            profile.Text = Path.Combine(userFolder, "AppData", "LocalLow", "CanOpener", "Dungeon Settlers");
            game.Font = pathFont; profile.Font = pathFont;
            game.Text = preview ? @"H:\Steam\steamapps\common\Dungeon Settlers" : "";
            if (handoff != null) { game.Text = handoff[0]; profile.Text = handoff[1]; }
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
            details.Click += (s, e) => ShowText(diagnostic ?? "", text["diagnostics"]); toolbar.Controls.Add(details);
            var preferences = preview ? new UpdatePreferences() : UpdatePreferences.Load(preferencesPath);
            automaticUpdates.Checked = preferences.Automatic;
            automaticUpdates.Margin = new Padding(8, 8, 3, 3);
            toolbar.Controls.Add(Bind(automaticUpdates, "localAutomatic"));
            checkUpdates = Bind(new Button { AutoSize = true, MinimumSize = new Size(0, 30) }, "localCheck");
            checkUpdates.Click += async (s, e) => { if (preview) return; if (updateCancellation != null) updateCancellation.Cancel(); else await CheckUpdates(); };
            toolbar.Controls.Add(checkUpdates); Add(toolbar);
            automaticUpdates.CheckedChanged += (s, e) => SaveUpdatePreferences();
            Add(PathRow("game", game, true)); Add(Wrap("mainQuestion"));
            var install = ActionButton("updateAction", ChooseUpdateAction);
            install.BackColor = Color.FromArgb(32, 92, 144); install.ForeColor = Color.White; Add(install);
            wrappingLabels.Add(packageStatus); Add(packageStatus);
            var remove = ActionButton("export", Disable); remove.MinimumSize = new Size(0, 64); Add(remove);
            var warning = Wrap("exportHint"); warning.ForeColor = Color.FromArgb(140, 75, 0); Add(warning);
            Add(ActionButton("characters", ConfigureCharacters));
            loadStatus.AutoSize = true; loadStatus.Margin = new Padding(0, 8, 0, 8); wrappingLabels.Add(loadStatus); Add(loadStatus);
            advancedToggle = Bind(new Button { AutoSize = true, Dock = DockStyle.Fill, MinimumSize = new Size(0, 34) }, "advancedOpen");
            advancedToggle.Click += (s, e) => SetAdvanced(!advancedOpen); Add(advancedToggle);
            advanced = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 1, Padding = new Padding(0, 10, 0, 10), Visible = false };
            advanced.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); Add(advanced);
            AddAdvanced(PathRow("profile", profile, false));
            AddAdvanced(ActionButton("savePackage", SavePackage));
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
            Shown += async (s, e) =>
            {
                RefreshHistory();
                if (preview) return;
                if (handoff != null) { BeginInvoke(new Action(Install)); return; } // Normal confirmation, never silent installation.
                if (await FindGameFolders() && !IsDisposed && automaticUpdates.Checked) await CheckUpdates();
            };
            game.TextChanged += (s, e) =>
            {
                gameRevision++; updateOffer = null; updateState = null; lastStatus = null;
                if (reportError == null && (reportKey == "gameMany" || reportKey == "gameNone") && !string.IsNullOrWhiteSpace(game.Text))
                    SetReport("ready");
                RenderUpdate();
            };
            game.Leave += (s, e) => { updateOffer = null; updateState = null; RenderUpdate(); lastStatus = null; RefreshHistory(); RenderReport(); };
            profile.Leave += (s, e) => RefreshHistory();
            if (!preview) { statusTimer.Tick += (s, e) => CheckLoad(); statusTimer.Start(); }
            FormClosed += (s, e) => { statusTimer.Dispose(); CancelGameSearch(); if (updateCancellation != null) updateCancellation.Cancel(); };
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
            RenderReport(); RenderUpdate(); UpdateWrap(); PerformLayout();
        }
        internal void ValidatePreview()
        {
            if (Font.Height <= 0) throw new InvalidOperationException("The active UI font must remain usable after language switching.");
            if (actions.Count(x => x.Parent == layout) != 3 || !actions.Any(x => x.Parent == layout && captions[x] == "characters" && x.Visible))
                throw new InvalidOperationException("Install, removal and character bindings must remain visible on the main screen.");
            if (!automaticUpdates.Visible || !checkUpdates.Visible || automaticUpdates.Parent == advanced || checkUpdates.Parent == advanced)
                throw new InvalidOperationException("Local update controls must remain visible outside the help area.");
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
        private Control PathRow(string key, Control input, bool isGame)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = isGame ? 3 : 2, RowCount = 2, Margin = new Padding(0, 0, 0, 8) };
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
                    CancelGameSearch();
                    using (var dialog = new OpenFileDialog { Filter = "Dungeon Settlers|DungeonSettlers.exe", Title = text["gamePicker"] })
                        if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = Path.GetDirectoryName(dialog.FileName);
                }
                else using (var dialog = new FolderBrowserDialog { Description = text["profilePicker"], SelectedPath = input.Text })
                    if (dialog.ShowDialog(this) == DialogResult.OK) input.Text = dialog.SelectedPath;
                lastStatus = null; RefreshHistory(); RenderReport();
            };
            if (isGame)
            {
                row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                findGame = Bind(new Button { AutoSize = true, Dock = DockStyle.Fill }, "gameSearch");
                findGame.Click += async (s, e) => { if (preview) return; if (gameSearch != null) CancelGameSearch(); else await FindGameFolders(); };
                row.Controls.Add(findGame, 1, 1);
                game.DropDown += (s, e) => game.DropDownWidth = Math.Min(1100, Math.Max(game.Width,
                    game.Items.Cast<string>().Select(x => TextRenderer.MeasureText(x, game.Font).Width + 35).DefaultIfEmpty(game.Width).Max()));
            }
            row.Controls.Add(browse, isGame ? 2 : 1, 1); return row;
        }
        private void CancelGameSearch() { if (gameSearch != null) gameSearch.Cancel(); }
        private async Task<bool> FindGameFolders()
        {
            if (preview || gameSearch != null || updateCancellation != null || preparingHelper) return false;
            var cancellation = new CancellationTokenSource(); gameSearch = cancellation;
            var revision = gameRevision;
            captions[findGame] = "cancel"; findGame.Text = text["cancel"];
            try
            {
                var results = await Task.Run(() => GameDiscovery.FindSystem(cancellation.Token));
                if (IsDisposed || cancellation.IsCancellationRequested) return false;
                var unchanged = gameRevision == revision;
                var selected = GameDiscovery.InitialChoice(results, game.Text, unchanged);
                game.BeginUpdate();
                try { game.Items.Clear(); game.Items.AddRange(results); game.Text = selected; }
                finally { game.EndUpdate(); }
                if (unchanged && string.IsNullOrWhiteSpace(selected)) SetReport(results.Length > 1 ? "gameMany" : "gameNone");
                lastStatus = null; RefreshHistory(); RenderReport();
                if (unchanged && string.IsNullOrWhiteSpace(selected) && results.Length > 1 && ActiveForm == this)
                { game.Focus(); game.DroppedDown = true; }
                return true;
            }
            catch (OperationCanceledException) { return false; }
            catch (Exception ex) { if (!IsDisposed) Error(ex); return false; }
            finally
            {
                gameSearch = null; cancellation.Dispose();
                if (!IsDisposed) { captions[findGame] = "gameSearch"; findGame.Text = text["gameSearch"]; }
            }
        }
        private Button ActionButton(string key, Action action)
        {
            var button = Bind(new Button { AutoSize = true, MinimumSize = new Size(0, 42), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 3, 10, 3), FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 3, 0, 3) }, key);
            button.Click += (s, e) => { if (!preview) { CancelGameSearch(); action(); } }; actions.Add(button); return button;
        }
        private DialogResult Prompt(string body, string title, params KeyValuePair<DialogResult, string>[] choices)
        {
            using (var dialog = CreatePrompt(body, title, choices)) return dialog.ShowDialog(this);
        }
        private Form CreatePrompt(string body, string title, params KeyValuePair<DialogResult, string>[] choices)
        {
            var dialog = new Form { Text = title, Font = Font, AutoScaleMode = AutoScaleMode.Dpi, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false, ClientSize = new Size(690, 460), MinimumSize = new Size(500, 340) };
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
            return dialog;
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
        private void ChooseUpdateAction()
        {
            if (updateCancellation != null || preview) return;
            var choice = Prompt(text["updateActionAsk"], text["updateAction"],
                Choice(DialogResult.Yes, "updateMod"), Choice(DialogResult.No, "helperOnly"), Choice(DialogResult.Cancel, "cancel"));
            if (choice == DialogResult.Yes) Install();
            else if (choice == DialogResult.No) UpdateHelperOnly();
        }
        private async void UpdateHelperOnly()
        {
            if (!await CheckUpdates(helperOnly: true)) return;
            if (helperOffer == null) { SetReport("localHelperNone"); return; }
            await OpenLocalHelper(helperOnly: true);
        }
        private async void Install()
        {
            if (!await CheckUpdates())
            {
                if (!IsDisposed && updateCancellation == null && updateState == "updateUnavailable"
                    && Prompt(report.Text, text["localCheck"], Choice(DialogResult.No, "updateUseIncluded"), Choice(DialogResult.Cancel, "cancel")) == DialogResult.No)
                    InstallBundled();
                return;
            }
            if (helperOffer != null) { await OpenLocalHelper(helperOnly: false); return; }
            var offer = updateOffer;
            if (offer == null) { InstallBundled(); return; }
            var choice = Prompt(text.Format("localAsk", offer.Version.ToString(3), offer.Path, game.Text), text["localCheck"],
                Choice(DialogResult.Yes, "localInstall"), Choice(DialogResult.No, "updateUseIncluded"), Choice(DialogResult.Cancel, "cancel"));
            if (choice == DialogResult.No) { InstallBundled(); return; }
            if (choice != DialogResult.Yes) return;
            Run(() => {
                if (LocalMods.Installed(game.Text) > offer.Version) throw new HelperFailure("errorLocalOlder", "A newer mod is already installed.");
                var payload = LocalMods.Recheck(offer, CancellationToken.None);
                var backup = operations.Install(game.Text, profile.Text, payload);
                Success(backup == null ? "alreadyCurrent" : "installDone", backup);
            });
        }
        private void InstallBundled()
        {
            if (Confirm(text.Format("installAsk", game.Text)))
                Run(() => {
                    if (LocalMods.Installed(game.Text) > Updates.ParseVersion(ReleaseInfo.ModVersion))
                        throw new HelperFailure("errorLocalOlder", "Bundled mod would downgrade the installed mod.");
                    var backup = operations.Install(game.Text, profile.Text, ReleaseInfo.Package());
                    Success(backup == null ? "alreadyCurrent" : "installDone", backup);
                });
        }
        private void RenderUpdate()
        {
            var notice = LocalUpdates.NoticeKey(updateState);
            packageStatus.Text = text["embedded"] + (notice == null ? "" : "  ·  " + text[notice]);
            packageStatus.ForeColor = notice != null ? Color.FromArgb(25, 110, 50) : Color.Black;
            captions[checkUpdates] = updateCancellation == null ? "localCheck" : "cancel";
            checkUpdates.Text = text[captions[checkUpdates]]; UpdateWrap();
        }
        private void SaveUpdatePreferences()
        {
            if (preview) return;
            try { new UpdatePreferences { Automatic = automaticUpdates.Checked, IncludeTests = false }.Save(preferencesPath); }
            catch (Exception ex) { Error(ex); }
        }
        private async Task<bool> CheckUpdates(bool helperOnly = false)
        {
            if (updateCancellation != null || preview) return false;
            var selectedGame = game.Text;
            var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30)); updateCancellation = cancellation;
            updateOffer = null; helperOffer = null; updateState = "localChecking"; RenderUpdate();
            try
            {
                var result = await Task.Run(() => LocalUpdates.Scan(LocalMods.Downloads(), selectedGame, helperOnly, cancellation.Token));
                if (IsDisposed) return false;
                cancellation.Token.ThrowIfCancellationRequested();
                if (!helperOnly && game.Text != selectedGame) { updateState = null; return false; }
                if (result.Helper != null) { helperOffer = result.Helper; updateState = "localHelperFound"; return true; }
                updateOffer = result.Mod;
                updateState = helperOnly ? "localHelperNone" : result.Mod == null ? "localNone" : result.Mod.Version > result.Baseline ? "updateAvailable" : "localFound";
                return true;
            }
            catch (OperationCanceledException) { if (!IsDisposed) updateState = "updateCancelled"; return false; }
            catch (Exception ex) { if (!IsDisposed) { updateState = "updateUnavailable"; Error(ex); } return false; }
            finally
            {
                updateCancellation = null; cancellation.Dispose();
                if (!IsDisposed) RenderUpdate();
            }
        }
        private static KeyValuePair<DialogResult, string>[] HelperUpdateChoices(bool helperOnly)
        {
            return helperOnly
                ? new[] { Choice(DialogResult.Yes, "localHelperOpen"), Choice(DialogResult.Cancel, "cancel") }
                : new[] { Choice(DialogResult.Yes, "localHelperOpen"), Choice(DialogResult.No, "updateUseIncluded"), Choice(DialogResult.Cancel, "cancel") };
        }
        private async Task OpenLocalHelper(bool helperOnly)
        {
            var selected = helperOffer;
            if (selected == null || updateCancellation != null || preview) return;
            var choice = Prompt(text.Format(helperOnly ? "localHelperOnlyAsk" : "localHelperAsk", selected.Offer.Version.ToString(3), selected.Path), text["localCheck"], HelperUpdateChoices(helperOnly));
            if (!helperOnly && choice == DialogResult.No) { InstallBundled(); return; }
            if (choice != DialogResult.Yes) return;
            string arguments;
            try {
                if (!helperOnly) new GamePolicy().Stopped();
                arguments = LocalUpdates.StartArguments(helperOnly, game.Text, profile.Text);
            } catch (Exception ex) { Error(ex); ShowText(report.Text, text["title"]); return; }
            var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2)); updateCancellation = cancellation; preparingHelper = true;
            foreach (var button in actions) button.Enabled = false;
            foreach (var pair in captions.Where(x => x.Value == "browse")) pair.Key.Enabled = false;
            languages.Enabled = game.Enabled = profile.Enabled = automaticUpdates.Enabled = findGame.Enabled = false;
            updateState = "updateDownloading"; SetAdvanced(true); RenderUpdate();
            try {
                var bytes = await Task.Run(() => LocalHelpers.ReadBytes(selected, cancellation.Token));
                string store = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ExtendedHotbarHelper", "Updates");
                var folder = await Task.Run(() => Updates.Stage(bytes, selected.Offer, store, cancellation.Token));
                if (IsDisposed) return;
                cancellation.Token.ThrowIfCancellationRequested();
                if (!helperOnly) new GamePolicy().Stopped();
                Updates.Launch(folder, bytes, selected.Offer, exe => {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = exe, WorkingDirectory = folder, Arguments = arguments, UseShellExecute = true });
                    if (process == null) throw Updates.Invalid("New helper could not be opened.");
                    process.Dispose();
                });
                Close();
            }
            catch (OperationCanceledException) { if (!IsDisposed) updateState = "updateCancelled"; }
            catch (Exception ex) { if (!IsDisposed) { updateState = "updateUnavailable"; Error(ex); ShowText(report.Text, text["title"]); } }
            finally {
                updateCancellation = null; preparingHelper = false; cancellation.Dispose();
                if (!IsDisposed) {
                    foreach (var button in actions) button.Enabled = true;
                    foreach (var pair in captions.Where(x => x.Value == "browse")) pair.Key.Enabled = true;
                    languages.Enabled = game.Enabled = profile.Enabled = automaticUpdates.Enabled = findGame.Enabled = true;
                    RenderUpdate(); RefreshHistory();
                }
            }
        }
        private void SavePackage()
        {
            using (var dialog = new SaveFileDialog { Title = text["savePackage"], Filter = "ZIP|*.zip", DefaultExt = "zip", AddExtension = true,
                FileName = "Extended-Hotbar-" + ReleaseInfo.ModVersion + ".zip", OverwritePrompt = false })
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                Run(() => { ReleaseInfo.ExportPackage(dialog.FileName); SetReport("packageSaved"); });
            }
        }
        private void ConfigureCharacters()
        {
            var choice = Prompt(text["charactersAsk"], text["characters"], Choice(DialogResult.Yes, "charactersShift"), Choice(DialogResult.No, "charactersDigits"), Choice(DialogResult.Cancel, "cancel"));
            if (choice != DialogResult.Yes && choice != DialogResult.No) return;
            Run(() =>
            {
                string expectedHash;
                var conflicts = operations.PreviewCharacterKeys(game.Text, profile.Text, choice == DialogResult.Yes, out expectedHash);
                bool overwrite = conflicts.Count != 0;
                if (overwrite && Prompt(text.Format("charactersOverwriteAsk", string.Join("\n", conflicts.Select(text.CharacterConflict))), text["characters"],
                    Choice(DialogResult.Yes, "charactersOverwrite"), Choice(DialogResult.Cancel, "cancel")) != DialogResult.Yes) return;
                var backup = operations.ConfigureCharacterKeys(game.Text, profile.Text, choice == DialogResult.Yes, overwrite, expectedHash);
                Success(backup == null ? "alreadyCurrent" : "charactersDone", backup);
            });
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
            if (updateCancellation != null || preparingHelper) return;
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
            var originalGame = game.Text;
            game.Items.AddRange(new[] { originalGame, @"J:\DNST-Test\Game" }); game.Text = ""; SetReport("gameMany");
            if (game.SelectedIndex != -1 || game.Items.Count != 2 || game.DropDownStyle != ComboBoxStyle.DropDown || !findGame.Visible)
                throw new InvalidOperationException("Multiple game copies require an editable unselected path list and visible search.");
            Application.DoEvents(); ValidatePreview();
            Snapshot(this, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-game-multiple.png"));
            game.SelectedIndex = 1; Application.DoEvents();
            AssertSelectionReportReady();
            Snapshot(this, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-game-selected.png"));
            foreach (var hint in new[] { "gameMany", "gameNone" })
            {
                game.Text = ""; SetReport(hint); game.Text = "  ";
                if (reportKey != hint) throw new InvalidOperationException("An empty path must not dismiss the discovery prompt.");
                game.Text = originalGame; AssertSelectionReportReady();
            }
            var previewError = new InvalidOperationException("Read-only preview diagnostic.");
            Error(previewError, "gameMany"); game.Text = @"J:\DNST-Test\Game";
            if (reportError != previewError || !details.Enabled)
                throw new InvalidOperationException("Path changes must preserve actual errors and diagnostics.");
            Success("installDone", "preview-backup"); var completedReport = report.Text; game.Text = originalGame;
            if (report.Text != completedReport || lastBackup != "preview-backup")
                throw new InvalidOperationException("Path changes must preserve operation results and backup information.");
            SetReport("ready");
            PreviewPrompt(path, "update-choice", text["updateActionAsk"], Choice(DialogResult.Yes, "updateMod"), Choice(DialogResult.No, "helperOnly"), Choice(DialogResult.Cancel, "cancel"));
            SetAdvanced(true); Application.DoEvents(); ValidatePreview();
            Snapshot(this, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-advanced.png"));
            var originalSize = Size; Size = MinimumSize; Application.DoEvents(); ValidatePreview();
            Size = originalSize;
            SetAdvanced(false); Application.DoEvents();
            PreviewPrompt(path, "characters", text["charactersAsk"], Choice(DialogResult.Yes, "charactersShift"), Choice(DialogResult.No, "charactersDigits"), Choice(DialogResult.Cancel, "cancel"));
            PreviewPrompt(path, "overwrite", text.Format("charactersOverwriteAsk", text.Format("bindingConflictLine", "1", text.Format("bindingSkill", 1), 1)), Choice(DialogResult.Yes, "charactersOverwrite"), Choice(DialogResult.Cancel, "cancel"));
            updateState = "updateAvailable"; RenderUpdate(); Application.DoEvents(); ValidatePreview();
            Snapshot(this, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-update.png"));
            PreviewPrompt(path, "update-confirm", text.Format("localAsk", "0.3.8", @"C:\Users\Player\Downloads\Extended-Hotbar-0.3.8.zip", game.Text), Choice(DialogResult.Yes, "localInstall"), Choice(DialogResult.No, "updateUseIncluded"), Choice(DialogResult.Cancel, "cancel"));
            PreviewPrompt(path, "helper-confirm", text.Format("localHelperAsk", "0.1.12", @"C:\Users\Player\Downloads\Extended-Hotbar-Helper-0.1.12-test.zip"), Choice(DialogResult.Yes, "localHelperOpen"), Choice(DialogResult.No, "updateUseIncluded"), Choice(DialogResult.Cancel, "cancel"));
            if (HelperUpdateChoices(true).Any(x => x.Key == DialogResult.No || x.Value == "updateUseIncluded"))
                throw new InvalidOperationException("Helper-only confirmation must not offer a mod fallback.");
            PreviewPrompt(path, "helper-only-confirm", text.Format("localHelperOnlyAsk", "0.1.12", @"C:\Users\Player\Downloads\Extended-Hotbar-Helper-0.1.12-test.zip"), HelperUpdateChoices(true));
            updateState = null; RenderUpdate();
            using (var dialog = new RemovalDialog(text, Font, profile.Text, true))
            {
                dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-20000, -20000);
                dialog.Show(); Application.DoEvents(); dialog.ValidatePreview(); Application.DoEvents();
                Snapshot(dialog, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-remove.png"));
                dialog.Size = dialog.MinimumSize; Application.DoEvents(); dialog.ValidatePreview();
            }
        }
        private void AssertSelectionReportReady()
        {
            if (reportKey != "ready" || reportError != null || report.Text != text["ready"].Replace("\n", "\r\n"))
                throw new InvalidOperationException("Choosing or entering a game path must dismiss the obsolete discovery prompt.");
        }
        private void PreviewPrompt(string path, string suffix, string body, params KeyValuePair<DialogResult, string>[] choices)
        {
            using (var dialog = CreatePrompt(body, text[suffix == "update-choice" ? "updateAction" : suffix.Contains("confirm") ? "localCheck" : "characters"], choices))
            {
                if (dialog.AcceptButton != null || dialog.CancelButton == null) throw new InvalidOperationException("Character choices need explicit consent and cancellation.");
                dialog.StartPosition = FormStartPosition.Manual; dialog.Location = new Point(-20000, -20000); dialog.Show(); Application.DoEvents();
                foreach (var button in dialog.Controls.OfType<FlowLayoutPanel>().SelectMany(panel => panel.Controls.OfType<Button>()))
                    if (TextRenderer.MeasureText(button.Text, button.Font).Width > button.ClientSize.Width + 4) throw new InvalidOperationException("Character choice clipped: " + text.Code);
                Snapshot(dialog, Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-" + suffix + ".png"));
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
    }
}
