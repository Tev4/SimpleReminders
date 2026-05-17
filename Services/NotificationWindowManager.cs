using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SimpleReminders.Forms;
using SimpleReminders.Models;

namespace SimpleReminders.Services
{
    public class NotificationWindowManager
    {
        private readonly List<NotificationForm> _openNotifications = new List<NotificationForm>();
        private readonly Queue<NotificationForm> _formPool = new Queue<NotificationForm>();
        private readonly int _spacing = 10;
        private int BottomOffset => WindowTrackingService.IsTaskbarAutoHideEnabled() ? 50 : 20;
        private readonly int _rightOffset = 10;
        private readonly SettingsService _settingsService;
        private readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();
        
        // Window Hook for focus tracking
        private readonly WinEventDelegate _winEventDelegate;
        private readonly IntPtr _winEventHook;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
        private const uint WINEVENT_OUTOFCONTEXT = 0;

        private int _currentOffsetX;
        private int _currentOffsetY;
        private int _currentWidth;
        private int _currentHeight;
        private NotificationAnchor _currentAnchor;

        public NotificationWindowManager(SettingsService settingsService)
        {
            _settingsService = settingsService;
            
            // Optimization: Pre-warm the pool and force OS/driver initialization
            // We show the form off-screen to force the entire DWM/rendering pipeline to initialize
            var warmForm = new NotificationForm(_settingsService);
            warmForm.Dismissed += (s, e) => CloseNotification(warmForm);
            
            // Move far off-screen and show/hide to trigger resource allocation
            warmForm.StartPosition = FormStartPosition.Manual;
            warmForm.Location = new Point(-20000, -20000);
            warmForm.Opacity = 0.01;
            warmForm.Show();
            warmForm.Hide();

            _formPool.Enqueue(warmForm);

            _winEventDelegate = new WinEventDelegate(WinEventProc);
            _winEventHook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _winEventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);

            // Initial offset calculation
            UpdateActiveOffsets();

            // Pre-cache the default font to avoid GDI initialization on first fire
            string defaultFont = _settingsService.Settings.DefaultFontFamily;
            if (string.IsNullOrEmpty(defaultFont)) defaultFont = "Segoe UI Variable Display";
            try
            {
                _fontCache[$"{defaultFont}_14"] = new Font(defaultFont, 14, FontStyle.Bold);
            }
            catch { /* Ignore font errors at startup */ }
        }

        public async void ShowNotification(Reminder reminder)
        {
            // Optimization: Measure text on a background thread to keep the UI thread clear
            string fontFamily = !string.IsNullOrEmpty(reminder.FontFamily) 
                ? reminder.FontFamily 
                : _settingsService.Settings.DefaultFontFamily;
            
            if (string.IsNullOrEmpty(fontFamily))
                fontFamily = "Segoe UI Variable Display";

            int width = reminder.Width > 0 ? reminder.Width : _currentWidth;
            float fontSize = reminder.FontSize;

            Font? capturedFont = null;
            Size textSize = await Task.Run(() => 
            {
                // Optimization: Use a cached font for measurement
                string fontKey = $"{fontFamily}_{fontSize}";
                Font? font;
                lock (_fontCache)
                {
                    if (!_fontCache.TryGetValue(fontKey, out font))
                    {
                        font = new Font(fontFamily, fontSize, FontStyle.Bold);
                        _fontCache[fontKey] = font;
                    }
                }
                capturedFont = font;

                return TextRenderer.MeasureText(
                    reminder.Message, 
                    font, 
                    new Size(width - 20, 0), 
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl
                );
            });

            NotificationForm form;
            lock (_formPool)
            {
                while (_formPool.Count > 0)
                {
                    var pooledForm = _formPool.Dequeue();
                    if (!pooledForm.IsDisposed)
                    {
                        form = pooledForm;
                        goto FormReady;
                    }
                }
                
                form = new NotificationForm(_settingsService);
                form.Dismissed += (s, e) => CloseNotification(form);
            }

            FormReady:
            form.InitializeForm(reminder, textSize, capturedFont!);
            
            _openNotifications.Add(form);
            await RepositionNotificationsAsync();
            
            form.Show();
        }

