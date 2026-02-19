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
    }
}
