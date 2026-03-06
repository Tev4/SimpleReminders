using System;
using System.Drawing;
using System.Windows.Forms;
using SimpleReminders.Models;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class SettingsForm : Form
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;

        private Button _bgColorBtn = null!;
        private Button _resetBgColorBtn = null!;
        private Button _fontColorBtn = null!;
        private Button _resetFontColorBtn = null!;
        private NumericUpDown _fontSizeNum = null!;
        private Button _resetFontSizeBtn = null!;
        private ComboBox _fontFamilyCombo = null!;
        private Button _resetFontBtn = null!;
        private NumericUpDown _widthNum = null!;
        private NumericUpDown _heightNum = null!;
        private Button _resetSizeBtn = null!;
        private Button _soundBtn = null!;
        private Button _resetSoundBtn = null!;
        private Label _soundLabel = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _restoreDefaultsBtn = null!;

        public SettingsForm(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = settingsService.Settings;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Default Reminder Settings";
            this.Icon = IconService.AppIcon;
            this.Size = new Size(420, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(15);
            layout.RowCount = 8;
            layout.ColumnCount = 2;
            for (int i = 0; i < 6; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.AutoSize = true;

            // Background Color
            layout.Controls.Add(new Label { Text = "Background Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 0);
            var bgPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _bgColorBtn = new Button { Text = "", Width = 60 };
            _bgColorBtn.Click += (s, e) => PickColor(_bgColorBtn, true);
            _resetBgColorBtn = CreateResetButton();
            _resetBgColorBtn.Click += (s, e) => {
                _bgColorBtn.BackColor = ColorTranslator.FromHtml("#005FB8");
                _bgColorBtn.Focus();
                UpdateResetButtonVisibilities();
            };
            bgPanel.Controls.Add(_bgColorBtn);
            bgPanel.Controls.Add(_resetBgColorBtn);
            layout.Controls.Add(bgPanel, 1, 0);

            // Text Color
            layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 1);
            var fgPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _fontColorBtn = new Button { Text = "", Width = 60 };
            _fontColorBtn.Click += (s, e) => PickColor(_fontColorBtn, false);
            _resetFontColorBtn = CreateResetButton();
            _resetFontColorBtn.Click += (s, e) => {
                _fontColorBtn.BackColor = ColorTranslator.FromHtml("#FFFFFF");
                _fontColorBtn.Focus();
                UpdateResetButtonVisibilities();
            };
            fgPanel.Controls.Add(_fontColorBtn);
            fgPanel.Controls.Add(_resetFontColorBtn);
            layout.Controls.Add(fgPanel, 1, 1);

            // Font Size
            layout.Controls.Add(new Label { Text = "Font Size:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 2);
            var fontSizePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _fontSizeNum = new NumericUpDown { Minimum = 8, Maximum = 72, Width = 60 };
            _fontSizeNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetFontSizeBtn = CreateResetButton();
            _resetFontSizeBtn.Click += (s, e) => {
                _fontSizeNum.Value = 14;
                _fontSizeNum.Focus();
            };
            fontSizePanel.Controls.Add(_fontSizeNum);
            fontSizePanel.Controls.Add(_resetFontSizeBtn);
            layout.Controls.Add(fontSizePanel, 1, 2);

            // Font Family
            layout.Controls.Add(new Label { Text = "Font Family:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 3);
            var fontPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left };
            _fontFamilyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 180 };
            _resetFontBtn = new Button { 
                Text = "✕", 
                Width = 25, 
                Height = 25,
                FlatStyle = FlatStyle.Flat, 
                ForeColor = Color.Red,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 0),
                TabStop = false
            };
            _resetFontBtn.FlatAppearance.BorderSize = 0;
            _resetFontBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _resetFontBtn.Click += (s, e) => {
                _fontFamilyCombo.SelectedIndex = _fontFamilyCombo.FindStringExact("Segoe UI Variable Display");
                if (_fontFamilyCombo.SelectedIndex < 0) _fontFamilyCombo.SelectedIndex = 0;
                _fontFamilyCombo.Focus();
            };

            // Populate fonts
            foreach (var family in FontFamily.Families)
            {
                _fontFamilyCombo.Items.Add(family.Name);
            }
            _fontFamilyCombo.SelectedIndexChanged += (s, e) => {
                _resetFontBtn.Visible = _fontFamilyCombo.SelectedItem?.ToString() != "Segoe UI Variable Display";
            };

            fontPanel.Controls.Add(_fontFamilyCombo);
            fontPanel.Controls.Add(_resetFontBtn);
            layout.Controls.Add(fontPanel, 1, 3);

            // Size
            layout.Controls.Add(new Label { Text = "Notification Size (W x H):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 4);
            var sizePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _widthNum = new NumericUpDown { Minimum = 100, Maximum = 1000, Width = 60 };
            _heightNum = new NumericUpDown { Minimum = 40, Maximum = 1000, Width = 60 };
            _widthNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _heightNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetSizeBtn = CreateResetButton();
            _resetSizeBtn.Click += (s, e) => {
                _widthNum.Value = 250;
                _heightNum.Value = 80;
                _widthNum.Focus();
            };
            sizePanel.Controls.Add(_widthNum);
            sizePanel.Controls.Add(new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 5, 0, 0) });
            sizePanel.Controls.Add(_heightNum);
            sizePanel.Controls.Add(_resetSizeBtn);
            layout.Controls.Add(sizePanel, 1, 4);

            // Sound
            layout.Controls.Add(new Label { Text = "Notification Sound:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 5);
            var soundPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left };
            _soundBtn = new Button { Text = "Browse", Width = 80 };
            _soundBtn.Click += (s, e) => PickSound();
            _resetSoundBtn = new Button { 
                Text = "✕", 
                Width = 25, 
                Height = 25,
                FlatStyle = FlatStyle.Flat, 
                ForeColor = Color.Red,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 0),
                TabStop = false
            };
            _resetSoundBtn.FlatAppearance.BorderSize = 0;
            _resetSoundBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            _resetSoundBtn.Click += (s, e) => {
                _settings.DefaultSoundPath = string.Empty;
                UpdateSoundLabel();
                _soundBtn.Focus();
            };
            _soundLabel = new Label { Text = "System Default", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left };
            soundPanel.Controls.Add(_soundBtn);
            soundPanel.Controls.Add(_resetSoundBtn);
            soundPanel.Controls.Add(_soundLabel);
            layout.Controls.Add(soundPanel, 1, 5);

            // Buttons
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 0, 10, 10) };
            _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };
            _restoreDefaultsBtn = new Button { Text = "Reset All to Defaults", Width = 140 };
            
            _restoreDefaultsBtn.Click += (s, e) => RestoreDefaults();

            _saveButton.Click += (s, e) =>
            {
                SaveData();
                this.Close();
            };

            btnPanel.Controls.Add(_cancelButton);
            btnPanel.Controls.Add(_saveButton);
            btnPanel.Controls.Add(_restoreDefaultsBtn);

            this.Controls.Add(btnPanel);
            this.Controls.Add(layout);
        }

        private void LoadData()
        {
            _bgColorBtn.BackColor = ColorTranslator.FromHtml(_settings.DefaultBackgroundColor);
            _fontColorBtn.BackColor = ColorTranslator.FromHtml(_settings.DefaultFontColor);
            _fontSizeNum.Value = (decimal)_settings.DefaultFontSize;
            _widthNum.Value = _settings.DefaultWidth;
            _heightNum.Value = _settings.DefaultHeight;

            int index = _fontFamilyCombo.FindStringExact(_settings.DefaultFontFamily);
            _fontFamilyCombo.SelectedIndex = index >= 0 ? index : 0;

            UpdateResetButtonVisibilities();
        }

        private void UpdateSoundLabel()
        {
            bool hasCustomSound = !string.IsNullOrEmpty(_settings.DefaultSoundPath);
            _soundLabel.Text = hasCustomSound ? System.IO.Path.GetFileName(_settings.DefaultSoundPath) : "System Default";
            _resetSoundBtn.Visible = hasCustomSound;
        }

        private void PickColor(Button btn, bool isBg)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = btn.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    btn.BackColor = cd.Color;
                }
                UpdateResetButtonVisibilities();
            }
        }

        private Button CreateResetButton()
        {
            var btn = new Button { 
                Text = "✕", 
                Width = 25, 
                Height = 25,
                FlatStyle = FlatStyle.Flat, 
                ForeColor = Color.Red,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 3, 0, 0),
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            return btn;
        }

        private void UpdateResetButtonVisibilities()
        {
            if (_bgColorBtn == null) return;

            _resetBgColorBtn.Visible = ColorTranslator.ToHtml(_bgColorBtn.BackColor).ToUpper() != "#005FB8";
            _resetFontColorBtn.Visible = ColorTranslator.ToHtml(_fontColorBtn.BackColor).ToUpper() != "#FFFFFF";
            _resetFontSizeBtn.Visible = _fontSizeNum.Value != 14m;
            _resetSizeBtn.Visible = _widthNum.Value != 250 || _heightNum.Value != 80;
            
            if (_fontFamilyCombo != null && _resetFontBtn != null)
            {
                string selectedFont = _fontFamilyCombo.SelectedItem?.ToString() ?? "";
                _resetFontBtn.Visible = selectedFont != "Segoe UI Variable Display";
            }

            if (_soundLabel != null)
            {
                UpdateSoundLabel();
            }
        }

        private void PickSound()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Audio Files|*.wav";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _settings.DefaultSoundPath = ofd.FileName;
                    UpdateSoundLabel();
                }
            }
        }

        private void SaveData()
        {
            _settings.DefaultBackgroundColor = ColorTranslator.ToHtml(_bgColorBtn.BackColor);
            _settings.DefaultFontColor = ColorTranslator.ToHtml(_fontColorBtn.BackColor);
            _settings.DefaultFontSize = (float)_fontSizeNum.Value;
            _settings.DefaultWidth = (int)_widthNum.Value;
            _settings.DefaultHeight = (int)_heightNum.Value;
            _settings.DefaultFontFamily = _fontFamilyCombo.SelectedItem?.ToString() ?? "Segoe UI Variable Display";
            // sound path is updated in PickSound
            _settingsService.SaveSettings();
        }

        private void RestoreDefaults()
        {
            if (MessageBox.Show("Are you sure you want to reset all reminder settings to defaults?", "Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _settings.DefaultBackgroundColor = "#005FB8";
                _settings.DefaultFontColor = "#FFFFFF";
                _settings.DefaultFontSize = 14f;
                _settings.DefaultFontFamily = "Segoe UI Variable Display";
                _settings.DefaultWidth = 250;
                _settings.DefaultHeight = 80;
                _settings.DefaultSoundPath = string.Empty;

                LoadData();
            }
        }
    }
}
