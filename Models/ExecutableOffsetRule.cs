using System;

namespace SimpleReminders.Models
{
    public class ExecutableOffsetRule
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public int XOffset { get; set; }
        public int YOffset { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public NotificationAnchor Anchor { get; set; } = NotificationAnchor.BottomRight;

        public string ExecutableName => System.IO.Path.GetFileName(ExecutablePath);
    }
}
