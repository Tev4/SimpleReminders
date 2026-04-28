using System;
using System.Collections.Generic;

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
        public string DefaultFontFamily { get; set; } = "Segoe UI Variable Display";
        public int DefaultWidth { get; set; } = 250;
        public int DefaultHeight { get; set; } = 80;
        public int DefaultOffsetX { get; set; } = 0;
        public int DefaultOffsetY { get; set; } = 0;
        public string DefaultSoundPath { get; set; } = string.Empty;
        public bool DefaultAutoFade { get; set; } = true;
        public int DefaultFadeDelay { get; set; } = 15;
        public bool DefaultFireIfMissed { get; set; } = false;

        public List<ExecutableOffsetRule> ExecutableOffsets { get; set; } = new List<ExecutableOffsetRule>();
    }
}
