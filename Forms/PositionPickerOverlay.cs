using System;
using System.Drawing;
using System.Windows.Forms;
using SimpleReminders.Models;

namespace SimpleReminders.Forms
{
    public class PositionPickerOverlay : Form
    {
        private Panel _placeholder;
        private Point _dragStart;
        private bool _isDragging;
        private bool _isResizing;
        private AnchorStyles _activeResizeAnchor;
        private Point _resizeStart;
        private Size _startSize;
        private Point _startLocation;

        private int _resultX;
        private int _resultY;
        private int _resultWidth;
        private int _resultHeight;
        private NotificationAnchor _resultAnchor;

        public int ResultX => _resultX;
        public int ResultY => _resultY;
        public int ResultWidth => _resultWidth;
        public int ResultHeight => _resultHeight;
        public NotificationAnchor ResultAnchor => _resultAnchor;

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        public PositionPickerOverlay(int currentOffsetX, int currentOffsetY, int currentWidth, int currentHeight, NotificationAnchor currentAnchor, AppSettings settings)
        {
            _resultWidth = currentWidth;
            _resultHeight = currentHeight;
            _resultAnchor = currentAnchor;
            
            this.DoubleBuffered = true;
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.Bounds = SystemInformation.VirtualScreen;
            this.TopMost = true;
            this.BackColor = Color.Black;
            this.Opacity = 0.6;
            this.ShowInTaskbar = false;
            this.Cursor = Cursors.Cross;
            this.KeyPreview = true;

            _placeholder = new Panel
            {
                Size = new Size(_resultWidth, _resultHeight),
                BackColor = ColorTranslator.FromHtml(settings.DefaultBackgroundColor),
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.SizeAll
            };

            var label = new Label
            {
                Text = "DRAG TO MOVE\nRESIZE CORNERS\nRight Click Corner to Anchor\nDouble Click to Save",
                ForeColor = ColorTranslator.FromHtml(settings.DefaultFontColor),
                Font = new Font(settings.DefaultFontFamily, 9, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                AutoSize = false
            };

            // Hook up dragging events
            label.MouseDown += Placeholder_MouseDown;
            label.MouseMove += Placeholder_MouseMove;
            label.MouseUp += Placeholder_MouseUp;
            label.DoubleClick += (s, e) => {
                if (e is MouseEventArgs me && me.Button == MouseButtons.Left)
                {
                    CalculateResults();
                    this.DialogResult = DialogResult.OK;
                }
            };

            // Resize handles
            CreateHandle(AnchorStyles.Top | AnchorStyles.Left, Cursors.SizeNWSE, NotificationAnchor.TopLeft);
            CreateHandle(AnchorStyles.Top | AnchorStyles.Right, Cursors.SizeNESW, NotificationAnchor.TopRight);
            CreateHandle(AnchorStyles.Bottom | AnchorStyles.Left, Cursors.SizeNESW, NotificationAnchor.BottomLeft);
            CreateHandle(AnchorStyles.Bottom | AnchorStyles.Right, Cursors.SizeNWSE, NotificationAnchor.BottomRight);

            _placeholder.Controls.Add(label);
            this.Controls.Add(_placeholder);

            // Calculate initial position based on current offsets and anchor
            var screen = Screen.PrimaryScreen;
            var workingArea = screen != null ? screen.WorkingArea : SystemInformation.WorkingArea;
            
            int paddingX = 20;
            int paddingY = 50;
            
            int startX = 0;
            int startY = 0;

            switch (currentAnchor)
            {
                case NotificationAnchor.TopLeft:
                    startX = workingArea.Left + paddingX + currentOffsetX;
                    startY = workingArea.Top + paddingY + currentOffsetY;
                    break;
                case NotificationAnchor.TopRight:
                    startX = workingArea.Right - _resultWidth - paddingX + currentOffsetX;
                    startY = workingArea.Top + paddingY + currentOffsetY;
                    break;
                case NotificationAnchor.BottomLeft:
                    startX = workingArea.Left + paddingX + currentOffsetX;
                    startY = workingArea.Bottom - paddingY - currentOffsetY - _resultHeight;
                    break;
                case NotificationAnchor.BottomRight:
                    startX = workingArea.Right - _resultWidth - paddingX + currentOffsetX;
                    startY = workingArea.Bottom - paddingY - currentOffsetY - _resultHeight;
                    break;
            }
            
            _placeholder.Location = this.PointToClient(new Point(startX, startY));

            this.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Escape) this.DialogResult = DialogResult.Cancel;
                if (e.KeyCode == Keys.Enter) { CalculateResults(); this.DialogResult = DialogResult.OK; }
            };
        }

