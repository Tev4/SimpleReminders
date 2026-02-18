using System;

namespace SimpleReminders.Models
{
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        // Customization
        public string BackgroundColor { get; set; } = "#005FB8"; // Default blue
        public string FontColor { get; set; } = "#FFFFFF"; // Default white
        public float FontSize { get; set; } = 14f;
        
        // Recurrence
        public bool IsRecurring { get; set; }
        public TimeSpan RecurrenceInterval { get; set; } // For custom recurring intervals
        
        // Scheduling
        public DateTime DueDate { get; set; }

        // Due date passed
        public bool IsPassed { get; set; } = false;
        
        // Sound
        public string SoundPath { get; set; } // Path to custom sound or null for default

        public Reminder() { }

        public Reminder(string title, string message, DateTime dueDate)
        {
            Title = title;
            Message = message;
            DueDate = dueDate;
        }

        public override string ToString()
        {
            return IsPassed ? $"{Title} (Passed)" : Title;
        }
    }
}