        private async void CloseNotification(NotificationForm form)
        {
            if (_openNotifications.Contains(form))
            {
                _openNotifications.Remove(form);
                
                // Optimization: Instead of disposing, hide and return to pool
                form.Hide();
                lock (_formPool)
                {
                    _formPool.Enqueue(form);
                }

                await RepositionNotificationsAsync();
            }
        }

        private async Task RepositionNotificationsAsync()
        {
            var screen = Screen.PrimaryScreen;
            if (screen == null) return;

            var workingArea = screen.WorkingArea;
            var anchor = _currentAnchor;

            int currentY;
            bool stackUpwards = (anchor == NotificationAnchor.BottomRight || anchor == NotificationAnchor.BottomLeft);
            
            if (stackUpwards)
                currentY = workingArea.Bottom - BottomOffset - _currentOffsetY;
            else
                currentY = workingArea.Top + BottomOffset + _currentOffsetY;

            // Loop in reverse order to keep newest notifications closest to the anchor point
            for (int i = _openNotifications.Count - 1; i >= 0; i--)
            {
                var form = _openNotifications[i];
                if (form.IsDisposed) continue;

                int x;
                int y;

                // X calculation
                if (anchor == NotificationAnchor.BottomRight || anchor == NotificationAnchor.TopRight)
                    x = workingArea.Right - form.Width - _rightOffset + _currentOffsetX;
                else // Left anchors
                    x = workingArea.Left + _rightOffset + _currentOffsetX;

                // Y calculation and stacking
                if (stackUpwards)
                {
                    y = currentY - form.Height;
                    currentY = y - _spacing;
                }
                else // Stack downwards
                {
                    y = currentY;
                    currentY = y + form.Height + _spacing;
                }
                
                form.Location = new Point(x, y);
            }
        }

        private void UpdateActiveOffsets()
        {
            // This is synchronous but called from WinEventProc or constructor
            string? activePath = WindowTrackingService.GetActiveExecutablePath();

            int offsetX = _settingsService.Settings.DefaultOffsetX;
            int offsetY = _settingsService.Settings.DefaultOffsetY;

            if (!string.IsNullOrEmpty(activePath))
            {
                var rule = _settingsService.Settings.ExecutableOffsets
                    .FirstOrDefault(r => 
                        string.Equals(r.ExecutablePath, activePath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(System.IO.Path.GetFileName(r.ExecutablePath), System.IO.Path.GetFileName(activePath), StringComparison.OrdinalIgnoreCase));
                
                if (rule != null)
                {
                    offsetX += rule.XOffset;
                    offsetY += rule.YOffset;
                }

                _currentWidth = (rule != null && rule.Width > 0) ? rule.Width : _settingsService.Settings.DefaultWidth;
                _currentHeight = (rule != null && rule.Height > 0) ? rule.Height : _settingsService.Settings.DefaultHeight;
                _currentAnchor = (rule != null) ? rule.Anchor : _settingsService.Settings.DefaultAnchor;
            }
            else
            {
                _currentWidth = _settingsService.Settings.DefaultWidth;
                _currentHeight = _settingsService.Settings.DefaultHeight;
                _currentAnchor = _settingsService.Settings.DefaultAnchor;
            }

            _currentOffsetX = offsetX;
            _currentOffsetY = offsetY;
        }

        private async void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            if (eventType == EVENT_SYSTEM_FOREGROUND)
            {
                // Delay slightly to allow the OS to finish the window switch and update its process info
                await Task.Delay(150);
                
                // Update cached offsets once per focus change
                UpdateActiveOffsets();
                
                // Reposition all open notifications to the new window's offsets
                await RepositionNotificationsAsync();
            }
        }
    }
}
