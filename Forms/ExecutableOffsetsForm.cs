using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SimpleReminders.Models;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class ExecutableOffsetsForm : Form
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;
        private List<ExecutableOffsetRule> _tempOffsets = new List<ExecutableOffsetRule>();

        private DataGridView _offsetGrid = null!;
        private Button _addOffsetBtn = null!;
        private Button _editOffsetBtn = null!;
        private Button _removeOffsetBtn = null!;
        private Button _saveBtn = null!;
        private Button _cancelBtn = null!;

        public ExecutableOffsetsForm(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = settingsService.Settings;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Offset for Applications";
            this.Icon = IconService.AppIcon;
            this.Size = new Size(500, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(15) };
            layout.RowCount = 2;
            layout.ColumnCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _offsetGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                MultiSelect = false,
                BackgroundColor = Color.White
            };
            _offsetGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ExecutableName", HeaderText = "Executable", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _offsetGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "XOffset", HeaderText = "X", Width = 40 });
            _offsetGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "YOffset", HeaderText = "Y", Width = 40 });
            _offsetGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Width", HeaderText = "W", Width = 40 });
            _offsetGrid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Height", HeaderText = "H", Width = 40 });
            
            layout.Controls.Add(_offsetGrid, 0, 0);

            var offsetBtnLayout = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Fill };
            _addOffsetBtn = new Button { Text = "Add", Width = 80 };
            _editOffsetBtn = new Button { Text = "Edit", Width = 80 };
            _removeOffsetBtn = new Button { Text = "Remove", Width = 80 };
            
            _addOffsetBtn.Click += AddOffsetRule;
            _editOffsetBtn.Click += EditOffsetRule;
            _removeOffsetBtn.Click += RemoveOffsetRule;

            offsetBtnLayout.Controls.Add(_addOffsetBtn);
            offsetBtnLayout.Controls.Add(_editOffsetBtn);
            offsetBtnLayout.Controls.Add(_removeOffsetBtn);
            layout.Controls.Add(offsetBtnLayout, 1, 0);

            var bottomPanel = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Bottom, Height = 40 };
            _cancelBtn = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
            _saveBtn = new Button { Text = "Save", DialogResult = DialogResult.OK };
            _saveBtn.Click += SaveData;
            bottomPanel.Controls.Add(_cancelBtn);
            bottomPanel.Controls.Add(_saveBtn);

            this.Controls.Add(layout);
            this.Controls.Add(bottomPanel);
        }

        private void LoadData()
        {
            _tempOffsets = _settings.ExecutableOffsets.Select(r => new ExecutableOffsetRule 
            { 
                ExecutablePath = r.ExecutablePath, 
                XOffset = r.XOffset, 
                YOffset = r.YOffset,
                Width = r.Width,
                Height = r.Height
            }).ToList();

            RefreshOffsetGrid();
        }

        private void RefreshOffsetGrid()
        {
            _offsetGrid.DataSource = null;
            _offsetGrid.DataSource = _tempOffsets;
        }

        private void AddOffsetRule(object? sender, EventArgs e)
        {
            using (var editor = new ExecutableOffsetEditorForm(_settings, _tempOffsets))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    _tempOffsets.Add(editor.Rule);
                    RefreshOffsetGrid();
                }
            }
        }

        private void EditOffsetRule(object? sender, EventArgs e)
        {
            if (_offsetGrid.SelectedRows.Count == 0) return;
            var rule = _offsetGrid.SelectedRows[0].DataBoundItem as ExecutableOffsetRule;
            if (rule == null) return;

            using (var editor = new ExecutableOffsetEditorForm(_settings, _tempOffsets, rule))
            {
                if (editor.ShowDialog() == DialogResult.OK)
                {
                    RefreshOffsetGrid();
                }
            }
        }

        private void RemoveOffsetRule(object? sender, EventArgs e)
        {
            if (_offsetGrid.SelectedRows.Count == 0) return;
            var rule = _offsetGrid.SelectedRows[0].DataBoundItem as ExecutableOffsetRule;
            if (rule == null) return;

            if (MessageBox.Show($"Are you sure you want to remove the rule for {rule.ExecutableName}?", "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _tempOffsets.Remove(rule);
                RefreshOffsetGrid();
            }
        }

        private void SaveData(object? sender, EventArgs e)
        {
            _settings.ExecutableOffsets.Clear();
            _settings.ExecutableOffsets.AddRange(_tempOffsets);
            _settingsService.SaveSettings();
            this.Close();
        }
    }
}
