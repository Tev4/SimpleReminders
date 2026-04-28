using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using SimpleReminders.Models;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class EditReminderForm : Form
    {
        public Reminder Reminder { get; private set; }
        private readonly SettingsService _settingsService;
        private readonly bool _isNew;

        private TextBox _titleBox = null!;
        private TextBox _messageBox = null!;
        private CheckBox _recurringCheck = null!;
        private NumericUpDown _daysNum = null!;
        private NumericUpDown _hoursNum = null!;
        private NumericUpDown _minutesNum = null!;
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
        private DateTimePicker _dueDatePicker = null!;
        private Button _soundBtn = null!;
        private Button _resetSoundBtn = null!;
        private Label _soundLabel = null!;
        private CheckBox _fireIfMissedCheck = null!;
        private CheckBox _autoFadeCheck = null!;
        private NumericUpDown _displayDurationNum = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        private CheckBox _specificDaysCheck = null!;
        private FlowLayoutPanel _daysPanel = null!;
        private CheckBox[] _dayCheckboxes = new CheckBox[7];
        private readonly string[] _dayLabels = { "Mo", "Tu", "We", "Th", "Fr", "Sa", "Su" };
        private readonly DayOfWeek[] _days = { 
            DayOfWeek.Monday, 
            DayOfWeek.Tuesday, 
            DayOfWeek.Wednesday, 
            DayOfWeek.Thursday, 
            DayOfWeek.Friday, 
            DayOfWeek.Saturday, 
            DayOfWeek.Sunday 
        };

        public EditReminderForm(SettingsService settingsService, Reminder? reminder = null)
        {
            _settingsService = settingsService;
            _isNew = reminder == null;
            if (_isNew)
            {
                var settings = _settingsService.Settings;
                Reminder = new Reminder
                {
                    BackgroundColor = settings.DefaultBackgroundColor,
                    FontColor = settings.DefaultFontColor,
                    FontSize = settings.DefaultFontSize,
                    FontFamily = settings.DefaultFontFamily,
                    Width = settings.DefaultWidth,
                    Height = settings.DefaultHeight,
                    SoundPath = settings.DefaultSoundPath,
                    FireIfMissed = settings.DefaultFireIfMissed,
                    DueDate = DateTime.Now.AddMinutes(5)
                };
            }
            else
            {
                Reminder = reminder!;
            }

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNew ? "New Reminder" : "Edit Reminder";
            this.Icon = IconService.AppIcon;
            this.Size = new Size(500, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 16;
            layout.ColumnCount = 2;
            for (int i = 0; i < 15; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            // Add a filler row to take up remaining space (row 15)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.AutoSize = true;

            // Title
            layout.Controls.Add(new Label { Text = "Title:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 0);
            _titleBox = new TextBox { Width = 250 };
            layout.Controls.Add(_titleBox, 1, 0);

            // Message
            layout.Controls.Add(new Label { Text = "Message:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 1);
            _messageBox = new TextBox { Width = 250 };
            layout.Controls.Add(_messageBox, 1, 1);

            // Due Date
            layout.Controls.Add(new Label { Text = "Next Due Date:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 2);
            _dueDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MM/dd/yyyy HH:mm:ss", Width = 250};
            layout.Controls.Add(_dueDatePicker, 1, 2);

            // Recurring
            layout.Controls.Add(new Label { Text = "Recurring:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 3);
            _recurringCheck = new CheckBox { Text = "Enable" };
            _recurringCheck.CheckedChanged += (s, e) => ToggleRecurring(_recurringCheck.Checked);
            layout.Controls.Add(_recurringCheck, 1, 3);

            // Recurrence Interval
            var recurTable = new TableLayoutPanel { ColumnCount = 2, RowCount = 3, AutoSize = true, Anchor = AnchorStyles.Left };
            _daysNum = new NumericUpDown { Maximum = 365, DecimalPlaces = 0, Width = 60 };
            _hoursNum = new NumericUpDown { Maximum = 23, DecimalPlaces = 0, Width = 60 };
            _minutesNum = new NumericUpDown { Maximum = 59, DecimalPlaces = 0, Width = 60 };
            
            recurTable.Controls.Add(new Label { Text = "Days:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            recurTable.Controls.Add(_daysNum, 1, 0);
            recurTable.Controls.Add(new Label { Text = "Hours:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            recurTable.Controls.Add(_hoursNum, 1, 1);
            recurTable.Controls.Add(new Label { Text = "Minutes:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            recurTable.Controls.Add(_minutesNum, 1, 2);
            layout.Controls.Add(recurTable, 1, 4);

            // Days selection enable
            layout.Controls.Add(new Label { Text = "Specific Days:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 5);
            _specificDaysCheck = new CheckBox { Text = "Enable selection" };
            _specificDaysCheck.CheckedChanged += (s, e) => 
            {
                bool specEnabled = _specificDaysCheck.Checked;
                
                // If specific days is enabled, Recurring MUST be enabled
                if (specEnabled && !_recurringCheck.Checked)
                {
                    _recurringCheck.Checked = true;
                }

                _daysPanel.Enabled = specEnabled;
                
                // Disable interval if specific days are enabled
                _daysNum.Enabled = !specEnabled && _recurringCheck.Checked;
                _hoursNum.Enabled = !specEnabled && _recurringCheck.Checked;
                _minutesNum.Enabled = !specEnabled && _recurringCheck.Checked;

                if (specEnabled)
                {
                    // By default, if specific days is enabled, we expect 1 day interval
                    // so it checks the next day for the enabled day list.
                    _daysNum.Value = 1;
                    _hoursNum.Value = 0;
                    _minutesNum.Value = 0;
                }
                else
                {
                    foreach (var cb in _dayCheckboxes) cb.Checked = false;
                }
            };
            layout.Controls.Add(_specificDaysCheck, 1, 5);

            // Days selection buttons
            _daysPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left };
            _daysPanel.Enabled = false;
            for (int i = 0; i < 7; i++)
            {
                var cb = new CheckBox
                {
                    Text = _dayLabels[i],
                    Appearance = Appearance.Button,
                    Size = new Size(35, 30),
                    TextAlign = ContentAlignment.MiddleCenter,
                    FlatStyle = FlatStyle.Flat,
                    Tag = _days[i]
                };
                cb.FlatAppearance.CheckedBackColor = Color.FromArgb(0, 95, 184); // Theme blue
                cb.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 120, 215);
                cb.ForeColor = Color.Black;
                cb.CheckedChanged += (s, ev) => cb.ForeColor = cb.Checked ? Color.White : Color.Black;
                
                _dayCheckboxes[i] = cb;
                _daysPanel.Controls.Add(cb);
            }
            layout.Controls.Add(_daysPanel, 1, 6);

            // Background Color
            layout.Controls.Add(new Label { Text = "Background Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 7);
            var bgPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _bgColorBtn = new Button { Text = "", Width = 60 };
            _bgColorBtn.Click += (s, e) => PickColor(_bgColorBtn, true);
            _resetBgColorBtn = CreateResetButton();
            _resetBgColorBtn.Click += (s, e) => {
                _bgColorBtn.BackColor = ColorTranslator.FromHtml(_settingsService.Settings.DefaultBackgroundColor);
                Reminder.BackgroundColor = _settingsService.Settings.DefaultBackgroundColor;
                _bgColorBtn.Focus();
                UpdateResetButtonVisibilities();
            };
            bgPanel.Controls.Add(_bgColorBtn);
            bgPanel.Controls.Add(_resetBgColorBtn);
            layout.Controls.Add(bgPanel, 1, 7);

            // Text Color
            layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 8);
            var fgPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _fontColorBtn = new Button { Text = "", Width = 60 };
            _fontColorBtn.Click += (s, e) => PickColor(_fontColorBtn, false);
            _resetFontColorBtn = CreateResetButton();
            _resetFontColorBtn.Click += (s, e) => {
                _fontColorBtn.BackColor = ColorTranslator.FromHtml(_settingsService.Settings.DefaultFontColor);
                Reminder.FontColor = _settingsService.Settings.DefaultFontColor;
                _fontColorBtn.Focus();
                UpdateResetButtonVisibilities();
            };
            fgPanel.Controls.Add(_fontColorBtn);
            fgPanel.Controls.Add(_resetFontColorBtn);
            layout.Controls.Add(fgPanel, 1, 8);

            // Font Size
            layout.Controls.Add(new Label { Text = "Font Size:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 9);
            var sizeInfoPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _fontSizeNum = new NumericUpDown { Minimum = 8, Maximum = 72, Width = 60 };
            _fontSizeNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetFontSizeBtn = CreateResetButton();
            _resetFontSizeBtn.Click += (s, e) => {
                _fontSizeNum.Value = (decimal)_settingsService.Settings.DefaultFontSize;
                _fontSizeNum.Focus();
            };
            sizeInfoPanel.Controls.Add(_fontSizeNum);
            sizeInfoPanel.Controls.Add(_resetFontSizeBtn);
            layout.Controls.Add(sizeInfoPanel, 1, 9);
            
            // Font Family
            layout.Controls.Add(new Label { Text = "Font Family:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 10);
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
                string defaultFont = _settingsService.Settings.DefaultFontFamily;
                int defaultIndex = _fontFamilyCombo.FindStringExact(defaultFont);
                _fontFamilyCombo.SelectedIndex = defaultIndex >= 0 ? defaultIndex : 0;
                _fontFamilyCombo.Focus();
            };

            // Populate fonts
            foreach (var family in FontFamily.Families)
            {
                _fontFamilyCombo.Items.Add(family.Name);
            }
            _fontFamilyCombo.SelectedIndexChanged += (s, e) => {
                string selectedFont = _fontFamilyCombo.SelectedItem?.ToString() ?? "";
                _resetFontBtn.Visible = selectedFont != _settingsService.Settings.DefaultFontFamily;
            };

            fontPanel.Controls.Add(_fontFamilyCombo);
            fontPanel.Controls.Add(_resetFontBtn);
            layout.Controls.Add(fontPanel, 1, 10);

            // Size
            layout.Controls.Add(new Label { Text = "Notification Size (W x H):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 11);
            var sizePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _widthNum = new NumericUpDown { Minimum = 100, Maximum = 1000, Width = 60 };
            _heightNum = new NumericUpDown { Minimum = 40, Maximum = 1000, Width = 60 };
            _widthNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _heightNum.ValueChanged += (s, e) => UpdateResetButtonVisibilities();
            _resetSizeBtn = CreateResetButton();
            _resetSizeBtn.Click += (s, e) => {
                _widthNum.Value = _settingsService.Settings.DefaultWidth;
                _heightNum.Value = _settingsService.Settings.DefaultHeight;
                _widthNum.Focus();
            };
            sizePanel.Controls.Add(_widthNum);
            sizePanel.Controls.Add(new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 5, 0, 0) });
            sizePanel.Controls.Add(_heightNum);
            sizePanel.Controls.Add(_resetSizeBtn);
            layout.Controls.Add(sizePanel, 1, 11);

            // Sound
            layout.Controls.Add(new Label { Text = "Notification Sound:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 12);
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
                Reminder.SoundPath = _settingsService.Settings.DefaultSoundPath;
                UpdateSoundLabel();
                _soundBtn.Focus();
            };
            _soundLabel = new Label { Text = "Default", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left };
            soundPanel.Controls.Add(_soundBtn);
            soundPanel.Controls.Add(_resetSoundBtn);
            soundPanel.Controls.Add(_soundLabel);
            layout.Controls.Add(soundPanel, 1, 12);
            
            // Auto-fade
            layout.Controls.Add(new Label { Text = "Auto-fade:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 13);
            var fadePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left, WrapContents = false };
            _autoFadeCheck = new CheckBox { Text = "Fade away after", AutoSize = true, Margin = new Padding(0, 5, 5, 0) };
            _displayDurationNum = new NumericUpDown { Minimum = 1, Maximum = 3600, Width = 60 };
            var secLabel = new Label { Text = "seconds", AutoSize = true, Margin = new Padding(0, 5, 0, 0) };
            
            _autoFadeCheck.CheckedChanged += (s, e) => _displayDurationNum.Enabled = _autoFadeCheck.Checked;
            
            fadePanel.Controls.Add(_autoFadeCheck);
            fadePanel.Controls.Add(_displayDurationNum);
            fadePanel.Controls.Add(secLabel);
            layout.Controls.Add(fadePanel, 1, 13);
            
            // Fire If Missed
            layout.Controls.Add(new Label { Text = "Fire If Missed:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 14);
            _fireIfMissedCheck = new CheckBox {};
            layout.Controls.Add(_fireIfMissedCheck, 1, 14);

            // Buttons
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };

            _saveButton.Click += (s, e) =>
            {
                SaveData();
                this.Close();
            };

            // Button is disabled if title is empty
            _titleBox.TextChanged += (s, e) =>
            {
                _saveButton.Enabled = !string.IsNullOrWhiteSpace(_titleBox.Text);
            };

            _saveButton.Enabled = !string.IsNullOrWhiteSpace(_titleBox.Text);

            btnPanel.Controls.Add(_cancelButton);
            btnPanel.Controls.Add(_saveButton);

            this.Controls.Add(btnPanel);
            this.Controls.Add(layout);
        }

        private void LoadData()
        {
            _titleBox.Text = Reminder.Title;
            _messageBox.Text = Reminder.Message;
            _dueDatePicker.Value = Reminder.DueDate;
            
            _recurringCheck.Checked = Reminder.IsRecurring;
            
            if (Reminder.RecurrenceInterval.TotalMinutes > 0)
            {
                _daysNum.Value = Reminder.RecurrenceInterval.Days;
                _hoursNum.Value = Reminder.RecurrenceInterval.Hours;
                _minutesNum.Value = Reminder.RecurrenceInterval.Minutes;
            }
            
            _fontSizeNum.Value = (decimal)Reminder.FontSize;
            _widthNum.Value = Reminder.Width > 0 ? Reminder.Width : 250;
            _heightNum.Value = Reminder.Height > 0 ? Reminder.Height : 80;
            _bgColorBtn.BackColor = ColorTranslator.FromHtml(Reminder.BackgroundColor);
            _fontColorBtn.BackColor = ColorTranslator.FromHtml(Reminder.FontColor);
            _autoFadeCheck.Checked = Reminder.AutoFade;
            _displayDurationNum.Value = Math.Max(_displayDurationNum.Minimum, Math.Min(_displayDurationNum.Maximum, Reminder.DisplayDurationSeconds));
            _displayDurationNum.Enabled = Reminder.AutoFade;
            _fireIfMissedCheck.Checked = Reminder.FireIfMissed;

            string targetFont = !string.IsNullOrEmpty(Reminder.FontFamily) 
                ? Reminder.FontFamily 
                : _settingsService.Settings.DefaultFontFamily;

            int index = _fontFamilyCombo.FindStringExact(targetFont);
            _fontFamilyCombo.SelectedIndex = index >= 0 ? index : 0;

            UpdateResetButtonVisibilities();

            // Load enabled days
            bool hasSpecificDays = Reminder.EnabledDays != null && Reminder.EnabledDays.Count > 0;
            _specificDaysCheck.Checked = hasSpecificDays;
            _daysPanel.Enabled = hasSpecificDays;

            for (int i = 0; i < 7; i++)
            {
                _dayCheckboxes[i].Checked = Reminder.EnabledDays?.Contains(_days[i]) ?? false;
            }
            
            UpdateSoundLabel();

            ToggleRecurring(Reminder.IsRecurring);
        }

        private void ToggleRecurring(bool enabled)
        {
            // Keep _specificDaysCheck always enabled so user can click it to turn on recurring
            _specificDaysCheck.Enabled = true; 
            
            bool specEnabled = _specificDaysCheck.Checked;
            _daysPanel.Enabled = enabled && specEnabled;
            
            // Interval is only enabled if recurring is ON AND specific days is OFF
            _daysNum.Enabled = enabled && !specEnabled;
            _hoursNum.Enabled = enabled && !specEnabled;
            _minutesNum.Enabled = enabled && !specEnabled;

            if (!enabled) 
            {
                _specificDaysCheck.Checked = false;
            }
        }

        private void PickColor(Button btn, bool isBg)
        {
            using (var cd = new ColorDialog())
            {
                cd.Color = btn.BackColor;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    btn.BackColor = cd.Color;
                    string hex = ColorTranslator.ToHtml(cd.Color);
                    if (isBg) Reminder.BackgroundColor = hex;
                    else Reminder.FontColor = hex;
                    UpdateResetButtonVisibilities();
                }
            }
        }

        private void PickSound()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Audio Files|*.wav";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Reminder.SoundPath = ofd.FileName;
                    UpdateSoundLabel();
                }
            }
        }

        private void UpdateSoundLabel()
        {
             string currentPath = Reminder.SoundPath;
             string defaultPath = _settingsService.Settings.DefaultSoundPath;
             
             bool isDefault = currentPath == defaultPath;
             
             if (isDefault)
             {
                 _soundLabel.Text = "App default";
             }
             else
             {
                 _soundLabel.Text = System.IO.Path.GetFileName(currentPath);
             }
             
             _resetSoundBtn.Visible = !isDefault;
        }

        private void SaveData()
        {
            Reminder.Title = _titleBox.Text;
            Reminder.Message = _messageBox.Text;
            Reminder.DueDate = _dueDatePicker.Value;
            Reminder.IsRecurring = _recurringCheck.Checked;
            Reminder.RecurrenceInterval = new TimeSpan((int)_daysNum.Value, (int)_hoursNum.Value, (int)_minutesNum.Value, 0);
            Reminder.FontSize = (float)_fontSizeNum.Value;
            Reminder.Width = (int)_widthNum.Value;
            Reminder.Height = (int)_heightNum.Value;
            Reminder.BackgroundColor = ColorTranslator.ToHtml(_bgColorBtn.BackColor);
            Reminder.FontColor = ColorTranslator.ToHtml(_fontColorBtn.BackColor);
            Reminder.FontFamily = _fontFamilyCombo.SelectedItem?.ToString() ?? _settingsService.Settings.DefaultFontFamily;
            Reminder.AutoFade = _autoFadeCheck.Checked;
            Reminder.DisplayDurationSeconds = (int)_displayDurationNum.Value;
            Reminder.FireIfMissed = _fireIfMissedCheck.Checked;

            // Save enabled days
            Reminder.EnabledDays.Clear();
            if (_specificDaysCheck.Checked)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (_dayCheckboxes[i].Checked)
                    {
                        Reminder.EnabledDays.Add(_days[i]);
                    }
                }
            }
            // SoundPath is already updated in PickSound/Reminder object reference
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
            var settings = _settingsService.Settings;
            
            if (_resetBgColorBtn != null)
                _resetBgColorBtn.Visible = ColorTranslator.ToHtml(_bgColorBtn.BackColor) != settings.DefaultBackgroundColor;
            
            if (_resetFontColorBtn != null)
                _resetFontColorBtn.Visible = ColorTranslator.ToHtml(_fontColorBtn.BackColor) != settings.DefaultFontColor;
            
            if (_resetFontSizeBtn != null)
                _resetFontSizeBtn.Visible = (float)_fontSizeNum.Value != settings.DefaultFontSize;
            
            if (_resetSizeBtn != null)
                _resetSizeBtn.Visible = (int)_widthNum.Value != settings.DefaultWidth || (int)_heightNum.Value != settings.DefaultHeight;
            
            if (_fontFamilyCombo != null && _resetFontBtn != null)
            {
                string selectedFont = _fontFamilyCombo.SelectedItem?.ToString() ?? "";
                _resetFontBtn.Visible = selectedFont != settings.DefaultFontFamily;
            }

            if (_soundLabel != null)
            {
                UpdateSoundLabel();
            }
        }
    }
}
