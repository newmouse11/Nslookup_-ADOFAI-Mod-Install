using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OneClick
{
    // 표: 항목 / 버전 / 파일(숫자는 우측 정렬) / 상태. 행 구분선·줄무늬 없음, 호버만 bg-elevated.
    internal sealed class ItemTable : Control
    {
        private const int HeaderH = 32, RowH = 28, Pad = 12;
        private int _hover = -1;
        public readonly List<string[]> Rows = new List<string[]>(); // name, version, files, status

        public ItemTable()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.BgSurface;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int row = e.Y < HeaderH ? -1 : (e.Y - HeaderH) / RowH;
            if (row >= Rows.Count) row = -1;
            if (row != _hover) { _hover = row; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.BgSurface);
            int w = Width;
            int xVer = (int)(w * 0.40), xFilesRight = (int)(w * 0.70), xStatus = (int)(w * 0.74);
            using (var small = Theme.Px(12))
            using (var semi = Theme.Px(12, true))
            using (var seam = new Pen(Theme.BgElevated))
            {
                var tf = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
                Action<string, int, int, int, int, Font, Color, bool> text = (s, x, y, cw, h, f, c, right) =>
                    TextRenderer.DrawText(g, s, f, new Rectangle(x, y, cw, h), c, tf | (right ? TextFormatFlags.Right : TextFormatFlags.Left));

                text("항목", Pad, 0, xVer - Pad, HeaderH, small, Theme.TextTertiary, false);
                text("버전", xVer, 0, xFilesRight - xVer - 60, HeaderH, small, Theme.TextTertiary, false);
                text("파일", xFilesRight - 48, 0, 48, HeaderH, small, Theme.TextTertiary, true);
                text("상태", xStatus, 0, w - xStatus - Pad, HeaderH, small, Theme.TextTertiary, false);
                g.DrawLine(seam, 0, HeaderH - 1, w, HeaderH - 1);

                for (int i = 0; i < Rows.Count; i++)
                {
                    int y = HeaderH + i * RowH;
                    if (i == _hover) using (var b = new SolidBrush(Theme.BgElevated)) g.FillRectangle(b, 0, y, w, RowH);
                    var r = Rows[i];
                    text(r[0], Pad, y, xVer - Pad, RowH, semi, Theme.TextPrimary, false);
                    text(r[1], xVer, y, xFilesRight - xVer - 60, RowH, small, Theme.TextSecondary, false);
                    text(r[2], xFilesRight - 48, y, 48, RowH, small, Theme.TextPrimary, true);
                    text(r[3], xStatus, y, w - xStatus - Pad, RowH, small, Theme.TextSecondary, false);
                }
            }
        }
    }

    // 하단 2px 진행선: 트랙 bg-elevated, 채움 text-secondary (노란색은 CTA에만 쓴다)
    internal sealed class ThinProgress : Control
    {
        private double _value;
        public double Value { get { return _value; } set { _value = value; Invalidate(); } }
        public ThinProgress()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgElevated);
            using (var b = new SolidBrush(Theme.TextSecondary))
                e.Graphics.FillRectangle(b, 0, 0, (int)(Width * _value), Height);
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly Manifest _manifest = Manifest.LoadEmbedded();
        private readonly TextBox _path = new TextBox();
        private readonly FlatButton _browse = new FlatButton { Text = "찾기" };
        private readonly FlatButton _install = new FlatButton { Text = "설치", IsPrimary = true, FontPx = 14 };
        private readonly Label _status = new Label();
        private readonly ItemTable _table = new ItemTable();
        private readonly ThinProgress _progress = new ThinProgress();
        private bool _confirmMismatch;
        private bool _busy;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public InstallerForm(string initialPath)
        {
            Text = "얼불춤 모드 원클릭 설치기";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(560, 460);
            BackColor = Theme.BgBase;
            Font = Theme.Px(12);

            // 상단 패널 (56px)
            var header = Panel(0, 0, 560, 56);
            var title = Lbl("얼불춤 모드 원클릭 설치기", 16, 10, 528, 22, Theme.Px(16, true), Theme.TextPrimary);
            var totalFiles = _manifest.Files.Count;
            var sub = Lbl("모드 " + _manifest.Items.Count + "개 · 파일 " + totalFiles + "개 · 기준 " + _manifest.BuiltAt, 16, 33, 528, 16, Theme.Px(12), Theme.TextTertiary);
            header.Controls.AddRange(new Control[] { title, sub });

            // 게임 폴더 패널 (88px)
            var folder = Panel(0, 57, 560, 88);
            var folderLbl = Lbl("게임 폴더", 16, 12, 200, 16, Theme.Px(12), Theme.TextTertiary);
            var card = new Panel { Bounds = new Rectangle(16, 36, 436, 40), BackColor = Theme.BgCard };
            _path.BorderStyle = BorderStyle.None;
            _path.BackColor = Theme.BgCard;
            _path.ForeColor = Theme.TextPrimary;
            _path.Font = Theme.Px(14);
            _path.Bounds = new Rectangle(12, 11, 412, 20);
            _path.AccessibleName = "게임 폴더 경로";
            _path.TextChanged += delegate { _confirmMismatch = false; _install.Text = "설치"; ShowNeutral(DefaultStatus()); };
            card.Controls.Add(_path);
            _browse.Bounds = new Rectangle(460, 36, 84, 40);
            _browse.AccessibleName = "게임 폴더 찾기";
            _browse.Click += Browse;
            folder.Controls.AddRange(new Control[] { folderLbl, card, _browse });

            // 표 패널
            var tablePanel = Panel(0, 146, 560, 232);
            _table.Bounds = new Rectangle(0, 0, 560, 232);
            foreach (var it in _manifest.Items) _table.Rows.Add(new[] { it.Name, it.Version, it.FileCount.ToString(), "대기" });
            tablePanel.Controls.Add(_table);

            // 하단 패널 (진행선 2px + 상태 + 설치 버튼)
            var footer = Panel(0, 379, 560, 81);
            _progress.Bounds = new Rectangle(0, 0, 560, 2);
            _status.Bounds = new Rectangle(16, 14, 388, 56);
            _status.Font = Theme.Px(12);
            _status.ForeColor = Theme.TextSecondary;
            _status.BackColor = Theme.BgSurface;
            _install.Bounds = new Rectangle(416, 16, 128, 48);
            _install.AccessibleName = "설치";
            _install.Click += async delegate { await RunInstall(); };
            footer.Controls.AddRange(new Control[] { _progress, _status, _install });

            Controls.AddRange(new Control[] { header, folder, tablePanel, footer });
            _path.Text = initialPath ?? GameLocator.Find() ?? "";
            if (_path.Text.Length == 0) ShowWarning("게임 폴더를 찾지 못했습니다. '찾기'로 직접 선택하세요.");
            ActiveControl = _install;
        }

        private static Panel Panel(int x, int y, int w, int h)
        {
            return new Panel { Bounds = new Rectangle(x, y, w, h), BackColor = Theme.BgSurface };
        }

        private static Label Lbl(string text, int x, int y, int w, int h, Font font, Color color)
        {
            return new Label { Text = text, Bounds = new Rectangle(x, y, w, h), Font = font, ForeColor = color, BackColor = Theme.BgSurface, AutoEllipsis = true };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // 제목 표시줄도 어둡게 (Win10 20H1+는 20, 이전 빌드는 19)
            int on = 1;
            try { if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4); }
            catch (Exception) { /* 미지원 OS는 기본 제목 표시줄 */ }
        }

        private string DefaultStatus()
        {
            return "설치하면 기존 파일은 백업 폴더에 보관한 뒤 덮어씁니다. 개인 통계는 포함하지 않습니다.";
        }

        private void ShowNeutral(string s) { _status.ForeColor = Theme.TextSecondary; _status.Text = s; }
        private void ShowWarning(string s) { _status.ForeColor = Theme.Warning; _status.Text = s; }

        private void Browse(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog { Description = "A Dance of Fire and Ice 폴더를 선택하세요", ShowNewFolderButton = false })
                if (dlg.ShowDialog(this) == DialogResult.OK) _path.Text = dlg.SelectedPath;
        }

        private async Task RunInstall()
        {
            if (_busy) return;
            var dir = _path.Text.Trim();
            if (!GameLocator.IsGameDir(dir)) { ShowWarning("게임 폴더가 아닙니다. 'A Dance of Fire and Ice.exe'가 있는 폴더를 선택하세요."); return; }
            if (GameLocator.IsGameRunning()) { ShowWarning("게임이 실행 중입니다. 게임을 끈 뒤 다시 누르세요."); return; }
            if (!_confirmMismatch && !GameLocator.MatchesBuild(dir, _manifest))
            {
                _confirmMismatch = true;
                _install.Text = "그래도 설치";
                ShowWarning("이 설치기는 다른 게임 빌드 기준으로 만들었습니다. 모드가 동작하지 않을 수 있습니다. 계속하려면 한 번 더 누르세요.");
                return;
            }

            _busy = true;
            _install.Enabled = false; _browse.Enabled = false; _path.Enabled = false;
            _install.Text = "설치 중";
            ShowNeutral("파일을 설치하는 중입니다.");
            try
            {
                var result = await Task.Run(() => Installer.Install(dir, _manifest, p => BeginInvoke((Action)(() => _progress.Value = p))));
                for (int i = 0; i < _manifest.Items.Count; i++)
                {
                    var c = result.PerItem[_manifest.Items[i].Name];
                    _table.Rows[i][3] = "변경 " + c[0] + " · 동일 " + c[1];
                }
                _table.Invalidate();
                ShowNeutral("설치 완료 · 변경 " + result.Written + " · 동일 " + result.Unchanged
                    + (result.BackedUp > 0 ? " · 백업 " + result.BackedUp + "개 (ADOFAI-OneClick-Backup 폴더)" : ""));
                _install.Text = "완료";
            }
            catch (Exception ex)
            {
                ShowWarning("설치 실패: " + ex.Message);
                _install.Text = "설치";
                _install.Enabled = true; _browse.Enabled = true; _path.Enabled = true;
                _progress.Value = 0;
            }
            finally { _busy = false; }
        }

        // 개발용: 폼을 PNG로 저장 (/shot:경로)
        public void SaveShot(string pngPath)
        {
            Opacity = 0; Show(); Application.DoEvents();
            using (var bmp = new Bitmap(Width, Height))
            {
                DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            Close();
        }
    }
}
