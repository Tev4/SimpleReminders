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
        private readonly bool _isNew;

        private TextBox _pathTxt = null!;
        private NumericUpDown _xNum = null!;
        private NumericUpDown _yNum = null!;
        private Button _browseBtn = null!;
        private Button _saveBtn = null!;
        private Button _cancelBtn = null!;

        public ExecutableOffsetEditorForm(IEnumerable<ExecutableOffsetRule> existingRules, ExecutableOffsetRule? rule = null)
        {
            _existingRules = existingRules;
            if (rule == null)
            {
                _rule = new ExecutableOffsetRule();
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
            this.Size = new Size(400, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15), RowCount = 4, ColumnCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            layout.Controls.Add(new Label { Text = "Executable:", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
            _pathTxt = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
            _browseBtn = new Button { Text = "Browse...", AutoSize = true };
            _browseBtn.Click += PickExecutable;
            layout.Controls.Add(_pathTxt, 1, 0);
            layout.Controls.Add(_browseBtn, 2, 0);

            layout.Controls.Add(new Label { Text = "X Offset (px):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
            _xNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 80 };
            layout.Controls.Add(_xNum, 1, 1);

            layout.Controls.Add(new Label { Text = "Y Offset (px):", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
            _yNum = new NumericUpDown { Minimum = -2000, Maximum = 2000, Width = 80 };
            layout.Controls.Add(_yNum, 1, 2);

            var btnPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            _saveBtn = new Button { Text = "Save", DialogResult = DialogResult.OK };
            _cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveBtn.Click += SaveData;
            btnPanel.Controls.Add(_cancelBtn);
            btnPanel.Controls.Add(_saveBtn);

            this.Controls.Add(layout);
            this.Controls.Add(btnPanel);
        }

        private void LoadData()
        {
            _pathTxt.Text = _rule.ExecutablePath;
            _xNum.Value = _rule.XOffset;
            _yNum.Value = _rule.YOffset;
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
        }

        public ExecutableOffsetRule Rule => _rule;
    }
}
