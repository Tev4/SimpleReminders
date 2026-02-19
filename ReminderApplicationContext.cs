using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Media;
using SimpleReminders.Services;
using SimpleReminders.Forms;
using System.Reflection;

namespace SimpleReminders
{
    public class ReminderApplicationContext : ApplicationContext
    {
        private NotifyIcon _notifyIcon;
        private ReminderManager _reminderManager;
        private NotificationWindowManager _notificationWindowManager;
        private ManagerForm? _managerForm;
        private readonly Control _uiContext;
        private readonly SettingsService _settingsService;
        private readonly StartupService _startupService;

        public ReminderApplicationContext()
        {
            _reminderManager = new ReminderManager();
            _reminderManager.ReminderDue += OnReminderDue;

            _notificationWindowManager = new NotificationWindowManager();
            _settingsService = new SettingsService();
            _startupService = new StartupService();

            // Enable startup by default on first run
            if (!_settingsService.Settings.HasInitializedStartup)
            {
                if (!_startupService.IsStartupEnabled())
                {
                    _startupService.SetStartup(true);
                }
                _settingsService.Settings.HasInitializedStartup = true;
                _settingsService.SaveSettings();
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = IconService.AppIcon,
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

            // Show manager if not starting minimized
            if (!_settingsService.Settings.StartMinimized)
            {
                ShowManager(null, EventArgs.Empty);
            }
        }

        private void ShowManager(object? sender, EventArgs e)
        {
            if (_managerForm == null || _managerForm.IsDisposed)
            {
                _managerForm = new ManagerForm(_reminderManager, _settingsService);
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
