using System.Drawing;
using System.Reflection;

namespace SimpleReminders.Services
{
    public static class IconService
    {
        private static Icon? _appIcon;

        public static Icon AppIcon
        {
            get
            {
                if (_appIcon == null)
                {
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        // The resource name usually includes the namespace. 
                        // In the .csproj, it's just SimpleReminders.ico, so it might be "SimpleReminders.SimpleReminders.ico" 
                        // depending on the project structure.
                        using (var stream = assembly.GetManifestResourceStream("SimpleReminders.SimpleReminders.ico"))
                        {
                            if (stream != null)
                            {
                                _appIcon = new Icon(stream);
                            }
                            else
                            {
                                // Fallback to system icon if resource not found
                                _appIcon = SystemIcons.Application;
                            }
                        }
                    }
                    catch
                    {
                        _appIcon = SystemIcons.Application;
                    }
                }
                return _appIcon;
            }
        }
        public static Image GetToggleIcon(bool enabled)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if (enabled)
                {
                    // Power icon look (red for disable)
                    using (var pen = new Pen(Color.Firebrick, 2))
                    {
                        g.DrawArc(pen, 3, 3, 10, 10, -60, 300);
                        g.DrawLine(pen, 8, 2, 8, 8);
                    }
                }
                else
                {
                    // Play icon look (green for enable)
                    using (var brush = new SolidBrush(Color.ForestGreen))
                    {
                        Point[] pts = { new Point(4, 3), new Point(13, 8), new Point(4, 13) };
                        g.FillPolygon(brush, pts);
                    }
                }
            }
            return bmp;
        }

        public static Image GetDuplicateIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Back square
                using (var pen = new Pen(Color.Gray, 1.5f))
                {
                    g.DrawRectangle(pen, 2, 2, 8, 8);
                }
                // Front square
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, 5, 5, 8, 8);
                }
                using (var pen = new Pen(Color.Black, 1.5f))
                {
                    g.DrawRectangle(pen, 5, 5, 8, 8);
                }
            }
            return bmp;
        }

        public static Image GetTriggerIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.Orange))
                {
                    // Bell body
                    g.FillEllipse(brush, 4, 3, 8, 8);
                    g.FillRectangle(brush, 3, 8, 10, 4);
                }
                // Bell clapper
                using (var brush = new SolidBrush(Color.DarkOrange))
                {
                    g.FillEllipse(brush, 7, 12, 2, 2);
                }
            }
            return bmp;
        }

        public static Image GetEditIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                // Pencil body
                using (var pen = new Pen(Color.DarkGoldenrod, 2))
                {
                    g.DrawLine(pen, 13, 3, 4, 12);
                }
                // Pencil tip
                using (var brush = new SolidBrush(Color.Black))
                {
                    Point[] pts = { new Point(2, 14), new Point(4, 11), new Point(5, 13) };
                    g.FillPolygon(brush, pts);
                }
            }
            return bmp;
        }

        public static Image GetDeleteIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.Red, 2.5f))
                {
                    g.DrawLine(pen, 3, 3, 13, 13);
                    g.DrawLine(pen, 13, 3, 3, 13);
                }
            }
            return bmp;
        }

        public static Image GetAddIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.ForestGreen, 3))
                {
                    g.DrawLine(pen, 8, 3, 8, 13);
                    g.DrawLine(pen, 3, 8, 13, 8);
                }
            }
            return bmp;
        }
    }
}
