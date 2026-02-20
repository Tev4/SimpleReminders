using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;
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
        private DoubleBufferedListBox _remindersList = null!;
        private Button _addButton = null!;
        private Button _editButton = null!;
        private Button _duplicateButton = null!;
        private Button _deleteButton = null!;
        private Button _debugButton = null!;
        private MenuStrip _menuStrip = null!;
        private ToolStripMenuItem _runOnStartupMenuItem = null!;
        private ToolStripMenuItem _minimizedToTrayMenuItem = null!;
        private readonly StartupService _startupService;
        private readonly SettingsService _settingsService;
        private int _dragInsertIndex = -1;

        public ManagerForm(ReminderManager reminderManager, SettingsService settingsService)
        {
            _reminderManager = reminderManager;
            _settingsService = settingsService;
            _startupService = new StartupService();
            InitializeComponent();
            RefreshList();
        }

        private void InitializeComponent()
        {
            this.Text = "Manage Reminders";
            this.Icon = IconService.AppIcon;
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

                var brush = (e.State & DrawItemState.Selected) == DrawItemState.Selected 
                    ? System.Drawing.Brushes.White 
                    : (reminder.IsPassed ? System.Drawing.Brushes.Gray : System.Drawing.Brushes.Black);
                e.Graphics.DrawString(reminder.ToString(), e.Font!, brush, e.Bounds);

                // Draw days info on the right
                string daysText = GetDaysDisplayString(reminder);
                if (!string.IsNullOrEmpty(daysText))
                {
                    var size = e.Graphics.MeasureString(daysText, e.Font!);
                    // Right-aligned with a small margin
                    float x = e.Bounds.Right - size.Width - 10;
                    float y = e.Bounds.Y + (e.Bounds.Height - size.Height) / 2;
                    e.Graphics.DrawString(daysText, e.Font!, brush, x, y);
                }
                
                // Draw insert line if dragging over this index
                if (_dragInsertIndex == e.Index)
                {
                    int lineY = e.Bounds.Top;
                    using var pen = new System.Drawing.Pen(System.Drawing.Color.Black, 1);
                    e.Graphics.DrawLine(pen, e.Bounds.Left, lineY, e.Bounds.Right, lineY);
                    e.DrawFocusRectangle();
                }
            };

            _remindersList.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    DeleteReminder(s, e);
                }
            };

            //Custom drag and drop cursor
            _remindersList.GiveFeedback += (s, e) =>
            {
                if ((e.Effect & DragDropEffects.Move) == DragDropEffects.Move)
                {
                    Cursor.Current = Cursors.HSplit;
                    e.UseDefaultCursors = false; // important: prevents default cursor
                }
                else
                {
                    Cursor.Current = Cursors.Default;
                    e.UseDefaultCursors = true;
                }
            };

            _remindersList.DoubleClick += (s, e) =>
            {
                int index = _remindersList.IndexFromPoint(_remindersList.PointToClient(Cursor.Position));

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

            _remindersList.DragDrop += (s, e) =>
            {
                if (e.Data?.GetData(typeof(Reminder)) is Reminder draggedReminder)
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
            btnPanel.Dock = DockStyle.Top;
            btnPanel.AutoSize = true;
            btnPanel.FlowDirection = FlowDirection.LeftToRight;

            _addButton = new Button { Text = "Add" };
            _addButton.Click += AddReminder;
            
            _editButton = new Button { Text = "Edit" };
            _editButton.Click += EditReminder;

            _duplicateButton = new Button { Text = "Duplicate" };
            _duplicateButton.Click += DuplicateReminder;
            
            _deleteButton = new Button { Text = "Delete" };
            _deleteButton.Click += DeleteReminder;

            _debugButton = new Button { Text = "Trigger Now", AutoSize = true };
            _debugButton.Click += TriggerDebug;

            btnPanel.Controls.Add(_addButton);
            btnPanel.Controls.Add(_editButton);
            btnPanel.Controls.Add(_duplicateButton);
            btnPanel.Controls.Add(_deleteButton);
            btnPanel.Controls.Add(_debugButton);

            layout.Controls.Add(btnPanel, 0, 1);

            // MenuStrip
            _menuStrip = new MenuStrip();
            var optionsMenu = new ToolStripMenuItem("Options");
            
            _runOnStartupMenuItem = new ToolStripMenuItem("Run On Startup")
            {
                CheckOnClick = true,
                Checked = _startupService.IsStartupEnabled()
            };
            _runOnStartupMenuItem.CheckedChanged += (s, e) =>
            {
                _startupService.SetStartup(_runOnStartupMenuItem.Checked);
            };

            optionsMenu.DropDownItems.Add(_runOnStartupMenuItem);
            
            // Start Minimized To Tray logic
            _minimizedToTrayMenuItem = new ToolStripMenuItem("Start Minimized To Tray")
            {
                CheckOnClick = true,
                Checked = _settingsService.Settings.StartMinimized
            };
            _minimizedToTrayMenuItem.CheckedChanged += (s, e) =>
            {
                _settingsService.Settings.StartMinimized = _minimizedToTrayMenuItem.Checked;
                _settingsService.SaveSettings();
            };

            optionsMenu.DropDownItems.Add(_minimizedToTrayMenuItem);

            optionsMenu.DropDownItems.Add(new ToolStripSeparator());
            var defaultSettingsMenuItem = new ToolStripMenuItem("Default Notification Settings");
            defaultSettingsMenuItem.Click += (s, e) =>
            {
                using (var form = new SettingsForm(_settingsService))
                {
                    form.ShowDialog();
                }
            };
            optionsMenu.DropDownItems.Add(defaultSettingsMenuItem);

            _menuStrip.Items.Add(optionsMenu);

            this.MainMenuStrip = _menuStrip;
            this.Controls.Add(_menuStrip); // MenuStrip at top
            
            // Adjust layout to be below MenuStrip
            layout.Padding = new Padding(10, _menuStrip.Height + 10, 10, 10);
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

        private void AddReminder(object? sender, EventArgs e)
        {
            var form = new EditReminderForm(_settingsService);
            if (form.ShowDialog() == DialogResult.OK)
            {
                // Store the index of the new reminder
                _reminderManager.Add(form.Reminder);
                RefreshList();

                // Find the newly added reminder's index
                int newIndex = _remindersList.Items.IndexOf(form.Reminder);
                
                // Select the newly added reminder
                if (newIndex >= 0)
                {
                    _remindersList.SelectedIndex = newIndex;
                }
            }
        }

        private void EditReminder(object? sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                // Store the index of the selected reminder before editing
                int selectedIndex = _remindersList.SelectedIndex;

                var form = new EditReminderForm(_settingsService, reminder);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _reminderManager.Update(form.Reminder);
                    RefreshList();

                    // After the form closes, reselect the same item
                    if (selectedIndex >= 0 && selectedIndex < _remindersList.Items.Count)
                    {
                        _remindersList.SelectedIndex = selectedIndex;
                    }
                }
            }
            else
            {
                MessageBox.Show("No reminder selected to edit.");
            }
        }

        private void DeleteReminder(object? sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                if (MessageBox.Show("Delete this reminder?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _reminderManager.Remove(reminder.Id);
                    RefreshList();
                }
            }
            else
            {
                MessageBox.Show("No reminder selected to delete.");
            }
        }

        private void TriggerDebug(object? sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder reminder)
            {
                _reminderManager.TriggerReminder(reminder.Id);
            }
        }

        private void DuplicateReminder(object? sender, EventArgs e)
        {
            if (_remindersList.SelectedItem is Reminder selectedReminder)
            {
                // Store the index of the selected item before refreshing the list
                int selectedIndex = _remindersList.SelectedIndex;

                var duplicatedReminder = new Reminder
                {
                    Id = Guid.NewGuid(),

                    Title = selectedReminder.Title,
                    Message = selectedReminder.Message,
                    BackgroundColor = selectedReminder.BackgroundColor,
                    FontColor = selectedReminder.FontColor,
                    FontSize = selectedReminder.FontSize,
                    Width = selectedReminder.Width,
                    Height = selectedReminder.Height,
                    IsRecurring = selectedReminder.IsRecurring,
                    RecurrenceInterval = selectedReminder.RecurrenceInterval,
                    DueDate = selectedReminder.DueDate,
                    EnabledDays = new List<DayOfWeek>(selectedReminder.EnabledDays),
                    IsPassed = selectedReminder.IsPassed,
                    SoundPath = selectedReminder.SoundPath
                };

                _reminderManager.Add(duplicatedReminder);
                RefreshList();

                // Ensure the same item is selected
                if (selectedIndex >= 0 && selectedIndex < _remindersList.Items.Count)
                {
                    _remindersList.SelectedIndex = selectedIndex;
                }
            }
            else
            {
                MessageBox.Show("No reminder selected to duplicate.");
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

        private string GetDaysDisplayString(Reminder reminder)
        {
            if (!reminder.IsRecurring || reminder.EnabledDays == null || reminder.EnabledDays.Count == 0)
                return string.Empty;

            if (reminder.EnabledDays.Count == 7)
                return "(Every day)";

            var dayMap = new System.Collections.Generic.Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Mo" }, { DayOfWeek.Tuesday, "Tu" }, { DayOfWeek.Wednesday, "We" },
                { DayOfWeek.Thursday, "Th" }, { DayOfWeek.Friday, "Fr" }, { DayOfWeek.Saturday, "Sa" },
                { DayOfWeek.Sunday, "Su" }
            };

            var orderedDays = reminder.EnabledDays
                .OrderBy(d => (int)d == 0 ? 7 : (int)d) // Mon-Sun order
                .Select(d => dayMap[d]);

            return string.Join(", ", orderedDays);
        }
    }
}
