using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

namespace AdofaiModInstaller
{
    // 마법사 전체가 공유하는 상태
    internal sealed class WizardState
    {
        public CatalogDoc Doc;
        public string GamePath;
        public int Release = -1;
        public bool IsUninstall;
        public readonly HashSet<string> SelectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public List<string> UninstallIds;          // null이면 전체 제거
        public List<Component> Resolved = new List<Component>();
        public readonly StringBuilder Log = new StringBuilder();
        public bool Success;
        public string Summary = "";
        public string Error;
        public bool Busy;      // 설치·제거 중에는 창을 닫을 수 없다 (중간에 끊기면 일부만 적용되므로)
        public bool Preview;   // /shot 미리보기: 화면만 그리고 실제 작업은 하지 않는다
    }

    internal static class Ui
    {
        public static readonly Font Arial10 = new Font("Arial", 10f);
        public static readonly Font Arial11 = new Font("Arial", 11f);
        public static readonly Font Arial12 = new Font("Arial", 12f);
        public static readonly Font Arial13 = new Font("Arial", 13f);
        public static readonly Font Arial16 = new Font("Arial", 16f);
        public static readonly Font Arial16B = new Font("Arial", 16f, FontStyle.Bold);
        public static readonly Font Arial24B = new Font("Arial", 24f, FontStyle.Bold);
        public static readonly Color SubColor = Color.FromArgb(109, 109, 109);

