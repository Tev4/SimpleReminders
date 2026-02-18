using System;

namespace SimpleReminders.Models
{
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        
        // Customization
        public string BackgroundColor { get; set; } = "#005FB8";
        public string FontColor { get; set; } = "#FFFFFF"; 
        public float FontSize { get; set; } = 14f;
        
        // Recurrence
        public bool IsRecurring { get; set; }
        public TimeSpan RecurrenceInterval { get; set; }
        
        // Scheduling
        public DateTime DueDate { get; set; }

        // Due date passed
        public bool IsPassed { get; set; } = false;
        
        // Sound
        public string SoundPath { get; set; } = string.Empty; // Path to custom sound or null for default

        public Reminder() { }

        public override string ToString()
        {
            return IsPassed ? $"{Title} (Passed)" : Title;
        }
    }
}
