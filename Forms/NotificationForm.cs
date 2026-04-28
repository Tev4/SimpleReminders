using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using SimpleReminders.Services;

namespace SimpleReminders.Forms
{
    public class NotificationForm : Form
    {
        private Models.Reminder _reminder;
        private Font? _currentFont;
        private string _message = string.Empty;
        private Color _fontColor;
        private bool _isHovered;
        public event EventHandler? Dismissed;
        private readonly SettingsService _settingsService;
        private System.Windows.Forms.Timer? _displayTimer;
        private System.Windows.Forms.Timer? _fadeTimer;
        
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private const int DWMWA_TRANSITIONS_FORCEDISABLED = 3;

        public NotificationForm(SettingsService settingsService)
        {
            _settingsService = settingsService;
            _reminder = new Models.Reminder(); // Temporary placeholder

            // Form Setup
            this.Icon = IconService.AppIcon;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;
            this.StartPosition = FormStartPosition.Manual;
            this.DoubleBuffered = true;
            this.TopMost = true;
            this.Cursor = Cursors.Hand;

            // Force WS_EX_LAYERED composition by setting opacity slightly below 1.0
            this.Opacity = 0.99;

            this.HandleCreated += (s, e) => 
            {
                int disableTransitions = 1;
                DwmSetWindowAttribute(this.Handle, DWMWA_TRANSITIONS_FORCEDISABLED, ref disableTransitions, sizeof(int));
            };

            this.MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        public void InitializeForm(Models.Reminder reminder, Size textSize, Font font)
        {
            _reminder = reminder;
            _message = reminder.Message;
            _fontColor = ColorTranslator.FromHtml(reminder.FontColor);
            _currentFont = font;
            this.BackColor = ColorTranslator.FromHtml(reminder.BackgroundColor);

            int preferredWidth = reminder.Width > 0 ? reminder.Width : 250;
            int preferredHeight = reminder.Height > 0 ? reminder.Height : 0;
            int height = preferredHeight > 0 ? preferredHeight : Math.Max(80, textSize.Height + 40);
            this.Size = new Size(preferredWidth, height);

            if (_reminder.AutoFade)
            {
                _displayTimer?.Stop();
                _displayTimer?.Dispose();
                _displayTimer = new System.Windows.Forms.Timer();
                _displayTimer.Interval = _reminder.DisplayDurationSeconds * 1000;
                _displayTimer.Tick += (s, e) => 
                {
                    _displayTimer.Stop();
                    StartFadeOut();
                };
                _displayTimer.Start();
            }
            
            this.Opacity = 0.99;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_currentFont == null) return;

            // Draw hover highlight
            if (_isHovered)
            {
                using (var brush = new SolidBrush(Color.FromArgb(30, Color.White)))
                {
                    e.Graphics.FillRectangle(brush, this.ClientRectangle);
                }
            }

            // Draw 1px black border
            using (var pen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            }
            
            TextRenderer.DrawText(
                e.Graphics, 
                _message, 
                _currentFont, 
                this.ClientRectangle, 
                _fontColor, 
                TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter
            );
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            this.Hide();
            Dismissed?.Invoke(this, EventArgs.Empty);
        }

        private void StartFadeOut()
        {
            _fadeTimer = new System.Windows.Forms.Timer();
            _fadeTimer.Interval = 15; // 300ms / (1.0 / 0.05) = 15ms
            _fadeTimer.Tick += (s, e) => 
            {
                this.Opacity -= 0.05;
                if (this.Opacity <= 0)
                {
                    _fadeTimer.Stop();
                    this.Hide();
                    Dismissed?.Invoke(this, EventArgs.Empty);
                }
            };
            _fadeTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _displayTimer?.Stop();
            _displayTimer?.Dispose();
            _fadeTimer?.Stop();
            _fadeTimer?.Dispose();
            base.OnFormClosing(e);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // WS_EX_TOPMOST (0x8)
                // WS_EX_TOOLWINDOW (0x80)
                // WS_EX_LAYERED (0x80000) - For hardware acceleration/DWM optimization
                // WS_EX_NOACTIVATE (0x08000000) - Prevent stealing focus
                // WS_EX_COMPOSITED (0x02000000) - Double buffering for the whole window
                cp.ExStyle |= 0x0A080088;
                return cp;
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = (IntPtr)MA_NOACTIVATE;
                return;
            }

            base.WndProc(ref m);
        }
    }
}
