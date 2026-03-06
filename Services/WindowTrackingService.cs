using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleReminders.Services
{
    public static class WindowTrackingService
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static string? GetActiveExecutablePath()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId == 0) return null;

                using (var process = Process.GetProcessById((int)processId))
                {
                    return process.MainModule?.FileName;
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
