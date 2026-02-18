using System;

namespace SimpleReminders.Models
{
    public class AppSettings
    {
        public bool StartMinimized { get; set; } = true;
        public bool HasInitializedStartup { get; set; } = false;
    }
}
