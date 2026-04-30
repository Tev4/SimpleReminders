using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleReminders.Models
{
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        private string _title = string.Empty;
        public string Title 
        { 
            get => _title; 
            set => _title = value?.Length > 100 ? value.Substring(0, 100) : value ?? string.Empty; 
        }
        private string _message = string.Empty;
        public string Message 
        { 
            get => _message; 
            set => _message = value?.Length > 1000 ? value.Substring(0, 1000) : value ?? string.Empty; 
        }
        
        // Customization
        public string BackgroundColor { get; set; } = "#005FB8";
        public string FontColor { get; set; } = "#FFFFFF"; 
        public float FontSize { get; set; } = 14f;
        public string FontFamily { get; set; } = string.Empty;
        public int Width { get; set; } = 250;
        public int Height { get; set; } = 80;
        
        // Recurrence
        public bool IsRecurring { get; set; }
        public TimeSpan RecurrenceInterval { get; set; }
        
        // Days of the week (if empty, all days are allowed)
        public List<DayOfWeek> EnabledDays { get; set; } = [];
        
        // Scheduling
        public DateTime DueDate { get; set; }

        // Due date passed
        public bool IsPassed { get; set; } = false;

        // Is the reminder enabled
        public bool IsEnabled { get; set; } = true;
        
        // Sound
        public string SoundPath { get; set; } = string.Empty; // Path to custom sound or null for default

        // Auto-dismiss
        public bool AutoFade { get; set; } = false;
        public int DisplayDurationSeconds { get; set; } = 15;

        // Missing handling
        public bool ShowOnStartupIfMissed { get; set; } = false;

        public Reminder() { }

        public override string ToString()
        {
            return IsPassed ? $"{Title} (Passed)" : Title;
        }
    }
}
