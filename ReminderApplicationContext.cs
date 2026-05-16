using System;

using System.Windows.Forms;
using System.IO;
using System.Media;
using SimpleReminders.Services;
using SimpleReminders.Forms;

using System.Runtime.InteropServices;
using System.Threading.Tasks;

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
        
        [DllImport("user32.dll")]
        private static extern void DisableProcessWindowsGhosting();

        public ReminderApplicationContext()
        {
            _reminderManager = new ReminderManager();
            _reminderManager.ReminderDue += OnReminderDue;

            _settingsService = new SettingsService();
            _notificationWindowManager = new NotificationWindowManager(_settingsService);
            _startupService = new StartupService();

            // Optimization: Set process priority to High to ensure the OS gives us CPU time during heavy gaming
            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            {
                process.PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            }

            // Optimization: Disable ghosting so Windows doesn't try to "help" if the UI thread is briefly busy
            DisableProcessWindowsGhosting();

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

            // Initialize after dependencies (uiContext, notificationWindowManager) are ready
            _reminderManager.Initialize();

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
            // Play sound on a background thread to prevent any I/O blocking from causing lag spikes
            System.Threading.Tasks.Task.Run(() => PlaySound(reminder.SoundPath));

            // Marshal UI creation to UI thread
            _uiContext.BeginInvoke(new Action(() =>
            {
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
