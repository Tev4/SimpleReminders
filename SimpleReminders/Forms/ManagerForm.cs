using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using SimpleReminders.Models;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class ManagerForm : Form
    {
        private readonly ReminderManager _reminderManager;
        private ListBox _remindersList;
        private Button _addButton;
        private Button _editButton;
        private Button _deleteButton;
        private Button _debugButton;

        public ManagerForm(ReminderManager reminderManager)
        {
            _reminderManager = reminderManager;
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Reminders";
            this.Size = new System.Drawing.Size(500, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            
            // Layout
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Padding = new Padding(10);
            layout.RowCount = 2;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // List
            _remindersList = new ListBox();
            _remindersList.Dock = DockStyle.Fill;
            _remindersList.DisplayMember = "Title"; 
            _remindersList.AllowDrop = true;

            // Grey out passed reminders
            _remindersList.DrawMode = DrawMode.OwnerDrawFixed;
            _remindersList.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                var reminder = (Reminder)_remindersList.Items[e.Index];
                e.DrawBackground();
                var brush = reminder.IsPassed ? System.Drawing.Brushes.Gray : System.Drawing.Brushes.Black;
                e.Graphics.DrawString(reminder.ToString(), e.Font, brush, e.Bounds);
                e.DrawFocusRectangle();
            };
            
            // Double-click to edit
            _remindersList.DoubleClick += (s, e) => {
                if (_remindersList.SelectedItem is Reminder reminder)
                {
                    var form = new EditReminderForm(reminder);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _reminderManager.Update(form.Reminder);
                        RefreshList();
                    }
                }
            };
            
            _remindersList.MouseDown += (s, e) => {
                if (_remindersList.SelectedItem == null) return;
                _remindersList.Cursor = Cursors.Hand;
                _remindersList.DoDragDrop(_remindersList.SelectedItem, DragDropEffects.Move);
                _remindersList.Cursor = Cursors.Default;
            };
            
            _remindersList.DragOver += (s, e) => {
                e.Effect = DragDropEffects.Move;
                // Draw drop indicator line
                Point point = _remindersList.PointToClient(new System.Drawing.Point(e.X, e.Y));
                int index = _remindersList.IndexFromPoint(point);
                _remindersList.Invalidate(); // Trigger repaint to show drop line
            };
            
            _remindersList.DragDrop += (s, e) => {
                Point point = _remindersList.PointToClient(new System.Drawing.Point(e.X, e.Y));
                int index = _remindersList.IndexFromPoint(point);
                if (index < 0) index = _remindersList.Items.Count - 1;
                
                if (e.Data.GetDataPresent(typeof(Reminder)))
                {
                    Reminder r = (Reminder)e.Data.GetData(typeof(Reminder));
                    
                   int oldIndex = -1;
                    for(int i=0; i<_remindersList.Items.Count; i++)
                    {
                        if (((Reminder)_remindersList.Items[i]).Id == r.Id) 
                        {
                            oldIndex = i; 
                            break;
                        }
                    }

                    if (oldIndex != index && oldIndex != -1 && index != -1)
                    {
                        _reminderManager.Move(oldIndex, index);
                        RefreshList();
                        _remindersList.SelectedIndex = index;
                    }
                }
                _remindersList.Invalidate();
            };
            
            layout.Controls.Add(_remindersList, 0, 0);

            // Buttons
            var btnPanel = new FlowLayoutPanel();
            btnPanel.Dock = DockStyle.Top; // Changed from Fill to make AutoSize logic cleaner
            btnPanel.AutoSize = true;
            btnPanel.FlowDirection = FlowDirection.LeftToRight;

            _addButton = new Button { Text = "Add" };
            _addButton.Click += AddReminder;
            
            _editButton = new Button { Text = "Edit" };
            _editButton.Click += EditReminder;
            
            _deleteButton = new Button { Text = "Delete" };
            _deleteButton.Click += DeleteReminder;

            _debugButton = new Button { Text = "Trigger Now", AutoSize = true };
            _debugButton.Click += TriggerDebug;

            btnPanel.Controls.Add(_addButton);
            btnPanel.Controls.Add(_editButton);
            btnPanel.Controls.Add(_deleteButton);
            btnPanel.Controls.Add(_debugButton);

            layout.Controls.Add(btnPanel, 0, 1);
            this.Controls.Add(layout);
        }

        private void RefreshList()
        {
            _remindersList.Items.Clear();
            var reminders = _reminderManager.GetAll();
            foreach (var r in reminders)
            {
                _remindersList.Items.Add(r); 
            }
        }

        private void AddReminder(object sender, EventArgs e)
        {
            var form = new EditReminderForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                _reminderManager.Add(form.Reminder);
                RefreshList();
            }
        }

        private void EditReminder(object sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                var form = new EditReminderForm(reminder);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _reminderManager.Update(form.Reminder);
                    RefreshList();
                }
            }
        }

        private void DeleteReminder(object sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                if (MessageBox.Show("Delete this reminder?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _reminderManager.Remove(reminder.Id);
                    RefreshList();
                }
            }
        }

        private void TriggerDebug(object sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                _reminderManager.TriggerReminder(reminder.Id);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}
