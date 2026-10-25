using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OneClick
{
    // BINANCE-STYLE.md 토큰을 그대로 옮긴 색/글꼴. 이 파일 밖에서는 원시 색상값을 쓰지 않는다.
    internal static class Theme
    {
        public static readonly Color BgBase = Color.FromArgb(0x0B, 0x0E, 0x11);
        public static readonly Color BgSurface = Color.FromArgb(0x18, 0x1A, 0x20);
        public static readonly Color BgCard = Color.FromArgb(0x1E, 0x23, 0x29);
        public static readonly Color BgElevated = Color.FromArgb(0x2B, 0x31, 0x39);
        public static readonly Color BgElevatedHover = Color.FromArgb(0x36, 0x3B, 0x44);
        public static readonly Color BorderStrong = Color.FromArgb(0x36, 0x3B, 0x44);
        public static readonly Color TextPrimary = Color.FromArgb(0xEA, 0xEC, 0xEF);
        public static readonly Color TextSecondary = Color.FromArgb(0xB7, 0xBD, 0xC6);
        public static readonly Color TextTertiary = Color.FromArgb(0x84, 0x8E, 0x9C);
        public static readonly Color TextDisabled = Color.FromArgb(0x5E, 0x66, 0x73);
        public static readonly Color Primary = Color.FromArgb(0xFC, 0xD5, 0x35);
        public static readonly Color PrimaryHover = Color.FromArgb(0xF0, 0xB9, 0x0B);
        public static readonly Color PrimaryFg = Color.FromArgb(0x0B, 0x0E, 0x11);
        public static readonly Color PrimaryMuted = Color.FromArgb(0x3A, 0x3A, 0x1F);
        public static readonly Color Warning = Color.FromArgb(0xF0, 0xB9, 0x0B);

        // 12px = text-xs(표/라벨), 14px = text-sm(본문/버튼), 16px = text-base(패널 제목)
        public static Font Px(float px, bool semibold = false)
        {
            return new Font(semibold ? "Segoe UI Semibold" : "Segoe UI", px, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // 평면 버튼: 반경 4px, 호버 시 색만 바뀜(이동/그림자 없음), 키보드 포커스는 2px 노란 윤곽선.
    internal sealed class FlatButton : Control
    {
        private bool _hover, _down;
        public bool IsPrimary;
        public int FontPx = 14;

        public FlatButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Focus(); Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }
        protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Enter || keyData == Keys.Space || base.IsInputKey(keyData); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Color bg, fg;
            if (!Enabled) { bg = IsPrimary ? Theme.PrimaryMuted : Theme.BgCard; fg = Theme.TextDisabled; }
            else if (IsPrimary) { bg = (_hover || _down) ? Theme.PrimaryHover : Theme.Primary; fg = Theme.PrimaryFg; }
            else { bg = (_hover || _down) ? Theme.BgElevatedHover : Theme.BgElevated; fg = Theme.TextPrimary; }

            var g = e.Graphics;
            g.Clear(Parent != null ? Parent.BackColor : Theme.BgSurface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.RoundRect(rect, 4))
            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);
            if (Focused && Enabled)
            {
                using (var path = Theme.RoundRect(new Rectangle(1, 1, Width - 3, Height - 3), 4))
                using (var pen = new Pen(Theme.Primary, 2))
                    g.DrawPath(pen, path);
            }
            using (var font = Theme.Px(FontPx, true))
                TextRenderer.DrawText(g, Text, font, ClientRectangle, fg,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }
    }
}
