using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SimpleReminders.Models;

namespace SimpleReminders.Services
{
    public class ReminderManager
    {
        private List<Reminder> _reminders;
        private readonly string _filePath;
        private System.Threading.Timer? _timer;

        public event EventHandler<Reminder>? ReminderDue;

        public ReminderManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, "SimpleReminders");
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            
            _filePath = Path.Combine(appDir, "reminders.json");

            _reminders = LoadReminders();
        }

        public void Initialize()
        {
            CatchUpReminders();
            ScheduleNextReminder();
        }

       private void ScheduleNextReminder()
        {
            _timer?.Dispose();

            if (!_reminders.Any())
                return;

            var now = DateTime.Now;

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
            bool triggerAny = false;

            foreach (var reminder in _reminders)
            {
                // If the reminder is in the past and NOT marked as passed, it's a missed occurrence
                if (reminder.DueDate <= now && !reminder.IsPassed)
                {
                    // Trigger it instantly only if it triggers once a day or less (interval >= 1 day)
                    // or if it's a one-time reminder.
                    bool isDailyOrLessFrequent = !reminder.IsRecurring || reminder.RecurrenceInterval.TotalDays >= 1;

                    if (isDailyOrLessFrequent)
                    {
                        ReminderDue?.Invoke(this, reminder);
                        triggerAny = true;
                    }
                }

                EnsureValidNextOccurrence(reminder);
            }

            if (triggerAny)
                SaveReminders();
        }

        private void EnsureValidNextOccurrence(Reminder reminder)
        {
            var now = DateTime.Now;
            if (reminder.IsRecurring)
            {
                // Special case: if interval is 0, we can't move forward
                if (reminder.RecurrenceInterval <= TimeSpan.Zero) return;

                // Move forward if DueDate is in the past OR if it's not on an enabled day
                int attempts = 0;
                while (attempts < 100000)
                {
                    bool isPast = reminder.DueDate <= now;
                    bool isDayDisabled = reminder.EnabledDays != null && 
                                        reminder.EnabledDays.Count > 0 && 
                                        !reminder.EnabledDays.Contains(reminder.DueDate.DayOfWeek);

                    if (isPast || isDayDisabled)
                    {
                        reminder.DueDate = CalculateNextDueDate(reminder, reminder.DueDate);
                        attempts++;
                    }
                    else
                    {
                        break;
                    }
                }
                
                reminder.IsPassed = false;
            }
            else
            {
                if (reminder.DueDate <= now)
                {
                    reminder.IsPassed = true;
                }
                else
                {
                    reminder.IsPassed = false;
                }
            }
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
            EnsureValidNextOccurrence(reminder);
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
                
                // Ensure the new settings are valid
                EnsureValidNextOccurrence(reminder);
                
                _reminders[index] = reminder;

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

        private void TimerElapsed(object? state)
        {
            var now = DateTime.Now;

            var dueReminders = _reminders
                .Where(r => r.DueDate <= now && (!r.IsPassed || r.IsRecurring))
                .ToList();

            foreach (var reminder in dueReminders)
            {
                // Double check enabled days before triggering (extra safety)
                bool isDayEnabled = reminder.EnabledDays == null || 
                                   reminder.EnabledDays.Count == 0 || 
                                   reminder.EnabledDays.Contains(now.DayOfWeek);

                if (!reminder.IsPassed && isDayEnabled)
                    ReminderDue?.Invoke(this, reminder);

                if (reminder.IsRecurring)
                {
                    // Fast-forward to next occurrence
                    while (reminder.DueDate <= now)
                    {
                        reminder.DueDate = CalculateNextDueDate(reminder, reminder.DueDate);
                    }

                    reminder.IsPassed = false;
                }
                else
                {
                    if (reminder.DueDate <= now && !reminder.IsRecurring)
                        reminder.IsPassed = true;
                }
            }

            if (dueReminders.Count != 0)
                SaveReminders();

            ScheduleNextReminder();
        }

        private DateTime CalculateNextDueDate(Reminder reminder, DateTime fromDate)
        {
            var next = fromDate.Add(reminder.RecurrenceInterval);
            if (reminder.EnabledDays == null || reminder.EnabledDays.Count == 0)
                return next;

            // Safety break to prevent infinite loops (e.g., if interval is 0)
            if (reminder.RecurrenceInterval <= TimeSpan.Zero)
                return next;

            int attempts = 0;
            // Keep adding interval until we hit an enabled day
            // We limit attempts to 100,000 to prevent long freezes for very small intervals
            while (!reminder.EnabledDays.Contains(next.DayOfWeek) && attempts < 100000)
            {
                next = next.Add(reminder.RecurrenceInterval);
                attempts++;
            }
            return next;
        }
    }
}