        // 빌드 해시가 붙은 ProductVersion 대신 1.2.3 형식으로 표시
        public static string Version { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3); } }

        public static Label Label(string text, Font font, Point at, Size? size = null, Color? color = null)
        {
            var l = new Label { Text = text, Font = font, Location = at, AutoSize = !size.HasValue };
            if (size.HasValue) l.Size = size.Value;
            if (color.HasValue) l.ForeColor = color.Value;
            return l;
        }

        // https 주소만 연다. 열 수 없으면 클립보드에 복사한다.
        public static void OpenUrl(string url)
        {
            Uri u;
            if (!Uri.TryCreate(url, UriKind.Absolute, out u) || u.Scheme != Uri.UriSchemeHttps) return;
            try { Process.Start(new ProcessStartInfo { FileName = u.AbsoluteUri, UseShellExecute = true }); }
            catch (Win32Exception)
            {
                Clipboard.SetText(u.AbsoluteUri);
                MessageBox.Show("브라우저를 열지 못했습니다. 주소를 클립보드에 복사했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static LinkLabel Link(string text, string url, Point at)
        {
            var l = new LinkLabel { Text = text, Font = Arial11, Location = at, AutoSize = true };
            l.LinkClicked += delegate { OpenUrl(url); };
            return l;
        }
    }

    // 왼쪽 배너(403x561). 원본의 그림을 쓰지 않고 직접 그린다.
    internal sealed class Banner : Control
    {
        public Banner()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Size = new Size(403, 561);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            using (var br = new LinearGradientBrush(ClientRectangle, Color.FromArgb(236, 196, 255), Color.FromArgb(170, 160, 255), 60f))
                g.FillRectangle(br, ClientRectangle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using (var big = new Font("Arial", 40f, FontStyle.Bold))
            using (var mid = new Font("Arial", 18f, FontStyle.Bold))
            using (var small = new Font("Arial", 10f, FontStyle.Bold | FontStyle.Italic))
            using (var shadow = new SolidBrush(Color.FromArgb(70, 90, 40, 160)))
            using (var white = new SolidBrush(Color.White))
            {
                var c = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                Action<string, Font, float> draw = (t, f, y) =>
                {
                    g.DrawString(t, f, shadow, new RectangleF(3, y + 3, Width, 70), c);
                    g.DrawString(t, f, white, new RectangleF(0, y, Width, 70), c);
                };
                draw("얼불춤", big, 190);
                draw("모드 설치기", big, 250);
                draw("ADOFAI Mod Installer", mid, 330);
                var r = new StringFormat { Alignment = StringAlignment.Far };
                g.DrawString("Developed by newmouse11", small, white, new RectangleF(0, 500, Width - 8, 20), r);
                g.DrawString("Inspired by JipperResourcePack Installer", small, white, new RectangleF(0, 520, Width - 8, 20), r);
            }
        }
    }

    internal abstract class Screen
    {
        public Screen Prev, Next;
        public bool Cancelable = true, Prevable = true, Nextable = true, StepsVisible = true;
        public string NextText = "다음";
        public int StepIndex = -1;
        protected WizardForm Host;
        protected WizardState State { get { return Host.State; } }

        public void Attach(WizardForm host) { Host = host; }
        public abstract void OnEnter();
        public virtual void OnLeave() { }
        public virtual bool OnNext() { return true; }
    }

    // 984x601 고정 창: 위쪽 단계 표시줄, 가운데 화면, 아래쪽 이전/다음/취소 버튼 (원본과 같은 구성)
    internal sealed class WizardForm : Form
    {
        public readonly WizardState State;
        public readonly Panel MainPanel = new Panel { Location = new Point(0, 0), Size = new Size(984, 561) };
        public readonly Panel TopPanel = new Panel { Location = new Point(0, 0), Size = new Size(984, 40), BackColor = SystemColors.ControlLight };
        public readonly Panel UnderPanel = new Panel { Location = new Point(0, 561), Size = new Size(984, 40), BackColor = SystemColors.ControlLight };
        public readonly Button PrevButton = new Button { Text = "이전", Location = new Point(710, 5), Size = new Size(80, 30) };
        public readonly Button NextButton = new Button { Text = "다음", Location = new Point(790, 5), Size = new Size(80, 30) };
        public readonly Button CancelButton2 = new Button { Text = "취소", Location = new Point(890, 5), Size = new Size(80, 30) };
        private readonly Label[] _steps = new Label[4];
        private Screen _current;

        public WizardForm(WizardState state)
        {
            State = state;
            Text = "얼불춤 모드 설치기";
            ClientSize = new Size(984, 601);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = Ui.Arial12;

            var version = new Label
            {
                Text = Ui.Version, Location = new Point(909, 547), Size = new Size(75, 14),
                TextAlign = ContentAlignment.TopRight, ForeColor = Color.FromArgb(224, 224, 224)
            };
            UnderPanel.Controls.AddRange(new Control[] { PrevButton, NextButton, CancelButton2 });
            BuildSteps();
            // 먼저 추가한 컨트롤이 위에 보인다 (버전 > 단계 표시줄 > 아래 버튼 > 가운데 화면)
            Controls.Add(version); Controls.Add(TopPanel); Controls.Add(UnderPanel); Controls.Add(MainPanel);
            PrevButton.Click += delegate { if (_current.Prev != null) Go(_current.Prev); };
            NextButton.Click += delegate { if (_current.OnNext()) Advance(); };
            CancelButton2.Click += delegate { Close(); };
        }

        private void BuildSteps()
        {
            var titles = new[] { "폴더 선택", "설치 요소 선택", "설치", "완료" };
            int x = 32;
            for (int i = 0; i < 7; i++)
            {
                var text = (i & 1) == 0 ? titles[i >> 1] : "→";
                var w = TextRenderer.MeasureText(text, Ui.Arial16, new Size(int.MaxValue, 30), TextFormatFlags.NoPadding).Width + 12;
                var l = new Label { Text = text, Font = Ui.Arial16, AutoSize = false, Location = new Point(x, 9), Size = new Size(w, 24) };
                if ((i & 1) == 0) _steps[i >> 1] = l;
                TopPanel.Controls.Add(l);
                x += w;
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (State.Busy && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                MessageBox.Show("설치·제거 중에는 닫을 수 없습니다. 끝날 때까지 기다려주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            base.OnFormClosing(e);
        }

        public void Advance()
        {
            if (_current.Next != null) Go(_current.Next);
            else { _current.OnLeave(); Close(); }
        }

        public void Go(Screen s)
        {
            if (_current != null) _current.OnLeave();
            s.Attach(this);
            MainPanel.Controls.Clear();
            TopPanel.Visible = s.StepsVisible;
            for (int i = 0; i < _steps.Length; i++) _steps[i].Font = i == s.StepIndex ? Ui.Arial16B : Ui.Arial16;
            PrevButton.Visible = s.Prev != null;
            PrevButton.Enabled = s.Prevable;
            NextButton.Enabled = s.Nextable;
            NextButton.Text = s.NextText;
            CancelButton2.Enabled = s.Cancelable;
            _current = s;
            s.OnEnter();
        }

        public void SetButtons(bool prev, bool next, bool cancel)
        {
            PrevButton.Enabled = prev; NextButton.Enabled = next; CancelButton2.Enabled = cancel;
        }

        // 개발용: 현재 화면을 PNG로 저장 (/shot:경로)
        public void SaveShot(string pngPath)
        {
            // 겹친 컨트롤(단계 표시줄)까지 실제로 그려진 모습을 얻으려고 화면을 잠깐 띄워 캡처한다
            TopMost = true; StartPosition = FormStartPosition.Manual; Location = new Point(40, 40);
            Show();
            for (int i = 0; i < 10; i++) { Application.DoEvents(); System.Threading.Thread.Sleep(60); }
            using (var bmp = new Bitmap(Width, Height))
            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(Location, Point.Empty, Size);
                bmp.Save(pngPath, System.Drawing.Imaging.ImageFormat.Png);
            }
            State.Busy = false;
            Close();
        }
    }
}
