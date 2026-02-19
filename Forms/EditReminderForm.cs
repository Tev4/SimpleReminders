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
        private readonly bool _isNew;

        private TextBox _titleBox = null!;
        private TextBox _messageBox = null!;
        private CheckBox _recurringCheck = null!;
        private NumericUpDown _daysNum = null!;
        private NumericUpDown _hoursNum = null!;
        private NumericUpDown _minutesNum = null!;
        private Button _bgColorBtn = null!;
        private Button _fontColorBtn = null!;
        private NumericUpDown _fontSizeNum = null!;
        private DateTimePicker _dueDatePicker = null!;
        private Button _soundBtn = null!;
        private Label _soundLabel = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public EditReminderForm(Reminder? reminder = null)
        {
            _isNew = reminder == null;
            Reminder = reminder ?? new Reminder();
            if (_isNew) Reminder.DueDate = DateTime.Now.AddMinutes(5); // Default 5 mins

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNew ? "New Reminder" : "Edit Reminder";
            this.Icon = IconService.AppIcon;
            this.Size = new Size(400, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 10;
            layout.ColumnCount = 2;
            layout.AutoSize = true;

            // Title
            layout.Controls.Add(new Label { Text = "Title:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
            _titleBox = new TextBox { Width = 250 };
            layout.Controls.Add(_titleBox, 1, 0);

            // Message
            layout.Controls.Add(new Label { Text = "Message:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
            _messageBox = new TextBox { Width = 250 };
            layout.Controls.Add(_messageBox, 1, 1);

            // Due Date
            layout.Controls.Add(new Label { Text = "Next Due Date:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 2);
            _dueDatePicker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "MM/dd/yyyy HH:mm:ss", Width = 250 };
            layout.Controls.Add(_dueDatePicker, 1, 2);

            // Recurring
            layout.Controls.Add(new Label { Text = "Recurring:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            _recurringCheck = new CheckBox { Text = "Enable" };
            _recurringCheck.CheckedChanged += (s, e) => ToggleRecurring(_recurringCheck.Checked);
            layout.Controls.Add(_recurringCheck, 1, 3);

            // Recurrence Interval
            var recurPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            _daysNum = new NumericUpDown { Maximum = 365, DecimalPlaces = 0, Width = 50 };
            _hoursNum = new NumericUpDown { Maximum = 23, DecimalPlaces = 0, Width = 50 };
            _minutesNum = new NumericUpDown { Maximum = 59, DecimalPlaces = 0, Width = 50 };
            recurPanel.Controls.Add(new Label { Text = "D:" });
            recurPanel.Controls.Add(_daysNum);
            recurPanel.Controls.Add(new Label { Text = "H:" });
            recurPanel.Controls.Add(_hoursNum);
            recurPanel.Controls.Add(new Label { Text = "M:" });
            recurPanel.Controls.Add(_minutesNum);
            layout.Controls.Add(recurPanel, 1, 4);

            // Background Color
            layout.Controls.Add(new Label { Text = "Background Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 5);
            _bgColorBtn = new Button { Text = "", Width = 60 };
            _bgColorBtn.Click += (s, e) => PickColor(_bgColorBtn, true);
            layout.Controls.Add(_bgColorBtn, 1, 5);

            // Text Color
            layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 6);
            _fontColorBtn = new Button { Text = "", Width = 60 };
            _fontColorBtn.Click += (s, e) => PickColor(_fontColorBtn, false);
            layout.Controls.Add(_fontColorBtn, 1, 6);

            // Font Size
            layout.Controls.Add(new Label { Text = "Font Size:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 7);
            _fontSizeNum = new NumericUpDown { Minimum = 8, Maximum = 72, Width = 60 };
            layout.Controls.Add(_fontSizeNum, 1, 7);

            // Sound
            layout.Controls.Add(new Label { Text = "Notification Sound:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 8);
            var soundPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true };
            _soundBtn = new Button { Text = "Browse", Width = 80 };
            _soundBtn.Click += (s, e) => PickSound();
            _soundLabel = new Label { Text = "Default", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left };
            soundPanel.Controls.Add(_soundBtn);
            soundPanel.Controls.Add(_soundLabel);
            layout.Controls.Add(soundPanel, 1, 8);

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

            this.Controls.Add(layout);
            this.Controls.Add(btnPanel);
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
            _bgColorBtn.BackColor = ColorTranslator.FromHtml(Reminder.BackgroundColor);
            _fontColorBtn.BackColor = ColorTranslator.FromHtml(Reminder.FontColor);
            
            UpdateSoundLabel();

            ToggleRecurring(Reminder.IsRecurring);
        }

        private void ToggleRecurring(bool enabled)
        {
            _daysNum.Enabled = enabled;
            _hoursNum.Enabled = enabled;
            _minutesNum.Enabled = enabled;
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
             _soundLabel.Text = string.IsNullOrEmpty(Reminder.SoundPath) ? "Default" : System.IO.Path.GetFileName(Reminder.SoundPath);
        }

        private void SaveData()
        {
            Reminder.Title = _titleBox.Text;
            Reminder.Message = _messageBox.Text;
            Reminder.DueDate = _dueDatePicker.Value;
            Reminder.IsRecurring = _recurringCheck.Checked;
            Reminder.RecurrenceInterval = new TimeSpan((int)_daysNum.Value, (int)_hoursNum.Value, (int)_minutesNum.Value, 0);
            Reminder.FontSize = (float)_fontSizeNum.Value;
            Reminder.BackgroundColor = ColorTranslator.ToHtml(_bgColorBtn.BackColor);
            Reminder.FontColor = ColorTranslator.ToHtml(_fontColorBtn.BackColor);
            // SoundPath is already updated in PickSound/Reminder object reference
        }
    }
}
