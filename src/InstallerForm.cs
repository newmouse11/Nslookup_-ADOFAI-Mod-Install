using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdofaiModInstaller
{
    internal sealed class Row
    {
        public Component C;
        public bool Checked, Locked;
        public string Status = "대기";
    }

    // 구성요소 표: 체크박스 / 이름 / 버전 / 라이선스 / 상태. 행 구분선·줄무늬 없음, 호버·커서만 bg-elevated.
    internal sealed class ComponentTable : Control
    {
        public const int HeaderH = 32, RowH = 28;
        public readonly List<Row> Rows = new List<Row>();
        public event Action<int> RowToggled;
        private int _hover = -1, _cursor;

        public ComponentTable()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw | ControlStyles.Selectable | ControlStyles.StandardClick, true);
            TabStop = true;
            BackColor = Theme.BgSurface;
        }

        public int PreferredHeight { get { return HeaderH + Rows.Count * RowH; } }

        private int RowAt(int y) { int r = (y - HeaderH) / RowH; return y < HeaderH || r >= Rows.Count ? -1 : r; }

        private void Toggle(int i)
        {
            if (i < 0 || Rows[i].Locked || Locked) return;
            Rows[i].Checked = !Rows[i].Checked;
            if (RowToggled != null) RowToggled(i);
            Invalidate();
        }

        public bool Locked; // 설치 중에는 선택 변경 금지

        protected override void OnMouseMove(MouseEventArgs e) { int r = RowAt(e.Y); if (r != _hover) { _hover = r; Invalidate(); } base.OnMouseMove(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { Focus(); int r = RowAt(e.Y); if (r >= 0) { _cursor = r; Toggle(r); } base.OnMouseDown(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override bool IsInputKey(Keys k) { return k == Keys.Up || k == Keys.Down || k == Keys.Space || base.IsInputKey(k); }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down && _cursor < Rows.Count - 1) { _cursor++; Invalidate(); }
            else if (e.KeyCode == Keys.Up && _cursor > 0) { _cursor--; Invalidate(); }
            else if (e.KeyCode == Keys.Space) Toggle(_cursor);
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Theme.BgSurface);
            int w = Width;
            const int xName = 36, xVer = 236, xLic = 330, xStatus = 430;
            var tf = TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis;
            using (var small = Theme.Px(12))
            using (var semi = Theme.Px(12, true))
            using (var seam = new Pen(Theme.BgElevated))
            {
                Action<string, int, int, int, Font, Color> text = (s, x, y, cw, f, c) =>
                    TextRenderer.DrawText(g, s, f, new Rectangle(x, y, cw, y < HeaderH ? HeaderH : RowH), c, tf);
                text("구성요소", xName, 0, xVer - xName - 8, small, Theme.TextTertiary);
                text("버전", xVer, 0, 90, small, Theme.TextTertiary);
                text("라이선스", xLic, 0, 96, small, Theme.TextTertiary);
                text("상태", xStatus, 0, w - xStatus - 12, small, Theme.TextTertiary);
                g.DrawLine(seam, 0, HeaderH - 1, w, HeaderH - 1);

                for (int i = 0; i < Rows.Count; i++)
                {
                    int y = HeaderH + i * RowH;
                    var row = Rows[i];
                    if (i == _hover) using (var b = new SolidBrush(Theme.BgElevated)) g.FillRectangle(b, 0, y, w, RowH);
                    if (Focused && i == _cursor) using (var p = new Pen(Theme.Primary, 2)) g.DrawRectangle(p, 1, y + 1, w - 3, RowH - 3);

                    var box = new Rectangle(12, y + 6, 16, 16);
                    using (var b = new SolidBrush(Theme.BgCard)) g.FillRectangle(b, box);
                    using (var p = new Pen(row.Locked ? Theme.BgElevated : Theme.BorderStrong)) g.DrawRectangle(p, box.X, box.Y, box.Width - 1, box.Height - 1);
                    if (row.Checked)
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var p = new Pen(row.Locked ? Theme.TextDisabled : Theme.TextPrimary, 2))
                            g.DrawLines(p, new[] { new Point(box.X + 3, box.Y + 8), new Point(box.X + 6, box.Y + 11), new Point(box.X + 12, box.Y + 4) });
                        g.SmoothingMode = SmoothingMode.Default;
                    }
                    var strong = row.Locked ? Theme.TextSecondary : Theme.TextPrimary;
                    text(row.C.name, xName, y, xVer - xName - 8, semi, strong);
                    text(row.C.version, xVer, y, 90, small, Theme.TextSecondary);
                    text(row.C.license ?? "", xLic, y, 96, small, Theme.TextSecondary);
                    text(row.Status, xStatus, y, w - xStatus - 12, small, Theme.TextSecondary);
                }
            }
        }
    }

    // 하단 2px 진행선: 트랙 bg-elevated, 채움 text-secondary (노란색은 CTA에만 쓴다)
    internal sealed class ThinProgress : Control
    {
        private double _value;
        public double Value { get { return _value; } set { _value = Math.Max(0, Math.Min(1, value)); Invalidate(); } }
        public ThinProgress() { SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.BgElevated);
            using (var b = new SolidBrush(Theme.TextSecondary)) e.Graphics.FillRectangle(b, 0, 0, (int)(Width * _value), Height);
        }
    }

    internal sealed class InstallerForm : Form
    {
        private readonly CatalogDoc _catalog;
        private readonly TextBox _path = new TextBox();
        private readonly FlatButton _browse = new FlatButton { Text = "찾기" };
        private readonly FlatButton _install = new FlatButton { Text = "설치", IsPrimary = true };
        private readonly FlatButton _uninstall = new FlatButton { Text = "제거" };
        private readonly Label _status = new Label();
        private readonly Label _sub = new Label();
        private readonly ComponentTable _table = new ComponentTable();
        private readonly ThinProgress _progress = new ThinProgress();
        private bool _busy, _confirmUninstall;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public InstallerForm(CatalogDoc catalog, string initialPath)
        {
            _catalog = catalog;
            Text = "얼불춤 모드 설치기";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgBase;
            Font = Theme.Px(12);

            foreach (var c in catalog.components)
                _table.Rows.Add(new Row { C = c, Checked = c.required || c.defaultOn, Locked = c.required });
            _table.RowToggled += OnRowToggled;
            int tableH = _table.PreferredHeight;
            ClientSize = new Size(560, 56 + 1 + 88 + 1 + tableH + 1 + 81);

            var header = Panel(0, 0, 560, 56);
            header.Controls.Add(Lbl("얼불춤 모드 설치기", 16, 10, 528, 22, Theme.Px(16, true), Theme.TextPrimary));
            _sub.SetBounds(16, 33, 528, 16); _sub.Font = Theme.Px(12); _sub.ForeColor = Theme.TextTertiary; _sub.BackColor = Theme.BgSurface;
            _sub.Text = "공식 배포처에서 받아 SHA-256을 확인한 뒤 설치합니다";
            header.Controls.Add(_sub);

            var folder = Panel(0, 57, 560, 88);
            var card = new Panel { Bounds = new Rectangle(16, 36, 436, 40), BackColor = Theme.BgCard };
            _path.BorderStyle = BorderStyle.None; _path.BackColor = Theme.BgCard; _path.ForeColor = Theme.TextPrimary;
            _path.Font = Theme.Px(14); _path.Bounds = new Rectangle(12, 11, 412, 20); _path.AccessibleName = "게임 폴더 경로";
            _path.TextChanged += delegate { OnPathChanged(); };
            card.Controls.Add(_path);
            _browse.Bounds = new Rectangle(460, 36, 84, 40); _browse.AccessibleName = "게임 폴더 찾기"; _browse.Click += Browse;
            folder.Controls.AddRange(new Control[] { Lbl("게임 폴더", 16, 12, 200, 16, Theme.Px(12), Theme.TextTertiary), card, _browse });

            var tablePanel = Panel(0, 146, 560, tableH);
            _table.Bounds = new Rectangle(0, 0, 560, tableH); _table.AccessibleName = "설치할 구성요소";
            tablePanel.Controls.Add(_table);

            var footer = Panel(0, 146 + tableH + 1, 560, 81);
            _progress.Bounds = new Rectangle(0, 0, 560, 2);
            _status.Bounds = new Rectangle(16, 14, 280, 56); _status.Font = Theme.Px(12); _status.BackColor = Theme.BgSurface;
            _uninstall.Bounds = new Rectangle(304, 16, 100, 48); _uninstall.AccessibleName = "제거"; _uninstall.Click += async delegate { await RunUninstall(); };
            _install.Bounds = new Rectangle(416, 16, 128, 48); _install.AccessibleName = "설치"; _install.Click += async delegate { await RunInstall(); };
            footer.Controls.AddRange(new Control[] { _progress, _status, _uninstall, _install });

            Controls.AddRange(new Control[] { header, folder, tablePanel, footer });
            _path.Text = initialPath ?? GameLocator.Find() ?? "";
            if (_path.Text.Length == 0) ShowWarning("게임 폴더를 찾지 못했습니다. '찾기'로 직접 선택하세요.");
            ActiveControl = _install;
        }

        private static Panel Panel(int x, int y, int w, int h) { return new Panel { Bounds = new Rectangle(x, y, w, h), BackColor = Theme.BgSurface }; }
        private static Label Lbl(string t, int x, int y, int w, int h, Font f, Color c) { return new Label { Text = t, Bounds = new Rectangle(x, y, w, h), Font = f, ForeColor = c, BackColor = Theme.BgSurface, AutoEllipsis = true }; }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            int on = 1; // 제목 표시줄도 어둡게 (Win10 20H1+는 20, 이전 빌드는 19)
            try { if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4); }
            catch (EntryPointNotFoundException) { /* 미지원 OS는 기본 제목 표시줄 */ }
        }

        private void ShowNeutral(string s) { _status.ForeColor = Theme.TextSecondary; _status.Text = s; }
        private void ShowWarning(string s) { _status.ForeColor = Theme.Warning; _status.Text = s; }

        private void OnPathChanged()
        {
            _confirmUninstall = false; _uninstall.Text = "제거";
            var dir = _path.Text.Trim();
            bool ok = GameLocator.IsGameDir(dir);
            _uninstall.Enabled = ok && Store.LoadReceipt(dir).components.Count > 0;
            if (ok)
            {
                int r = GameLocator.GetRelease(dir);
                _sub.Text = r > 0 ? "게임 빌드 r" + r + " · 공식 배포처에서 받아 SHA-256을 확인한 뒤 설치합니다" : "공식 배포처에서 받아 SHA-256을 확인한 뒤 설치합니다";
                ShowNeutral("설치하면 덮어쓰는 파일은 백업하고, 설치 기록을 남겨 나중에 제거할 수 있습니다.");
            }
        }

        // 의존/충돌 정리: 켜면 필요한 것도 켜고 충돌하는 것은 끄고, 끄면 그것을 필요로 하는 것도 끈다.
        private void OnRowToggled(int index)
        {
            var row = _table.Rows[index];
            var byId = _table.Rows.ToDictionary(r => r.C.id, StringComparer.OrdinalIgnoreCase);
            if (row.Checked)
            {
                foreach (var dep in row.C.requires) { byId[dep].Checked = true; }
                foreach (var x in row.C.conflicts) { byId[x].Checked = false; }
            }
            else
            {
                foreach (var other in _table.Rows.Where(r => r.C.requires.Contains(row.C.id, StringComparer.OrdinalIgnoreCase))) other.Checked = false;
            }
            _table.Invalidate();
        }

        private void Browse(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog { Description = "A Dance of Fire and Ice 폴더를 선택하세요", ShowNewFolderButton = false })
                if (dlg.ShowDialog(this) == DialogResult.OK) _path.Text = dlg.SelectedPath;
        }

        private void SetBusy(bool busy)
        {
            _busy = busy; _table.Locked = busy;
            _install.Enabled = !busy; _browse.Enabled = !busy; _path.Enabled = !busy;
            _uninstall.Enabled = !busy && GameLocator.IsGameDir(_path.Text.Trim()) && Store.LoadReceipt(_path.Text.Trim()).components.Count > 0;
        }

        private string CheckGame()
        {
            var dir = _path.Text.Trim();
            if (!GameLocator.IsGameDir(dir)) return "게임 폴더가 아닙니다. 'A Dance of Fire and Ice.exe'가 있는 폴더를 선택하세요.";
            if (GameLocator.IsGameRunning()) return "게임이 실행 중입니다. 게임을 끈 뒤 다시 누르세요.";
            return null;
        }

        private async Task RunInstall()
        {
            if (_busy) return;
            var problem = CheckGame();
            if (problem != null) { ShowWarning(problem); return; }
            var dir = _path.Text.Trim();
            List<Component> comps;
            try { comps = Catalog.Resolve(_catalog, _table.Rows.Where(r => r.Checked).Select(r => r.C.id)); }
            catch (System.IO.InvalidDataException ex) { ShowWarning(ex.Message); return; }

            foreach (var r in _table.Rows) r.Status = comps.Contains(r.C) ? "대기" : "-";
            _table.Invalidate();
            SetBusy(true); _install.Text = "설치 중"; _progress.Value = 0;
            ShowNeutral("내려받고 검증하는 중입니다.");
            try
            {
                Action<string, string> status = (id, s) => BeginInvoke((Action)(() => { _table.Rows.First(r => r.C.id == id).Status = s; _table.Invalidate(); }));
                Action<double> progress = p => BeginInvoke((Action)(() => _progress.Value = p));
                var report = await Task.Run(() => Installer.InstallAsync(dir, comps, status, progress));
                foreach (var r in _table.Rows)
                {
                    int[] c;
                    if (report.PerComponent.TryGetValue(r.C.id, out c)) r.Status = "변경 " + c[0] + " · 동일 " + c[1];
                }
                _table.Invalidate();
                ShowNeutral("설치 완료 · 변경 " + report.Written + " · 동일 " + report.Unchanged + (report.BackedUp > 0 ? " · 백업 " + report.BackedUp : ""));
                _install.Text = "다시 설치";
            }
            catch (Exception ex) when (ex is DownloadException || ex is InvalidOperationException || ex is System.IO.IOException || ex is System.IO.InvalidDataException || ex is UnauthorizedAccessException)
            {
                foreach (var r in _table.Rows.Where(r => r.Status != "-")) r.Status = "중단";
                _table.Invalidate();
                ShowWarning("설치 실패: " + ex.Message);
                _install.Text = "설치"; _progress.Value = 0;
            }
            finally { SetBusy(false); }
        }

        private async Task RunUninstall()
        {
            if (_busy) return;
            var problem = CheckGame();
            if (problem != null) { ShowWarning(problem); return; }
            if (!_confirmUninstall)
            {
                _confirmUninstall = true; _uninstall.Text = "정말 제거";
                ShowWarning("설치 기록에 있는 파일만 지우고 백업을 되돌립니다. 계속하려면 '정말 제거'를 누르세요.");
                return;
            }
            _confirmUninstall = false; _uninstall.Text = "제거";
            SetBusy(true);
            try
            {
                var msg = await Task.Run(() => Installer.Uninstall(_path.Text.Trim()));
                foreach (var r in _table.Rows) r.Status = "대기";
                _table.Invalidate();
                ShowNeutral("제거 완료 · " + msg);
            }
            catch (Exception ex) when (ex is System.IO.IOException || ex is UnauthorizedAccessException || ex is System.IO.InvalidDataException)
            {
                ShowWarning("제거 실패: " + ex.Message);
            }
            finally { SetBusy(false); }
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
