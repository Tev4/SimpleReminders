using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SimpleReminders.Models;

namespace SimpleReminders.Forms
{
    public class ExecutableOffsetEditorForm : Form
    {
        private readonly ExecutableOffsetRule _rule;
        private readonly IEnumerable<ExecutableOffsetRule> _existingRules;
        private readonly AppSettings _settings;
        private readonly bool _isNew;

        private TextBox _pathTxt = null!;
        private NumericUpDown _xNum = null!;
        private NumericUpDown _yNum = null!;
        private NumericUpDown _widthNum = null!;
        private NumericUpDown _heightNum = null!;
        private Button _browseBtn = null!;
        private Button _pickPosBtn = null!;
        private ComboBox _anchorCombo = null!;
        private Button _saveBtn = null!;
        private Button _cancelBtn = null!;

        public ExecutableOffsetEditorForm(AppSettings settings, IEnumerable<ExecutableOffsetRule> existingRules, ExecutableOffsetRule? rule = null)
        {
            _settings = settings;
            _existingRules = existingRules;
            if (rule == null)
            {
                _rule = new ExecutableOffsetRule 
                { 
                    Width = settings.DefaultWidth, 
                    Height = settings.DefaultHeight 
                };
                _isNew = true;
            }
            else
            {
                _rule = rule;
                _isNew = false;
            }

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = _isNew ? "Add Executable Offset" : "Edit Executable Offset";
            this.Size = new Size(440, 320);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), RowCount = 7, ColumnCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Executable
            layout.Controls.Add(new Label { Text = "Executable:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            _pathTxt = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            _browseBtn = new Button { Text = "Browse...", AutoSize = true };
            _browseBtn.Click += PickExecutable;
            layout.Controls.Add(_pathTxt, 1, 0);
            layout.Controls.Add(_browseBtn, 2, 0);

            // Position (X/Y)
            layout.Controls.Add(new Label { Text = "Position Offset:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            var posPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
            _xNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 60 };
            _yNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 60 };
            posPanel.Controls.Add(new Label { Text = "X:", AutoSize = false, Width = 20, Height = 23, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 2, 0, 0) });
            posPanel.Controls.Add(_xNum);
            posPanel.Controls.Add(new Label { Text = "Y:", AutoSize = false, Width = 20, Height = 23, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 2, 0, 0) });
            posPanel.Controls.Add(_yNum);
            layout.Controls.Add(posPanel, 1, 1);

            // Size (W/H)
            layout.Controls.Add(new Label { Text = "Notification Size:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            var sizePanel = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, AutoSize = true, WrapContents = false };
            _widthNum = new NumericUpDown { Minimum = 100, Maximum = 4000, Width = 60 };
            _heightNum = new NumericUpDown { Minimum = 40, Maximum = 4000, Width = 60 };
            sizePanel.Controls.Add(new Label { Text = "W:", AutoSize = false, Width = 25, Height = 23, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 2, 0, 0) });
            sizePanel.Controls.Add(_widthNum);
            sizePanel.Controls.Add(new Label { Text = "H:", AutoSize = false, Width = 20, Height = 23, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 2, 0, 0) });
            sizePanel.Controls.Add(_heightNum);
            layout.Controls.Add(sizePanel, 1, 2);