        private void CreateHandle(AnchorStyles anchor, Cursor cursor, NotificationAnchor anchorType)
        {
            var handle = new Panel
            {
                Size = new Size(12, 12),
                BackColor = (_resultAnchor == anchorType) ? Color.Lime : Color.White,
                Cursor = cursor,
                Anchor = anchor,
                Tag = anchorType
            };

            int x = (anchor.HasFlag(AnchorStyles.Right)) ? _placeholder.Width - 12 : 0;
            int y = (anchor.HasFlag(AnchorStyles.Bottom)) ? _placeholder.Height - 12 : 0;
            handle.Location = new Point(x, y);

            handle.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    _isResizing = true;
                    _activeResizeAnchor = anchor;
                    _resizeStart = Cursor.Position;
                    _startSize = _placeholder.Size;
                    _startLocation = _placeholder.Location;
                }
                else if (e.Button == MouseButtons.Right) {
                    _resultAnchor = anchorType;
                    UpdateHandleColors();
                }
            };
            handle.MouseMove += Handle_MouseMove;
            handle.MouseUp += (s, e) => _isResizing = false;

            _placeholder.Controls.Add(handle);
            handle.BringToFront();
        }

        private void UpdateHandleColors()
        {
            foreach (Control ctrl in _placeholder.Controls)
            {
                if (ctrl is Panel handle && handle.Tag is NotificationAnchor type)
                {
                    handle.BackColor = (type == _resultAnchor) ? Color.Lime : Color.White;
                }
            }
        }

        private void Handle_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isResizing)
            {
                Point currentMouse = Cursor.Position;
                int diffX = currentMouse.X - _resizeStart.X;
                int diffY = currentMouse.Y - _resizeStart.Y;

                int newWidth = _startSize.Width;
                int newHeight = _startSize.Height;
                int newX = _placeholder.Left;
                int newY = _placeholder.Top;

                if (_activeResizeAnchor.HasFlag(AnchorStyles.Right))
                    newWidth = Math.Clamp(_startSize.Width + diffX, 100, 4000);
                else if (_activeResizeAnchor.HasFlag(AnchorStyles.Left))
                {
                    newWidth = Math.Clamp(_startSize.Width - diffX, 100, 4000);
                    newX = _startLocation.X + (_startSize.Width - newWidth);
                }

                if (_activeResizeAnchor.HasFlag(AnchorStyles.Bottom))
                    newHeight = Math.Clamp(_startSize.Height + diffY, 40, 4000);
                else if (_activeResizeAnchor.HasFlag(AnchorStyles.Top))
                {
                    newHeight = Math.Clamp(_startSize.Height - diffY, 40, 4000);
                    newY = _startLocation.Y + (_startSize.Height - newHeight);
                }

                _placeholder.Bounds = new Rectangle(newX, newY, newWidth, newHeight);
                _placeholder.Refresh();
            }
        }

        private void Placeholder_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isDragging = true;
                _dragStart = e.Location;
            }
        }

        private void Placeholder_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _placeholder.Left += e.X - _dragStart.X;
                _placeholder.Top += e.Y - _dragStart.Y;
            }
        }

        private void Placeholder_MouseUp(object? sender, MouseEventArgs e) => _isDragging = false;

        private void CalculateResults()
        {
            var screen = Screen.PrimaryScreen;
            var workingArea = screen != null ? screen.WorkingArea : SystemInformation.WorkingArea;
            int paddingX = 20;
            int paddingY = 50;

            _resultWidth = _placeholder.Width;
            _resultHeight = _placeholder.Height;
            Point screenPos = this.PointToScreen(_placeholder.Location);

            switch (_resultAnchor)
            {
                case NotificationAnchor.TopLeft:
                    _resultX = screenPos.X - (workingArea.Left + paddingX);
                    _resultY = screenPos.Y - (workingArea.Top + paddingY);
                    break;
                case NotificationAnchor.TopRight:
                    _resultX = screenPos.X - (workingArea.Right - _resultWidth - paddingX);
                    _resultY = screenPos.Y - (workingArea.Top + paddingY);
                    break;
                case NotificationAnchor.BottomLeft:
                    _resultX = screenPos.X - (workingArea.Left + paddingX);
                    _resultY = workingArea.Bottom - paddingY - _resultHeight - screenPos.Y;
                    break;
                case NotificationAnchor.BottomRight:
                    _resultX = screenPos.X - (workingArea.Right - _resultWidth - paddingX);
                    _resultY = workingArea.Bottom - paddingY - _resultHeight - screenPos.Y;
                    break;
            }
        }
    }
}
