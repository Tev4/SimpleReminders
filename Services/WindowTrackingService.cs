using System;

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

        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        private const uint ABM_GETSTATE = 0x4;
        private const int ABS_AUTOHIDE = 0x1;

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

        public static bool IsTaskbarAutoHideEnabled()
        {
            try
            {
                APPBARDATA data = new APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA));
                IntPtr state = SHAppBarMessage(ABM_GETSTATE, ref data);
                return (state.ToInt32() & ABS_AUTOHIDE) == ABS_AUTOHIDE;
            }
            catch
            {
                return false;
            }
        }
    }
}
