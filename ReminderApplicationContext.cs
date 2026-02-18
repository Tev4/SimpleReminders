using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Media;
using SimpleReminders.Services;
using SimpleReminders.Forms;

namespace SimpleReminders
{
    public class ReminderApplicationContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;
        private ReminderManager _reminderManager;
        private NotificationWindowManager _notificationWindowManager;
        private ManagerForm? _managerForm;
        private readonly Control _uiContext;

        public ReminderApplicationContext()
        {
            _reminderManager = new ReminderManager();
            _reminderManager.ReminderDue += OnReminderDue;

            _notificationWindowManager = new NotificationWindowManager();

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                Text = "Simple Reminders"
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, ShowManager);
            contextMenu.Items.Add("Exit", null, ExitApp);
            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.DoubleClick += ShowManager;

            // Create a hidden control on the UI thread for marshaling
            _uiContext = new Control();
            _uiContext.CreateControl();
        }

        private void ShowManager(object? sender, EventArgs e)
        {
            if (_managerForm == null || _managerForm.IsDisposed)
            {
                _managerForm = new ManagerForm(_reminderManager);
            }
            
            if (!_managerForm.Visible)
            {
                _managerForm.Show();
            }
            else
            {
                _managerForm.Activate();
            }
        }

        private void ExitApp(object? sender, EventArgs e)
        {
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void OnReminderDue(object? sender, Models.Reminder reminder)
        {
            // Marshal to UI thread
            _uiContext.BeginInvoke(new Action(() =>
            {
                PlaySound(reminder.SoundPath);
                _notificationWindowManager.ShowNotification(reminder);
            }));
        }

        private static void PlaySound(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using var player = new SoundPlayer(path);
                    player.Play();
                }
                else
                {
                     SystemSounds.Exclamation.Play();
                }
            }
            catch 
            {
                SystemSounds.Beep.Play();
            }
        }
    }
}
