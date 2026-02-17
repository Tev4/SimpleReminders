using System;

namespace SimpleReminders.Models
{
    public enum RecurrenceType
    {
        None,
        CustomInterval,
        OnComputerStart,
        OnUserLogin,
        OnTheHour,
        OnAppLaunch
    }

    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Message { get; set; } = string.Empty;
        
        // Customization
        public string BackgroundColor { get; set; } = "#005FB8"; // Default blue
        public string FontColor { get; set; } = "#FFFFFF"; // Default white
        public float FontSize { get; set; } = 11f;
        
        // Recurrence
        public bool IsRecurring { get; set; }
        public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
        public TimeSpan RecurrenceInterval { get; set; } // For custom recurring intervals
        
        // Scheduling
        public DateTime DueDate { get; set; }
        
        // Sound
        public string SoundPath { get; set; } // Path to custom sound or null for default

        public Reminder() { }

        public Reminder(string message, DateTime dueDate)
        {
            Message = message;
            DueDate = dueDate;
        }

        public override string ToString()
        {
            return $"{DueDate:g} - {Message}";
        }
    }
}
