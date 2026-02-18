using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SimpleReminders.Forms;
using SimpleReminders.Models;

namespace SimpleReminders.Services
{
    public class NotificationWindowManager
    {
        private readonly List<NotificationForm> _openNotifications = new List<NotificationForm>();
        private readonly int _spacing = 10;
        private readonly int _bottomOffset = 50;
        private readonly int _rightOffset = 20;

        public void ShowNotification(Reminder reminder)
        {
            var form = new NotificationForm(reminder);
            form.Dismissed += (s, e) => CloseNotification(form);
            
            _openNotifications.Add(form);
            RepositionNotifications();
            
            form.Show();
        }

        private void CloseNotification(NotificationForm form)
        {
            if (_openNotifications.Contains(form))
            {
                _openNotifications.Remove(form);
                // Dispose is called by the form itself on close usually, but good to be sure
                if (!form.IsDisposed) form.Dispose();
                RepositionNotifications();
            }
        }

        private void RepositionNotifications()
        {
            var screen = Screen.PrimaryScreen;
            if (screen == null) return; // handle null screen

            var workingArea = screen.WorkingArea;

            int currentY = workingArea.Bottom - _bottomOffset;

            for (int i = _openNotifications.Count - 1; i >= 0; i--)
            {
                var form = _openNotifications[i];
                if (form.IsDisposed) continue;

                int x = workingArea.Right - form.Width - _rightOffset;
                int y = currentY - form.Height;
                
                form.Location = new Point(x, y);
                
                currentY = y - _spacing;
            }
        }
    }
}
