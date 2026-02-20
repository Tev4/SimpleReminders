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
        private Button _fontColorBtn = null!;
        private NumericUpDown _fontSizeNum = null!;
        private NumericUpDown _widthNum = null!;
        private NumericUpDown _heightNum = null!;
        private Button _soundBtn = null!;
        private Button _resetSoundBtn = null!;
        private Label _soundLabel = null!;
        private Button _saveButton = null!;
        private Button _cancelButton = null!;

        public SettingsForm(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = settingsService.Settings;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Default Notification Settings";
            this.Icon = IconService.AppIcon;
            this.Size = new Size(400, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(15);
            layout.RowCount = 7;
            layout.ColumnCount = 2;
            for (int i = 0; i < 6; i++)
            {
                layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.AutoSize = true;

            // Background Color
            layout.Controls.Add(new Label { Text = "Background Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 0);
            _bgColorBtn = new Button { Text = "", Width = 60 };
            _bgColorBtn.Click += (s, e) => PickColor(_bgColorBtn, true);
            layout.Controls.Add(_bgColorBtn, 1, 0);

            // Text Color
            layout.Controls.Add(new Label { Text = "Text Color:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 1);
            _fontColorBtn = new Button { Text = "", Width = 60 };
            _fontColorBtn.Click += (s, e) => PickColor(_fontColorBtn, false);
            layout.Controls.Add(_fontColorBtn, 1, 1);

            // Font Size
            layout.Controls.Add(new Label { Text = "Font Size:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 2);
            _fontSizeNum = new NumericUpDown { Minimum = 8, Maximum = 72, Width = 60 };
            layout.Controls.Add(_fontSizeNum, 1, 2);

            // Size
            layout.Controls.Add(new Label { Text = "Notification Size (W x H):", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 3);
            var sizePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Anchor = AnchorStyles.Left };
            _widthNum = new NumericUpDown { Minimum = 100, Maximum = 1000, Width = 60 };
            _heightNum = new NumericUpDown { Minimum = 40, Maximum = 1000, Width = 60 };
            sizePanel.Controls.Add(_widthNum);
            sizePanel.Controls.Add(new Label { Text = "x", AutoSize = true, Anchor = AnchorStyles.Left });
            sizePanel.Controls.Add(_heightNum);
            layout.Controls.Add(sizePanel, 1, 3);

            // Sound
            layout.Controls.Add(new Label { Text = "Notification Sound:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left }, 0, 4);
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
            };
            _soundLabel = new Label { Text = "Default", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft, Anchor = AnchorStyles.Left };
            soundPanel.Controls.Add(_soundBtn);
            soundPanel.Controls.Add(_resetSoundBtn);
            soundPanel.Controls.Add(_soundLabel);
            layout.Controls.Add(soundPanel, 1, 4);

            // Buttons
            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 0, 10, 10) };
            _cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK };

            _saveButton.Click += (s, e) =>
            {
                SaveData();
                this.Close();
            };

            btnPanel.Controls.Add(_cancelButton);
            btnPanel.Controls.Add(_saveButton);

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
            UpdateSoundLabel();
        }

        private void UpdateSoundLabel()
        {
            bool hasCustomSound = !string.IsNullOrEmpty(_settings.DefaultSoundPath);
            _soundLabel.Text = hasCustomSound ? System.IO.Path.GetFileName(_settings.DefaultSoundPath) : "Default";
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
            // sound path is updated in PickSound
            _settingsService.SaveSettings();
        }
    }
}
