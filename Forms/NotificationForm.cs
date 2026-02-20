using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class NotificationForm : Form
    {
        private readonly Models.Reminder _reminder;
        private readonly Button _dismissButton;
        private readonly int _cornerRadius = 24;

        public event EventHandler? Dismissed;

        public NotificationForm(Models.Reminder reminder)
        {
            _reminder = reminder;

            // Form Setup
            this.Icon = IconService.AppIcon;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = ColorTranslator.FromHtml(reminder.BackgroundColor);

            // Label Setup for emoji support
            _dismissButton = new Button();
            _dismissButton.Text = reminder.Message;
            _dismissButton.Dock = DockStyle.Fill;
            _dismissButton.FlatStyle = FlatStyle.Flat;
            _dismissButton.FlatAppearance.BorderSize = 0;
            _dismissButton.ForeColor = ColorTranslator.FromHtml(reminder.FontColor);
            _dismissButton.Font = new Font("Segoe UI Variable Display", reminder.FontSize, FontStyle.Bold);
            _dismissButton.Cursor = Cursors.Hand;
            _dismissButton.TabStop = false;
            _dismissButton.UseCompatibleTextRendering = false; // Use GDI+ for emoji rendering
            
            // Calculate Size
            int preferredWidth = reminder.Width > 0 ? reminder.Width : 250;
            int preferredHeight = reminder.Height > 0 ? reminder.Height : 0; // 0 means auto-calculate if not set

            Size fixedSize = new Size(preferredWidth, 0);
            Size textSize = TextRenderer.MeasureText(
                reminder.Message, 
                _dismissButton.Font, 
                fixedSize, 
                TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter
            );

            // Minimum height 80 or user defined, add padding
            int height = preferredHeight > 0 ? preferredHeight : Math.Max(80, textSize.Height + 40);
            this.Size = new Size(preferredWidth, height);
            
            _dismissButton.Click += (s, e) => {
                this.Close();
                Dismissed?.Invoke(this, EventArgs.Empty);
            };

            this.Controls.Add(_dismissButton);

            // Shape
            this.Load += (s, e) => SetRoundedRegion();
            this.Resize += (s, e) => SetRoundedRegion();
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_TOPMOST (0x8)
                // WS_EX_TOOLWINDOW (0x80)
                // WS_EX_NOACTIVATE (0x08000000)
                cp.ExStyle |= 0x08000088;
                return cp;
            }
        }

        private void SetRoundedRegion()
        {
            if (this.Width == 0 || this.Height == 0) return;
            
            using (GraphicsPath path = new GraphicsPath())
            {
                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                int size = _cornerRadius;
                
                path.AddArc(rect.X, rect.Y, size, size, 180, 90);
                path.AddArc(rect.Right - size, rect.Y, size, size, 270, 90);
                path.AddArc(rect.Right - size, rect.Bottom - size, size, size, 0, 90);
                path.AddArc(rect.X, rect.Bottom - size, size, size, 90, 90);
                path.CloseAllFigures();
                
                this.Region = new Region(path);
            }
        }
    }
}
