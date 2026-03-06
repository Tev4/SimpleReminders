using System;

namespace SimpleReminders.Models
{
    public class ExecutableOffsetRule
    {
        public string ExecutablePath { get; set; } = string.Empty;
        public int XOffset { get; set; }
        public int YOffset { get; set; }

        public string ExecutableName => System.IO.Path.GetFileName(ExecutablePath);
    }
}