            // Visual Editor
            layout.Controls.Add(new Label { Text = "Visual Editor:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
            _pickPosBtn = new Button { Text = "Positioning Overlay", AutoSize = true, Height = 30, Margin = new Padding(3, 10, 3, 10) };
            _pickPosBtn.Click += PickPosition;
            layout.Controls.Add(_pickPosBtn, 1, 3);
            layout.SetColumnSpan(_pickPosBtn, 2);

            // Anchor Row
            layout.Controls.Add(new Label { Text = "Anchor Point:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 4);
            _anchorCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
            foreach (var val in Enum.GetValues(typeof(NotificationAnchor))) _anchorCombo.Items.Add(val);
            _anchorCombo.SelectedItem = _rule.Anchor;
            layout.Controls.Add(_anchorCombo, 1, 4);

            // Priority Note
            var priorityNote = new Label { 
                Text = "* This anchor overrides your global default settings.", 
                Font = new Font(this.Font.FontFamily, 8, FontStyle.Italic),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Margin = new Padding(0, 5, 0, 0)
            };
            layout.Controls.Add(priorityNote, 1, 5);
            layout.SetColumnSpan(priorityNote, 2);

            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            _saveBtn = new Button { Text = "Save", DialogResult = DialogResult.OK };
            _cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveBtn.Click += SaveData;
            btnPanel.Controls.Add(_cancelBtn);
            btnPanel.Controls.Add(_saveBtn);

            // Hook up events
            _pathTxt.TextChanged += (s, e) => CheckForChanges();
            _xNum.ValueChanged += (s, e) => CheckForChanges();
            _yNum.ValueChanged += (s, e) => CheckForChanges();
            _widthNum.ValueChanged += (s, e) => CheckForChanges();
            _heightNum.ValueChanged += (s, e) => CheckForChanges();
            _anchorCombo.SelectedIndexChanged += (s, e) => CheckForChanges();

            this.Controls.Add(layout);
            this.Controls.Add(btnPanel);
        }

        private void LoadData()
        {
            _pathTxt.Text = _rule.ExecutablePath;
            _xNum.Value = _rule.XOffset;
            _yNum.Value = _rule.YOffset;
            _widthNum.Value = _rule.Width > 0 ? _rule.Width : _settings.DefaultWidth;
            _heightNum.Value = _rule.Height > 0 ? _rule.Height : _settings.DefaultHeight;
            _anchorCombo.SelectedItem = _rule.Anchor;
            
            CheckForChanges();
        }

        private void CheckForChanges()
        {
            if (_saveBtn == null) return;
            
            bool hasChanges = false;
            hasChanges |= _pathTxt.Text != _rule.ExecutablePath;
            hasChanges |= _xNum.Value != _rule.XOffset;
            hasChanges |= _yNum.Value != _rule.YOffset;
            
            decimal startWidth = _rule.Width > 0 ? _rule.Width : _settings.DefaultWidth;
            decimal startHeight = _rule.Height > 0 ? _rule.Height : _settings.DefaultHeight;
            hasChanges |= _widthNum.Value != startWidth;
            hasChanges |= _heightNum.Value != startHeight;
            
            hasChanges |= (NotificationAnchor)(_anchorCombo.SelectedItem ?? NotificationAnchor.BottomRight) != _rule.Anchor;

            _saveBtn.Enabled = hasChanges && !string.IsNullOrWhiteSpace(_pathTxt.Text);
        }

        private void PickExecutable(object? sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Executables (*.exe)|*.exe|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _pathTxt.Text = ofd.FileName;
                }
            }
        }

        private void PickPosition(object? sender, EventArgs e)
        {
            var tempSettings = new AppSettings();
            // Clone settings but use current form values for dimensions
            tempSettings.DefaultBackgroundColor = _settings.DefaultBackgroundColor;
            tempSettings.DefaultFontColor = _settings.DefaultFontColor;
            tempSettings.DefaultFontFamily = _settings.DefaultFontFamily;
            tempSettings.DefaultWidth = (int)_widthNum.Value;
            tempSettings.DefaultHeight = (int)_heightNum.Value;

            using (var overlay = new PositionPickerOverlay((int)_xNum.Value, (int)_yNum.Value, (int)_widthNum.Value, (int)_heightNum.Value, (NotificationAnchor)(_anchorCombo.SelectedItem ?? NotificationAnchor.BottomRight), tempSettings))
            {
                if (overlay.ShowDialog() == DialogResult.OK)
                {
                    _xNum.Value = overlay.ResultX;
                    _yNum.Value = overlay.ResultY;
                    _widthNum.Value = overlay.ResultWidth;
                    _heightNum.Value = overlay.ResultHeight;
                    _anchorCombo.SelectedItem = overlay.ResultAnchor;
                }
            }
        }

        private void SaveData(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pathTxt.Text))
            {
                MessageBox.Show("Please select an executable.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (!File.Exists(_pathTxt.Text))
            {
                MessageBox.Show("The selected file does not exist.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Check for duplicates
            var existing = _existingRules.FirstOrDefault(r => string.Equals(r.ExecutablePath, _pathTxt.Text, StringComparison.OrdinalIgnoreCase));
            if (existing != null && (_isNew || existing != _rule))
            {
                MessageBox.Show("A rule for this executable already exists.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            _rule.ExecutablePath = _pathTxt.Text;
            _rule.XOffset = (int)_xNum.Value;
            _rule.YOffset = (int)_yNum.Value;
            _rule.Width = (int)_widthNum.Value;
            _rule.Height = (int)_heightNum.Value;
            _rule.Anchor = (NotificationAnchor)(_anchorCombo.SelectedItem ?? NotificationAnchor.BottomRight);
        }

        public ExecutableOffsetRule Rule => _rule;
    }
}
