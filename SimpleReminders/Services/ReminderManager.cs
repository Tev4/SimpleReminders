using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
// using System.Timers; // Removed to avoid conflict, switched to Windows.Forms.Timer
using SimpleReminders.Models;

namespace SimpleReminders.Services
{
    public class ReminderManager
    {
        private List<Reminder> _reminders;
        private readonly string _filePath;
        private readonly System.Windows.Forms.Timer _timer; // UI Thread Timer

        public event EventHandler<Reminder> ReminderDue;

        public ReminderManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, "SimpleReminders");
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            
            _filePath = Path.Combine(appDir, "reminders.json");
            _reminders = LoadReminders();

            // Check every minute
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 60000;
            _timer.Tick += CheckReminders;
            _timer.Start();
        }

        private List<Reminder> LoadReminders()
        {
            if (!File.Exists(_filePath)) return new List<Reminder>();
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Reminder>>(json) ?? new List<Reminder>();
            }
            catch
            {
                return new List<Reminder>();
            }
        }

        public void SaveReminders()
        {
            string json = JsonSerializer.Serialize(_reminders, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public List<Reminder> GetAll() => _reminders.ToList();

        public void Add(Reminder reminder)
        {
            _reminders.Add(reminder);
            SaveReminders();
        }

        public void Remove(Guid id)
        {
            var r = _reminders.FirstOrDefault(x => x.Id == id);
            if (r != null)
            {
                _reminders.Remove(r);
                SaveReminders();
            }
        }
        
        public void Update(Reminder reminder)
        {
            var existing = _reminders.FirstOrDefault(x => x.Id == reminder.Id);
            if (existing != null)
            {
                // We want to keep the same index? Typically "Update" means Modify in place.
                // The previous code did remove + add ( append to end which is why it goes to bottom? No, wait)
                // "New ones should go at the bottom"
                // "Reminders are ordered by last edited/created" -> User wants drag/drop order.
                
                // If we want to preserve order on edit:
                int index = _reminders.IndexOf(existing);
                _reminders[index] = reminder;
                SaveReminders();
            }
        }

        public void Move(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= _reminders.Count || 
                newIndex < 0 || newIndex >= _reminders.Count) return;

            var item = _reminders[oldIndex];
            _reminders.RemoveAt(oldIndex);
            _reminders.Insert(newIndex, item);
            SaveReminders();
        }

        public void TriggerReminder(Guid id)
        {
            var reminder = _reminders.FirstOrDefault(x => x.Id == id);
            if (reminder != null)
            {
                ReminderDue?.Invoke(this, reminder);
            }
        }

        private void CheckReminders(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            // Find due reminders
            var dueReminders = _reminders.Where(r => r.DueDate <= now).ToList();

            foreach (var reminder in dueReminders)
            {
                ReminderDue?.Invoke(this, reminder);

                if (reminder.IsRecurring)
                {
                    reminder.DueDate = now.Add(reminder.RecurrenceInterval);
                }
                else
                {
                    _reminders.Remove(reminder);
                }
            }
            
            if (dueReminders.Any()) SaveReminders();
        }
    }
}
