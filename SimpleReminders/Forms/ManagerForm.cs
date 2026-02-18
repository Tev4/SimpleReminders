using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using SimpleReminders.Models;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class DoubleBufferedListBox : ListBox
    {
        public DoubleBufferedListBox()
        {
            // Enable double buffering
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | 
                        ControlStyles.AllPaintingInWmPaint, true);
            this.UpdateStyles();
        }
    }

    public class ManagerForm : Form
    {
        private readonly ReminderManager _reminderManager;
        private DoubleBufferedListBox _remindersList;
        private Button _addButton;
        private Button _editButton;
        private Button _deleteButton;
        private Button _debugButton;
        private int _dragInsertIndex = -1;


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
            _remindersList = new DoubleBufferedListBox();
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
                
                // Draw insert line if dragging over this index
                if (_dragInsertIndex == e.Index)
                {
                    int lineY = e.Bounds.Top;
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1))
                    {
                        e.Graphics.DrawLine(pen, e.Bounds.Left, lineY, e.Bounds.Right, lineY);
                    }
                }

                e.DrawFocusRectangle();
            };

            _remindersList.DoubleClick += (s, e) =>
            {
                // Get the index of the item that was clicked
                int index = _remindersList.IndexFromPoint(_remindersList.PointToClient(Cursor.Position));

                // Only edit if the clicked item is the currently selected item
                if (index >= 0 && index == _remindersList.SelectedIndex)
                {
                    EditReminder(s, e);
                }
            };

            _remindersList.AllowDrop = true;

            Point _mouseDownLocation = Point.Empty;

            _remindersList.MouseDown += (s, e) =>
            {
                int index = _remindersList.IndexFromPoint(e.Location);

                // Unselect if clicked on empty space
                if (index < 0)
                {
                    _remindersList.ClearSelected();
                    _mouseDownLocation = Point.Empty;
                    return;
                }

                // Remember the mouse down location for drag detection
                _mouseDownLocation = e.Location;
            };

            _remindersList.MouseMove += (s, e) =>
            {
                // Only start drag if mouse was pressed and moved enough
                if (_mouseDownLocation == Point.Empty)
                    return;

                // Calculate distance moved
                int dx = Math.Abs(e.X - _mouseDownLocation.X);
                int dy = Math.Abs(e.Y - _mouseDownLocation.Y);

                // Windows Forms standard drag threshold
                if (dx >= SystemInformation.DragSize.Width || dy >= SystemInformation.DragSize.Height)
                {
                    int index = _remindersList.IndexFromPoint(_mouseDownLocation);
                    if (index >= 0)
                    {
                        _remindersList.DoDragDrop(_remindersList.Items[index], DragDropEffects.Move);
                        _mouseDownLocation = Point.Empty; // reset
                    }
                }
            };

            _remindersList.MouseUp += (s, e) =>
            {
                // Reset mouse down location on mouse release
                _mouseDownLocation = Point.Empty;
            };

            _remindersList.DragOver += (s, e) =>
            {
                e.Effect = DragDropEffects.Move;
                Point point = _remindersList.PointToClient(Cursor.Position);

                int index = _remindersList.IndexFromPoint(point);
                if (index < 0)
                    index = _remindersList.Items.Count;
                else
                {
                    var itemBounds = _remindersList.GetItemRectangle(index);
                    if (point.Y > itemBounds.Top + itemBounds.Height / 2) index++;
                }

                if (index != _dragInsertIndex)
                {
                    // Invalidate only old and new insert lines
                    if (_dragInsertIndex >= 0 && _dragInsertIndex < _remindersList.Items.Count)
                    {
                        _remindersList.Invalidate(_remindersList.GetItemRectangle(_dragInsertIndex));
                    }
                    if (index >= 0 && index < _remindersList.Items.Count)
                    {
                        _remindersList.Invalidate(_remindersList.GetItemRectangle(index));
                    }

                    _dragInsertIndex = index;
                }
            };

            // DragDrop: handle the drop logic
            _remindersList.DragDrop += (s, e) =>
            {
                if (e.Data.GetData(typeof(Reminder)) is Reminder draggedReminder)
                {
                    int oldIndex = _remindersList.Items.IndexOf(draggedReminder);
                    int targetIndex = _dragInsertIndex;

                    // Adjust targetIndex if moving downward in the list
                    if (oldIndex < targetIndex) targetIndex--;

                    if (oldIndex != targetIndex)
                    {
                        _reminderManager.Move(oldIndex, targetIndex);
                        RefreshList();
                        _remindersList.SelectedIndex = targetIndex;
                    }
                }

                // Reset insert indicator
                _dragInsertIndex = -1;
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
