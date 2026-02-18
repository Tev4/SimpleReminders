using System;
using System.IO;
using System.Text.Json;
using SimpleReminders.Models;

namespace SimpleReminders.Services
{
    public class SettingsService
    {
        private readonly string _filePath;
        private AppSettings _settings;

        public SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDir = Path.Combine(appData, "SimpleReminders");
            if (!Directory.Exists(appDir)) Directory.CreateDirectory(appDir);
            
            _filePath = Path.Combine(appDir, "settings.json");
            _settings = LoadSettings();
        }

        public AppSettings Settings => _settings;

        private AppSettings LoadSettings()
        {
            if (!File.Exists(_filePath)) return new AppSettings();
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch 
            {
                return new AppSettings();
            }
        }

        public void SaveSettings()
        {
            try
            {
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Silently fail
            }
        }
    }
}
