using System;

namespace SimpleReminders.Models
{
    public class AppSettings
    {
        public bool StartMinimized { get; set; } = true;
        public bool HasInitializedStartup { get; set; } = false;

        // Default Reminder Settings
        public string DefaultBackgroundColor { get; set; } = "#005FB8";
        public string DefaultFontColor { get; set; } = "#FFFFFF";
        public float DefaultFontSize { get; set; } = 14f;
        public int DefaultWidth { get; set; } = 250;
        public int DefaultHeight { get; set; } = 80;
        public string DefaultSoundPath { get; set; } = string.Empty;
    }
}
