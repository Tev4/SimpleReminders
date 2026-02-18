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
        private System.Threading.Timer _timer;

        public event EventHandler<Reminder> ReminderDue;

        public ReminderManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, "SimpleReminders");
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            
            _filePath = Path.Combine(appDir, "reminders.json");

            _reminders = LoadReminders();
            CatchUpReminders();
            ScheduleNextReminder();
        }

       private void ScheduleNextReminder()
        {
            _timer?.Dispose();

            if (!_reminders.Any())
                return;

            var now = DateTime.Now;

            // Only consider reminders that are either:
            // - recurring (always schedule next)
            // - non-recurring and not passed
            var nextReminder = _reminders
                .Where(r => r.IsRecurring || !r.IsPassed)
                .OrderBy(r => r.DueDate)
                .FirstOrDefault();

            if (nextReminder == null)
                return;

            var delay = nextReminder.DueDate - now;

            // If the next reminder is still in the past for some reason (recurring), set 0
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;

            _timer = new System.Threading.Timer(
                TimerElapsed,
                null,
                delay,
                Timeout.InfiniteTimeSpan
            );
        }

        private void CatchUpReminders()
        {
            var now = DateTime.Now;
            bool updated = false;

            foreach (var reminder in _reminders)
            {
                if (reminder.IsRecurring)
                {
                    // Fast-forward recurring reminders to the next due date in the future
                    while (reminder.DueDate <= now)
                    {
                        reminder.DueDate = reminder.DueDate.Add(reminder.RecurrenceInterval);
                        updated = true;
                    }

                    reminder.IsPassed = false; // recurring reminders are always upcoming
                }
                else
                {
                    // Mark non-recurring past reminders as passed
                    if (reminder.DueDate <= now)
                    {
                        reminder.IsPassed = true;
                        updated = true;
                    }
                }
            }

            if (updated)
                SaveReminders();
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
            ScheduleNextReminder();
        }

        public void Remove(Guid id)
        {
            var r = _reminders.FirstOrDefault(x => x.Id == id);
            if (r != null)
            {
                _reminders.Remove(r);
                SaveReminders();
                ScheduleNextReminder();
            }
        }
        
        public void Update(Reminder reminder)
        {
            var existing = _reminders.FirstOrDefault(x => x.Id == reminder.Id);
            if (existing != null)
            {
                int index = _reminders.IndexOf(existing);
                _reminders[index] = reminder;

                // If the reminder is non-recurring and its DueDate is in the past, mark as passed
                if (!reminder.IsRecurring && reminder.DueDate <= DateTime.Now)
                {
                    reminder.IsPassed = true;
                }
                else
                {
                    reminder.IsPassed = false;
                }

                SaveReminders();
                ScheduleNextReminder();
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

        private void TimerElapsed(object state)
        {
            var now = DateTime.Now;

            // Only reminders that are due and actionable
            var dueReminders = _reminders
                .Where(r => r.DueDate <= now && (!r.IsPassed || r.IsRecurring))
                .ToList();

            foreach (var reminder in dueReminders)
            {
                // Only fire for actionable reminders
                if (!reminder.IsPassed)
                    ReminderDue?.Invoke(this, reminder);

                if (reminder.IsRecurring)
                {
                    // Fast-forward to next occurrence
                    while (reminder.DueDate <= now)
                    {
                        reminder.DueDate = reminder.DueDate.Add(reminder.RecurrenceInterval);
                    }

                    reminder.IsPassed = false; // recurring reminders are always active
                }
                else
                {
                    // Non-recurring passed reminders: do not remove
                    if (reminder.DueDate <= now && !reminder.IsRecurring)
                        reminder.IsPassed = true;
                }
            }

            if (dueReminders.Any())
                SaveReminders();

            ScheduleNextReminder(); // schedule next exact one
        }
    }
}
