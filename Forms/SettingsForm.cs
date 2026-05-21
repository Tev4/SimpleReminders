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
        private NumericUpDown _offsetXNum = null!;
        private NumericUpDown _offsetYNum = null!;
        private Button _resetSizeBtn = null!;
        private Button _resetOffsetBtn = null!;
        private Button _soundBtn = null!;
        private Button _resetSoundBtn = null!;
        private Button _pickPosBtn = null!;
        private Label _soundLabel = null!;
        private ComboBox _anchorCombo = null!;
        private CheckBox _showOnStartupIfMissedCheck = null!;
        private CheckBox _autoFadeCheck = null!;
        private NumericUpDown _fadeDelayNum = null!;
        private TextBox _hotkeyBox = null!;
        private Button _resetHotkeyBtn = null!;
        private Keys _currentHotkey = Keys.None;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;
        private Button _restoreDefaultsBtn = null!;
        private string _tempSoundPath = string.Empty;
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
            this.Size = new Size(420, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(15);
            layout.RowCount = 13;
            layout.ColumnCount = 2;
            for (int i = 0; i < 12; i++)
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
            _widthNum = new NumericUpDown { Minimum = 100, Maximum = 4000, Width = 60 };
            _heightNum = new NumericUpDown { Minimum = 40, Maximum = 4000, Width = 60 };
            _widthNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _heightNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetSizeBtn = CreateResetButton();
            _resetSizeBtn.Click += (s, e) => {
                _widthNum.Value = 250;
                _heightNum.Value = 80;
                _widthNum.Focus();
            };
            sizePanel.Controls.Add(_widthNum);
            sizePanel.Controls.Add(new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 0, 1) });
            sizePanel.Controls.Add(_heightNum);
            sizePanel.Controls.Add(_resetSizeBtn);
            layout.Controls.Add(sizePanel, 1, 4);

            // Default Offset (Position)
            layout.Controls.Add(new Label { Text = "Default Position (X x Y):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 5);
            var offsetPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _offsetXNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 60 };
            _offsetYNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 60 };
            _offsetXNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _offsetYNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetOffsetBtn = CreateResetButton();
            _resetOffsetBtn.Click += (s, e) => {
                _offsetXNum.Value = 0;
                _offsetYNum.Value = 0;
                _offsetXNum.Focus();
            };
            offsetPanel.Controls.Add(_offsetXNum);
            offsetPanel.Controls.Add(new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 0, 0, 1) });
            offsetPanel.Controls.Add(_offsetYNum);
            offsetPanel.Controls.Add(_resetOffsetBtn);
            layout.Controls.Add(offsetPanel, 1, 5);

            // Anchor Row
            layout.Controls.Add(new Label { Text = "Anchor Point:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 6);
            _anchorCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            foreach (var val in Enum.GetValues(typeof(NotificationAnchor))) _anchorCombo.Items.Add(val);
            _anchorCombo.SelectedItem = _settings.DefaultAnchor;
            _anchorCombo.SelectedIndexChanged += (s, e) => {
                _settings.DefaultAnchor = (NotificationAnchor)_anchorCombo.SelectedItem;
                UpdateResetButtonVisibilities();
            };
            layout.Controls.Add(_anchorCombo, 1, 6);

            // Visual Picker Row
            layout.Controls.Add(new Label { Text = "Visual Editor:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 7);
            _pickPosBtn = new Button { Text = "Positioning Overlay", AutoSize = true, Height = 30, Margin = new Padding(3, 10, 3, 10) };
            _pickPosBtn.Click += (s, e) => {
                using (var overlay = new PositionPickerOverlay((int)_offsetXNum.Value, (int)_offsetYNum.Value, (int)_widthNum.Value, (int)_heightNum.Value, (NotificationAnchor)_anchorCombo.SelectedItem, _settings))
                {
                    if (overlay.ShowDialog() == DialogResult.OK)
                    {
                        _offsetXNum.Value = overlay.ResultX;
                        _offsetYNum.Value = overlay.ResultY;
                        _widthNum.Value = overlay.ResultWidth;
                        _heightNum.Value = overlay.ResultHeight;
                        _anchorCombo.SelectedItem = overlay.ResultAnchor;
                    }
                }
            };
            layout.Controls.Add(_pickPosBtn, 1, 7);

            // Sound
            layout.Controls.Add(new Label { Text = "Notification Sound:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 8);
            var soundPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
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
                _tempSoundPath = string.Empty;
                UpdateSoundLabel();
                _soundBtn.Focus();
                UpdateResetButtonVisibilities();
            };
            _soundLabel = new Label { 
                Text = "System Default", 
                AutoSize = false, 
                Width = 180, 
                Height = 25, 
                AutoEllipsis = true, 
                TextAlign = ContentAlignment.MiddleLeft, 
                Anchor = AnchorStyles.Left 
            };
            soundPanel.Controls.Add(_soundBtn);
            soundPanel.Controls.Add(_resetSoundBtn);
            soundPanel.Controls.Add(_soundLabel);
            layout.Controls.Add(soundPanel, 1, 8);
            
            // Show on startup if missed
            layout.Controls.Add(new Label { Text = "Show on startup if missed:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 9);
            _showOnStartupIfMissedCheck = new CheckBox {};
            layout.Controls.Add(_showOnStartupIfMissedCheck, 1, 9);

            // Auto Fade
            layout.Controls.Add(new Label { Text = "Auto-fade:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 10);
            var fadePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _autoFadeCheck = new CheckBox { Text = "Fade away after", AutoSize = true, Margin = new Padding(0, 4, 5, 0) };
            _fadeDelayNum = new NumericUpDown { Minimum = 1, Maximum = 3600, Width = 60 };
            var secLabel = new Label { Text = "seconds", AutoSize = true, Margin = new Padding(0, 6, 0, 0) };
            
            _autoFadeCheck.CheckedChanged += (s, e) => _fadeDelayNum.Enabled = _autoFadeCheck.Checked;
            
            fadePanel.Controls.Add(_autoFadeCheck);
            fadePanel.Controls.Add(_fadeDelayNum);
            fadePanel.Controls.Add(secLabel);
            layout.Controls.Add(fadePanel, 1, 10);

            // Hotkey
            layout.Controls.Add(new Label { Text = "Dismiss Hotkey:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 11);
            var hotkeyPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _hotkeyBox = new TextBox { Width = 150, ReadOnly = true, BackColor = SystemColors.Window };
            _hotkeyBox.KeyDown += (s, e) => {
                e.Handled = true;
                e.SuppressKeyPress = true;
                if (e.KeyCode == Keys.ShiftKey || e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.Menu) return;
                if (e.KeyCode == Keys.Escape) {
                    _currentHotkey = Keys.None;
                } else {
                    _currentHotkey = e.KeyData;
                }
                UpdateHotkeyText();
                CheckForChanges();
                UpdateResetButtonVisibilities();
            };
            _resetHotkeyBtn = CreateResetButton();
            _resetHotkeyBtn.Click += (s, e) => {
                _currentHotkey = Keys.None;
                UpdateHotkeyText();
                CheckForChanges();
                UpdateResetButtonVisibilities();
            };
            hotkeyPanel.Controls.Add(_hotkeyBox);
            hotkeyPanel.Controls.Add(_resetHotkeyBtn);
            layout.Controls.Add(hotkeyPanel, 1, 11);

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
            
            // Hook up events for change tracking
            _bgColorBtn.BackColorChanged += (s, e) => CheckForChanges();
            _fontColorBtn.BackColorChanged += (s, e) => CheckForChanges();
            _fontSizeNum.ValueChanged += (s, e) => CheckForChanges();
            _widthNum.ValueChanged += (s, e) => CheckForChanges();
            _heightNum.ValueChanged += (s, e) => CheckForChanges();
            _offsetXNum.ValueChanged += (s, e) => CheckForChanges();
            _offsetYNum.ValueChanged += (s, e) => CheckForChanges();
            _fontFamilyCombo.SelectedIndexChanged += (s, e) => CheckForChanges();
            _anchorCombo.SelectedIndexChanged += (s, e) => CheckForChanges();
            _showOnStartupIfMissedCheck.CheckedChanged += (s, e) => CheckForChanges();
            _autoFadeCheck.CheckedChanged += (s, e) => CheckForChanges();
            _fadeDelayNum.ValueChanged += (s, e) => CheckForChanges();

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
            _offsetXNum.Value = _settings.DefaultOffsetX;
            _offsetYNum.Value = _settings.DefaultOffsetY;

            int index = _fontFamilyCombo.FindStringExact(_settings.DefaultFontFamily);
            _fontFamilyCombo.SelectedIndex = index >= 0 ? index : 0;

            _tempSoundPath = _settings.DefaultSoundPath;
            _showOnStartupIfMissedCheck.Checked = _settings.DefaultShowOnStartupIfMissed;
            _autoFadeCheck.Checked = _settings.DefaultAutoFade;
            _fadeDelayNum.Value = _settings.DefaultFadeDelay;
            _fadeDelayNum.Enabled = _autoFadeCheck.Checked;
            _currentHotkey = _settings.DefaultDismissHotkey;
            UpdateHotkeyText();
            UpdateResetButtonVisibilities();
            
            CheckForChanges();
        }

        private void UpdateHotkeyText()
        {
            if (_currentHotkey == Keys.None)
            {
                _hotkeyBox.Text = "None (Press a key)";
            }
            else
            {
                var kc = new KeysConverter();
                _hotkeyBox.Text = kc.ConvertToString(_currentHotkey) ?? "Unknown";
            }
        }

        private void UpdateSoundLabel()
        {
            bool hasCustomSound = !string.IsNullOrEmpty(_tempSoundPath);
            _soundLabel.Text = hasCustomSound ? System.IO.Path.GetFileName(_tempSoundPath) : "System Default";
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
            _resetOffsetBtn.Visible = _offsetXNum.Value != 0 || _offsetYNum.Value != 0;
            
            if (_fontFamilyCombo != null && _resetFontBtn != null)
            {
                string selectedFont = _fontFamilyCombo.SelectedItem?.ToString() ?? "";
                _resetFontBtn.Visible = selectedFont != "Segoe UI Variable Display";
            }

            if (_resetHotkeyBtn != null)
            {
                _resetHotkeyBtn.Visible = _currentHotkey != Keys.None;
            }

            if (_soundLabel != null)
            {
                UpdateSoundLabel();
            }
            
            // Check if sound differs from settings to show/hide reset correctly if needed 
            // (but UpdateSoundLabel already handles visibility of the red X based on current selection)
            
            CheckForChanges();
        }

        private void CheckForChanges()
        {
            if (_saveButton == null) return;
            
            bool hasChanges = false;
            hasChanges |= ColorTranslator.ToHtml(_bgColorBtn.BackColor).ToUpper() != _settings.DefaultBackgroundColor.ToUpper();
            hasChanges |= ColorTranslator.ToHtml(_fontColorBtn.BackColor).ToUpper() != _settings.DefaultFontColor.ToUpper();
            hasChanges |= _fontSizeNum.Value != (decimal)_settings.DefaultFontSize;
            hasChanges |= _widthNum.Value != _settings.DefaultWidth;
            hasChanges |= _heightNum.Value != _settings.DefaultHeight;
            hasChanges |= _offsetXNum.Value != _settings.DefaultOffsetX;
            hasChanges |= _offsetYNum.Value != _settings.DefaultOffsetY;
            hasChanges |= (_fontFamilyCombo.SelectedItem?.ToString() ?? "Segoe UI Variable Display") != _settings.DefaultFontFamily;
            hasChanges |= _tempSoundPath != _settings.DefaultSoundPath;
            hasChanges |= _showOnStartupIfMissedCheck.Checked != _settings.DefaultShowOnStartupIfMissed;
            hasChanges |= _autoFadeCheck.Checked != _settings.DefaultAutoFade;
            hasChanges |= _fadeDelayNum.Value != _settings.DefaultFadeDelay;
            hasChanges |= _currentHotkey != _settings.DefaultDismissHotkey;
            
            NotificationAnchor currentAnchor = (NotificationAnchor)(_anchorCombo.SelectedItem ?? NotificationAnchor.BottomRight);
            hasChanges |= currentAnchor != _settings.DefaultAnchor;

            _saveButton.Enabled = hasChanges;
        }

        private void PickSound()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Audio Files|*.wav";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _tempSoundPath = ofd.FileName;
                    UpdateSoundLabel();
                    UpdateResetButtonVisibilities();
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
            _settings.DefaultOffsetX = (int)_offsetXNum.Value;
            _settings.DefaultOffsetY = (int)_offsetYNum.Value;
            _settings.DefaultFontFamily = _fontFamilyCombo.SelectedItem?.ToString() ?? "Segoe UI Variable Display";
            _settings.DefaultSoundPath = _tempSoundPath;
            _settings.DefaultShowOnStartupIfMissed = _showOnStartupIfMissedCheck.Checked;
            _settings.DefaultAutoFade = _autoFadeCheck.Checked;
            _settings.DefaultFadeDelay = (int)_fadeDelayNum.Value;
            _settings.DefaultDismissHotkey = _currentHotkey;
            _settingsService.SaveSettings();
        }

        private void RestoreDefaults()
        {
            if (MessageBox.Show("Are you sure you want to reset all reminder settings to defaults?", "Restore Defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _bgColorBtn.BackColor = ColorTranslator.FromHtml("#005FB8");
                _fontColorBtn.BackColor = ColorTranslator.FromHtml("#FFFFFF");
                _fontSizeNum.Value = 14m;
                
                int index = _fontFamilyCombo.FindStringExact("Segoe UI Variable Display");
                _fontFamilyCombo.SelectedIndex = index >= 0 ? index : 0;
                
                _widthNum.Value = 250;
                _heightNum.Value = 80;
                _offsetXNum.Value = 0;
                _offsetYNum.Value = 0;
                _tempSoundPath = string.Empty;
                _showOnStartupIfMissedCheck.Checked = false;
                _autoFadeCheck.Checked = false;
                _fadeDelayNum.Value = 15;
                _anchorCombo.SelectedItem = NotificationAnchor.BottomRight;
                _currentHotkey = Keys.None;
                UpdateHotkeyText();

                UpdateSoundLabel();
                UpdateResetButtonVisibilities();
            }
        }
    }
}
