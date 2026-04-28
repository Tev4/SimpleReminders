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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        public static string? GetActiveExecutablePath()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                GetWindowThreadProcessId(hwnd, out uint processId);
                if (processId == 0) return null;

                IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                if (hProcess == IntPtr.Zero) return null;

                try
                {
                    uint bufferSize = 1024;
                    StringBuilder buffer = new StringBuilder((int)bufferSize);
                    if (QueryFullProcessImageName(hProcess, 0, buffer, ref bufferSize))
                    {
                        return buffer.ToString();
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
