using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ExtendedHotbar.Helper
{
    // Collects choices only. All backup, export and removal work stays in Operations.
    internal sealed class RemovalDialog : Form
    {
        private readonly UiText text;
        private readonly string profile;
        private readonly bool preview;
        private readonly TextBox save = new TextBox(), backup = new TextBox();
        private readonly CheckBox customKeys = new CheckBox();
        private readonly Button submit;
        private readonly TableLayoutPanel backupRow;
        private readonly List<Label> labels = new List<Label>();
        private readonly List<Button> buttons = new List<Button>();
        private readonly TableLayoutPanel layout;
        internal string SavePath { get { return save.Text; } }
        internal string Bindings { get; private set; }
        private bool CanSubmit { get { return save.Text.Length > 0 && (!customKeys.Checked || backup.Text.Length > 0); } }

        internal RemovalDialog(UiText text, Font font, string profile, bool preview = false)
        {
            this.text = text; this.profile = profile; this.preview = preview;
            Text = text["removeTitle"]; Font = font; AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterParent; ShowInTaskbar = false; MinimizeBox = false; MaximizeBox = false;
            ClientSize = new Size(760, 720); MinimumSize = new Size(650, 620); BackColor = Color.FromArgb(246, 247, 249);
            var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Padding = new Padding(22) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); scroll.Controls.Add(layout); Controls.Add(scroll);
            Add(Label(text["saveStep"])); Add(Label(text["noSaveSelected"]));
            Add(FileRow(save, text["chooseSave"], ChooseSave));
            Add(Label(text["summaryStep"])); Add(Label(text.Format("removalSummary", text["saveSuffix"])));
            Add(Label(text["keysHint"]));
            customKeys.Text = text["useOldKeys"]; customKeys.AutoSize = true; customKeys.Margin = new Padding(0, 10, 0, 8); Add(customKeys);
            backupRow = FileRow(backup, text["backupChoice"], ChooseBackup); backupRow.Visible = false; Add(backupRow);
            var warning = Label(text["removalWarning"]); warning.ForeColor = Color.FromArgb(140, 75, 0); Add(warning);
            var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Padding = new Padding(16) };
            var cancel = new Button { Text = text["cancel"], DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(90, 38) };
            submit = new Button { Text = text["removeNow"], AutoSize = true, MinimumSize = new Size(160, 38), Enabled = false };
            buttons.Add(cancel); buttons.Add(submit); footer.Controls.Add(cancel); footer.Controls.Add(submit); Controls.Add(footer);
            CancelButton = cancel; // Enter does not implicitly confirm removal.
            customKeys.CheckedChanged += (s, e) => { backupRow.Visible = customKeys.Checked; UpdateSubmit(); };
            save.TextChanged += (s, e) => UpdateSubmit(); backup.TextChanged += (s, e) => UpdateSubmit();
            submit.Click += (s, e) =>
            {
                if (!CanSubmit) return;
                try
                {
                    // Preview tests never read the live profile. The parent performs no
                    // write until this dialog returns OK and all operation guards pass.
                    Bindings = customKeys.Checked && !preview ? Profiles.Selected(Files.Text(Files.Read(backup.Text))) : Profiles.VanillaDefaults();
                    DialogResult = DialogResult.OK; Close();
                }
                catch (Exception ex) { MessageBox.Show(this, text.Format("notDone", text.Error(ex)), text["removeTitle"], MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            layout.SizeChanged += (s, e) => WrapLabels(); WrapLabels();
        }
        private void Add(Control control) { layout.Controls.Add(control, 0, layout.RowCount++); }
        private Label Label(string caption)
        {
            var label = new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 5, 0, 10) }; labels.Add(label); return label;
        }
        private void WrapLabels()
        {
            int width = Math.Max(180, layout.ClientSize.Width - layout.Padding.Horizontal - 8);
            foreach (var label in labels) label.MaximumSize = new Size(width, 0);
            customKeys.MaximumSize = new Size(width, 0);
        }
        private TableLayoutPanel FileRow(TextBox input, string caption, Action choose)
        {
            var row = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 2, Margin = new Padding(0, 0, 0, 8) };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            input.ReadOnly = true; input.Dock = DockStyle.Fill; input.AccessibleName = caption; row.Controls.Add(input, 0, 0);
            var button = new Button { Text = caption, AutoSize = true, Dock = DockStyle.Fill }; buttons.Add(button);
            button.Click += (s, e) =>
            {
                if (preview) return;
                try { choose(); }
                catch (Exception ex) { MessageBox.Show(this, text.Format("notDone", text.Error(ex)), text["removeTitle"], MessageBoxButtons.OK, MessageBoxIcon.Warning); }
            };
            row.Controls.Add(button, 1, 0); return row;
        }
        private void ChooseSave()
        {
            using (var picker = new OpenFileDialog { Title = text["savePicker"], Filter = text["saveFilter"], InitialDirectory = Files.Under(profile, "Saves"), CheckFileExists = true })
                if (picker.ShowDialog(this) == DialogResult.OK) save.Text = picker.FileName;
        }
        private void ChooseBackup()
        {
            using (var picker = new OpenFileDialog { Title = text["backupPicker"], Filter = text["backupFilter"], CheckFileExists = true })
                if (picker.ShowDialog(this) == DialogResult.OK) backup.Text = picker.FileName;
        }
        private void UpdateSubmit() { submit.Enabled = CanSubmit; }
        internal void ValidatePreview()
        {
            if (!preview) throw new InvalidOperationException("Preview validation is isolated.");
            if (submit.Enabled || AcceptButton != null || customKeys.Checked) throw new InvalidOperationException("Removal must start without an implicit choice.");
            save.Text = @"C:\TEST\Saves\10SlotsTestfile.json";
            if (!submit.Enabled) throw new InvalidOperationException("Selecting a save must enable explicit confirmation.");
            customKeys.Checked = true;
            if (submit.Enabled) throw new InvalidOperationException("Custom keys require a selected backup.");
            backup.Text = @"C:\TEST\UserSetting.json";
            if (!submit.Enabled) throw new InvalidOperationException("Custom selection should be ready.");
            customKeys.Checked = false; backup.Text = ""; save.Text = "";
            if (submit.Enabled || DialogResult != DialogResult.None || Bindings != null) throw new InvalidOperationException("Choice changes must never execute an operation.");
            PerformLayout();
            foreach (var label in labels)
                if (label.Height < label.GetPreferredSize(new Size(label.MaximumSize.Width, 0)).Height - 2) throw new InvalidOperationException("Removal label clipped: " + text.Code);
            foreach (var button in buttons)
                if (button.Visible && TextRenderer.MeasureText(button.Text, button.Font).Width > button.ClientSize.Width + 4) throw new InvalidOperationException("Removal button clipped: " + text.Code);
        }
    }
}
