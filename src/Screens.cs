using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AdofaiModInstaller
{
    internal static class Screens
    {
        public const string RepoUrl = "https://github.com/newmouse11/Nslookup_-ADOFAI-Mod-Install";

        // 시작 -> 폴더 선택 -> 설치 요소 선택 -> 설치 -> 완료
        public static Screen[] Build()
        {
            var welcome = new WelcomeScreen(); var location = new LocationScreen(); var select = new SelectScreen();
            var install = new InstallScreen(); var finish = new FinishScreen();
            welcome.Next = location;
            location.Prev = welcome; location.Next = select;
            select.Prev = location; select.Next = install;
            install.Prev = select; install.Next = finish;
            finish.Prev = install;
            return new Screen[] { welcome, location, select, install, finish };
        }

        // 버그/문의·출처 링크 줄 (시작·완료 화면 공통)
        public static void AddLinks(Panel panel)
        {
            panel.Controls.Add(Ui.Label("문의 / 출처", Ui.Arial12, new Point(440, 444)));
            panel.Controls.Add(Ui.Link("GitHub 저장소", RepoUrl, new Point(445, 474)));
            panel.Controls.Add(Ui.Link("이슈 · 문의", RepoUrl + "/issues", new Point(445, 498)));
            panel.Controls.Add(Ui.Link("출처 · 라이선스", RepoUrl + "/blob/main/NOTICE.md", new Point(600, 474)));
            panel.Controls.Add(Ui.Link("바꾼 내용", RepoUrl + "/blob/main/CHANGES.md", new Point(600, 498)));
        }
    }

    // 1. 시작
    internal sealed class WelcomeScreen : Screen
    {
        public WelcomeScreen() { StepsVisible = false; }

        public override void OnEnter()
        {
            var p = Host.MainPanel;
            p.SuspendLayout();
            p.Controls.Add(new Banner { Location = new Point(0, 0) });
            p.Controls.Add(Ui.Label("얼불춤 모드 설치를 시작합니다.", Ui.Arial24B, new Point(430, 48), new Size(520, 40)));
            var lines = new[]
            {
                "UnityModManager와 모드를 공식 배포처에서 받아 얼불춤에 설치합니다.",
                "설치하려면 GitHub와 Dropbox에 접속할 수 있는 인터넷이 필요합니다.",
                "받은 파일은 SHA-256을 확인한 뒤에만 설치하고, 덮어쓰는 파일은 백업합니다.",
                "설치 기록이 남아서 나중에 '제거'로 되돌릴 수 있습니다.",
                "이 프로그램은 7th Beat Games의 공식 프로그램이 아닌 커뮤니티 도구입니다.",
                "게임이 실행 중이면 설치할 수 없습니다. 게임을 끄고 진행해주세요.",
                "계속하시려면 '다음' 버튼을 눌러주세요."
            };
            for (int i = 0; i < lines.Length; i++)
                p.Controls.Add(Ui.Label(lines[i], Ui.Arial12, new Point(435, 140 + i * 30), new Size(517, 24)));
            Screens.AddLinks(p);
            p.ResumeLayout();
        }

        public override bool OnNext()
        {
            if (NetworkInterface.GetAllNetworkInterfaces().Any(n => n.OperationalStatus == OperationalStatus.Up
                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel))
                return true;
            MessageBox.Show("인터넷에 연결되어 있지 않습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    // 2. 폴더 선택
    internal sealed class LocationScreen : Screen
    {
        private TextBox _box;
        private GroupBox _notify;
        private string _error;
        private int _generation;

        public LocationScreen() { StepIndex = 0; }

        public override void OnEnter()
        {
            if (string.IsNullOrEmpty(State.GamePath)) State.GamePath = GameLocator.Find();
            var p = Host.MainPanel;
            p.SuspendLayout();
            p.Controls.Add(Ui.Label("얼불춤 폴더를 선택해주세요.", Ui.Arial16, new Point(30, 62)));
            p.Controls.Add(Ui.Label("얼불춤에 모드를 설치합니다.\n다른 폴더에 설치하거나 프로그램이 폴더를 찾지 못했다면 '찾아보기' 버튼을 눌러 얼불춤 폴더를 선택해주세요.", Ui.Arial13, new Point(32, 99)));
            p.Controls.Add(Ui.Label("얼불춤 폴더 선택", Ui.Arial13, new Point(35, 230)));
            _box = new TextBox { Text = State.GamePath ?? "", Location = new Point(38, 254), Font = Ui.Arial13, Size = new Size(770, 50) };
            _box.TextChanged += delegate { Update(); };
            p.Controls.Add(_box);
            var browse = new Button { Text = "찾아보기", Location = new Point(818, 254), Font = Ui.Arial13, Size = new Size(130, 30) };
            browse.Click += delegate
            {
                using (var dlg = new FolderBrowserDialog { Description = "A Dance of Fire and Ice 폴더를 선택하세요", ShowNewFolderButton = false })
                    if (dlg.ShowDialog(Host) == DialogResult.OK) _box.Text = dlg.SelectedPath;
            };
            p.Controls.Add(browse);
            _notify = new GroupBox();
            p.Controls.Add(_notify);
            p.ResumeLayout();
            Update();
        }

        public override void OnLeave()
        {
            if (_box != null) State.GamePath = _box.Text.Trim();
        }

        public override bool OnNext()
        {
            if (_error == null) { State.GamePath = _box.Text.Trim(); return true; }
            MessageBox.Show(_error, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        // 경로 분석: 항목마다 한 줄 (kind 0 정상, 1 오류, 2 주의)
        private int _count;

        private Label Notify(int kind, string text, string sub = null)
        {
            var glyph = new Label { Text = kind == 0 ? "✓" : kind == 1 ? "✗" : "!", Font = new Font("Segoe UI Symbol", 13f, FontStyle.Bold),
                ForeColor = kind == 0 ? Color.FromArgb(0, 128, 0) : kind == 1 ? Color.FromArgb(192, 0, 0) : Color.FromArgb(200, 110, 0),
                Location = new Point(14, 48 + 32 * _count), AutoSize = true };
            var label = new Label { Text = text, Font = Ui.Arial12, Location = new Point(42, 51 + 32 * _count), AutoSize = true };
            _notify.Controls.Add(glyph); _notify.Controls.Add(label);
            Label subLabel = null;
            if (sub != null)
            {
                _count++;
                subLabel = new Label { Text = sub, Font = Ui.Arial11, ForeColor = Ui.SubColor, Location = new Point(42, 51 + 32 * _count), AutoSize = true };
                _notify.Controls.Add(subLabel);
            }
            _count++;
            return label;
        }

        private void Update()
        {
            int gen = ++_generation;
            _notify.SuspendLayout();
            _notify.Controls.Clear();
            _count = 0;
            _error = null;
            State.Release = -1;
            _notify.Controls.Add(new Label { Text = "경로 분석", Font = Ui.Arial16B, Location = new Point(12, 16), AutoSize = true });
            var path = _box.Text.Trim();
            try
            {
                if (string.IsNullOrWhiteSpace(path)) { _error = "위치가 설정되지 않았습니다."; Notify(1, "폴더 위치가 설정되어 있지 않습니다"); return; }
                if (!Directory.Exists(path)) { _error = "폴더가 존재하지 않습니다."; Notify(1, "폴더가 존재하지 않습니다"); return; }
                Notify(0, "폴더가 존재합니다");
                if (!GameLocator.IsGameDir(path)) { _error = "선택한 폴더는 얼불춤 폴더가 아닙니다."; Notify(1, "게임 실행 파일이나 데이터를 찾을 수 없습니다"); return; }
                var found = Notify(0, "게임을 찾았습니다");
                Task.Run(() => GameLocator.GetRelease(path)).ContinueWith(t =>
                {
                    if (gen != _generation || t.IsFaulted || Host.IsDisposed) return;
                    Host.BeginInvoke((Action)(() => { State.Release = t.Result; if (t.Result > 0) found.Text = "게임을 찾았습니다 (r" + t.Result + ")"; }));
                });
                var umm = Path.Combine(path, GameLocator.DataDir, "Managed", "UnityModManager", "UnityModManager.dll");
                var winhttp = Path.Combine(path, "winhttp.dll");
                if (File.Exists(umm))
                {
                    if (!File.Exists(winhttp))
                        Notify(2, "UnityModManager가 Assembly 방식으로 설치되어 있습니다", "게임을 업데이트할 때마다 다시 설치해야 합니다.");
                    else
                    {
                        Version v;
                        var fv = FileVersionInfo.GetVersionInfo(winhttp).FileVersion;
                        if (Version.TryParse(fv, out v) && v < new Version(4, 4, 0, 0))
                            Notify(2, "Doorstop이 구버전입니다 (" + fv + ")", "최신 게임에서 충돌할 수 있어 설치 요소에서 다시 설치하는 것을 권장합니다.");
                        else Notify(0, "UnityModManager가 설치되어 있습니다");
                    }
                }
                var receipt = Store.LoadReceipt(path);
                if (receipt.components.Count > 0) Notify(0, "이 설치기로 설치한 기록이 있습니다 (구성요소 " + receipt.components.Count + "개)");
            }
            finally
            {
                int h = 32 * _count;
                _notify.Location = new Point(38, 492 - h);
                _notify.Size = new Size(909, 50 + h);
                _notify.ResumeLayout();
            }
        }
    }

    // 3. 설치 요소 선택
    internal sealed class SelectScreen : Screen
    {
        private RadioButton _install, _uninstall;
        private Panel _contents;
        private readonly Dictionary<string, CheckBox> _checks = new Dictionary<string, CheckBox>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Component> _byId = new Dictionary<string, Component>(StringComparer.OrdinalIgnoreCase);
        private RadioButton _uninstallAll, _uninstallSome;
        private Panel _uninstallList;
        private Label _infoTitle, _infoMeta, _infoNote;
        private LinkLabel _infoLink;
        private bool _updating;

        public SelectScreen() { StepIndex = 1; }

        private bool Present(Component c) { return Catalog.IsPresent(c, State.GamePath); }

        // 체크박스 글자: 이름 버전 + 이미 설치되어 있으면 그 버전
        private string Label(Component c)
        {
            var v = Catalog.InstalledVersion(c, State.GamePath);
            // 같은 버전이면 "(설치됨)", 다르면 설치된 버전을 함께 표시
            return c.name + " " + c.version + (v == null ? "" : v.Length == 0 || v == c.version ? " (설치됨)" : " (설치됨 " + v + ")");
        }

        public override void OnEnter()
        {
            _byId.Clear();
            foreach (var c in State.Doc.components) _byId[c.id] = c;
            var receipt = Store.LoadReceipt(State.GamePath);
            var p = Host.MainPanel;
            p.SuspendLayout();
            p.Controls.Add(Ui.Label("설치 요소를 선택해주세요.", Ui.Arial16, new Point(30, 62)));
            p.Controls.Add(Ui.Label("작업", Ui.Arial13, new Point(34, 103)));
            var radios = new Panel { Location = new Point(40, 125), Size = new Size(910, 22) };
            _install = new RadioButton { Text = "설치", Font = Ui.Arial13, AutoSize = true, Location = new Point(0, 0), Checked = !State.IsUninstall };
            _uninstall = new RadioButton { Text = "제거", Font = Ui.Arial13, AutoSize = true, Location = new Point(80, 0), Checked = State.IsUninstall, Enabled = receipt.components.Count > 0 };
            _uninstall.CheckedChanged += delegate { State.IsUninstall = _uninstall.Checked; BuildContents(); };
            radios.Controls.Add(_install); radios.Controls.Add(_uninstall);
            p.Controls.Add(radios);
            _contents = new Panel { Location = new Point(34, 169), Size = new Size(916, 380) };
            p.Controls.Add(_contents);
            p.ResumeLayout();
            BuildContents();
        }

        public override void OnLeave()
        {
            if (_checks.Count > 0)
            {
                State.SelectedIds.Clear();
                foreach (var kv in _checks.Where(kv => kv.Value.Checked)) State.SelectedIds.Add(kv.Key);
            }
        }

        private void BuildContents()
        {
            _contents.SuspendLayout();
            _contents.Controls.Clear();
            _checks.Clear();
            if (State.IsUninstall) BuildUninstall(); else BuildInstall();
            _contents.ResumeLayout();
        }

        private void BuildInstall()
        {
            bool restored = State.SelectedIds.Count > 0;
            _contents.Controls.Add(Ui.Label("필수 요소 설치", Ui.Arial13, new Point(0, 0)));
            var req = new Panel { Location = new Point(6, 28), Size = new Size(910, 22) };
            int x = 0;
            foreach (var c in State.Doc.components.Where(c => c.required))
            {
                bool present = Present(c);
                var cb = new CheckBox { Text = Label(c), Font = Ui.Arial11, AutoSize = true, Location = new Point(x, 0),
                    Checked = restored ? State.SelectedIds.Contains(c.id) : !present, Enabled = present };
                Hook(cb, c);
                req.Controls.Add(cb);
                x += 280;
            }
            _contents.Controls.Add(req);
            _contents.Controls.Add(Ui.Label("추가 모드 설치", Ui.Arial13, new Point(0, 71)));
            var optional = State.Doc.components.Where(c => !c.required).ToList();
            int rows = (optional.Count + 2) / 3;
            var grid = new Panel { Location = new Point(6, 96), Size = new Size(910, Math.Max(25, rows * 25)) };
            for (int i = 0; i < optional.Count; i++)
            {
                var c = optional[i];
                bool present = Present(c);
                var cb = new CheckBox { Text = Label(c), Font = Ui.Arial11, Size = new Size(300, 24),
                    Location = new Point(i % 3 * 303, i / 3 * 25), Checked = restored ? State.SelectedIds.Contains(c.id) : (c.defaultOn && !present) };
                Hook(cb, c);
                grid.Controls.Add(cb);
            }
            _contents.Controls.Add(grid);

            var info = new GroupBox { Text = "구성요소 정보", Font = Ui.Arial11, Location = new Point(0, 96 + grid.Height + 14), Size = new Size(916, 380 - (96 + grid.Height + 14)) };
            _infoTitle = new Label { Font = Ui.Arial13, Location = new Point(12, 26), AutoSize = true, Text = "항목에 마우스를 올리면 만든 사람, 라이선스, 출처가 표시됩니다." };
            _infoMeta = new Label { Font = Ui.Arial11, ForeColor = Ui.SubColor, Location = new Point(12, 52), AutoSize = true };
            _infoNote = new Label { Font = Ui.Arial11, Location = new Point(12, 76), Size = new Size(890, 40) };
            _infoLink = new LinkLabel { Font = Ui.Arial11, Location = new Point(12, 120), AutoSize = true, Visible = false };
            _infoLink.LinkClicked += delegate { if (_infoLink.Tag != null) Ui.OpenUrl((string)_infoLink.Tag); };
            info.Controls.AddRange(new Control[] { _infoTitle, _infoMeta, _infoNote, _infoLink });
            _contents.Controls.Add(info);
        }

        private void Hook(CheckBox cb, Component c)
        {
            _checks[c.id] = cb;
            cb.CheckedChanged += delegate { if (!_updating) Propagate(c, cb.Checked); ShowInfo(c); };
            cb.MouseEnter += delegate { ShowInfo(c); };
            cb.Enter += delegate { ShowInfo(c); };
        }

        // 켜면 필요한 것을 켜고 충돌하는 것은 끄고, 끄면 그것을 필요로 하는 것도 끈다
        private void Propagate(Component c, bool on)
        {
            _updating = true;
            try
            {
                if (on)
                {
                    foreach (var dep in c.requires)
                        if ((c.exact || !Present(_byId[dep])) && _checks.ContainsKey(dep)) _checks[dep].Checked = true;
                    foreach (var x in c.conflicts) if (_checks.ContainsKey(x)) _checks[x].Checked = false;
                }
                else
                {
                    foreach (var other in State.Doc.components.Where(o => o.requires.Contains(c.id, StringComparer.OrdinalIgnoreCase) && (o.exact || !Present(c))))
                        if (_checks.ContainsKey(other.id)) _checks[other.id].Checked = false;
                }
            }
            finally { _updating = false; }
        }

        private void ShowInfo(Component c)
        {
            if (_infoTitle == null) return;
            _infoTitle.Text = c.name + " " + c.version;
            _infoMeta.Text = "만든 사람 " + c.author + "  ·  라이선스 " + c.license;
            _infoNote.Text = c.note ?? "";
            _infoLink.Text = "출처: " + c.homepage;
            _infoLink.Tag = c.homepage;
            _infoLink.Visible = !string.IsNullOrEmpty(c.homepage);
        }

        private void BuildUninstall()
        {
            var receipt = Store.LoadReceipt(State.GamePath);
            _contents.Controls.Add(Ui.Label("제거 옵션", Ui.Arial13, new Point(0, 0)));
            var panel = new Panel { Location = new Point(6, 28), Size = new Size(910, 62) };
            _uninstallAll = new RadioButton { Text = "전체 제거", Font = Ui.Arial13, Size = new Size(110, 22), Location = new Point(0, 0), Checked = State.UninstallIds == null };
            _uninstallSome = new RadioButton { Text = "선택한 것만 제거", Font = Ui.Arial13, Size = new Size(160, 22), Location = new Point(0, 30), Checked = State.UninstallIds != null };
            panel.Controls.Add(_uninstallAll);
            panel.Controls.Add(Ui.Label("이 설치기로 설치한 모든 구성요소를 제거합니다.", Ui.Arial11, new Point(112, 5), null, Ui.SubColor));
            panel.Controls.Add(_uninstallSome);
            panel.Controls.Add(Ui.Label("아래에서 고른 구성요소만 제거합니다. 나머지는 계속 동작합니다.", Ui.Arial11, new Point(162, 35), null, Ui.SubColor));
            _contents.Controls.Add(panel);
            _contents.Controls.Add(Ui.Label("설치 기록 (" + receipt.components.Count + "개)", Ui.Arial13, new Point(0, 104)));
            _uninstallList = new Panel { Location = new Point(6, 132), Size = new Size(910, 150), AutoScroll = true };
            for (int i = 0; i < receipt.components.Count; i++)
            {
                var rc = receipt.components[i];
                Component c;
                var name = _byId.TryGetValue(rc.id, out c) ? c.name : rc.id;
                var cb = new CheckBox { Text = name + " " + rc.version + " · 파일 " + rc.files.Count + "개", Font = Ui.Arial11, Size = new Size(430, 24),
                    Location = new Point(i % 2 * 440, i / 2 * 25), Checked = State.UninstallIds == null || State.UninstallIds.Contains(rc.id, StringComparer.OrdinalIgnoreCase), Tag = rc.id };
                _uninstallList.Controls.Add(cb);
            }
            _contents.Controls.Add(_uninstallList);
            _contents.Controls.Add(Ui.Label("설치 기록에 있는 파일만 지우고, 덮어썼던 파일은 백업에서 되돌립니다.\n설치한 뒤 직접 수정한 파일은 지우지 않고 남겨둡니다.", Ui.Arial11, new Point(0, 296), null, Ui.SubColor));
            Action sync = () => { foreach (Control c in _uninstallList.Controls) c.Enabled = _uninstallSome.Checked; };
            _uninstallSome.CheckedChanged += delegate { sync(); };
            sync();
        }

        public override bool OnNext()
        {
            if (GameLocator.IsGameRunning())
            {
                MessageBox.Show("게임이 실행 중입니다. 게임을 끈 뒤 다시 눌러주세요.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (State.IsUninstall)
            {
                State.UninstallIds = _uninstallAll.Checked ? null
                    : _uninstallList.Controls.OfType<CheckBox>().Where(c => c.Checked).Select(c => (string)c.Tag).ToList();
                if (State.UninstallIds != null && State.UninstallIds.Count == 0)
                {
                    MessageBox.Show("제거할 구성요소를 하나 이상 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
                return MessageBox.Show("선택한 구성요소를 제거할까요?\n설치 기록에 있는 파일만 지우고 백업을 되돌립니다.", "제거 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
            }
            var ids = _checks.Where(kv => kv.Value.Checked).Select(kv => kv.Key).ToList();
            try { State.Resolved = Catalog.Resolve(State.Doc, ids, c => Present(c)); }
            catch (InvalidDataException ex) { MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); return false; }
            if (State.Resolved.Count == 0)
            {
                MessageBox.Show("설치할 구성요소를 하나 이상 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
    }

    // 4. 설치 (진행 막대 + 로그)
    internal sealed class InstallScreen : Screen
    {
        private ProgressBar _bar;
        private Label _current;
        private TextBox _log;

        public InstallScreen() { StepIndex = 2; Cancelable = false; Prevable = false; Nextable = false; }

        public override void OnEnter()
        {
            var p = Host.MainPanel;
            _bar = new ProgressBar { Minimum = 0, Maximum = 1000, Size = new Size(800, 20), Location = new Point(92, 120) };
            _current = new Label { Font = Ui.Arial12, AutoSize = true, Location = new Point(92, 95) };
            _log = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = Ui.Arial10, BackColor = Color.Silver,
                Location = new Point(92, 150), Size = new Size(800, 400) };
            p.Controls.Add(_bar); p.Controls.Add(_current); p.Controls.Add(_log);
            State.Log.Clear(); State.Success = false; State.Error = null; State.Summary = "";
            Write("ADOFAI Mod Installer v" + Ui.Version);
            Write("게임 폴더: " + State.GamePath + (State.Release > 0 ? " (r" + State.Release + ")" : ""));
            Write("작업: " + (State.IsUninstall ? "제거" : "설치"));
            if (!State.IsUninstall) Write("구성요소: " + string.Join(", ", State.Resolved.Select(c => c.name + " " + c.version)));
            Write("작업을 시작합니다...");
            if (State.Preview)
            {
                Write("UnityModManager 0.33.2 다운로드 시작");
                Write("UnityModManager 다운로드 완료, SHA-256 일치");
                _bar.Value = 420;
                return; // 미리보기에서는 실제 작업을 하지 않는다
            }
            Run();
        }

        private void Write(string line)
        {
            Action add = () => { _log.AppendText(line + "\r\n"); State.Log.AppendLine(line); _current.Text = line; };
            if (Host.InvokeRequired) Host.BeginInvoke(add); else add();
        }

        private async void Run()
        {
            State.Busy = true;
            try
            {
                if (State.IsUninstall)
                {
                    State.Summary = await Task.Run(() => Installer.Uninstall(State.GamePath, State.UninstallIds, Write));
                    Write("완료: " + State.Summary);
                    Host.BeginInvoke((Action)(() => _bar.Value = _bar.Maximum));
                }
                else
                {
                    var report = await Task.Run(() => Installer.InstallAsync(State.GamePath, State.Resolved, (id, s) => { },
                        pr => Host.BeginInvoke((Action)(() => _bar.Value = Math.Min(_bar.Maximum, (int)(pr * 1000)))), Write));
                    State.Summary = "변경 " + report.Written + " · 동일 " + report.Unchanged + (report.BackedUp > 0 ? " · 백업 " + report.BackedUp : "");
                    Write("완료: " + State.Summary);
                }
                State.Success = true;
            }
            catch (Exception ex) when (ex is DownloadException || ex is InvalidOperationException || ex is IOException || ex is InvalidDataException || ex is UnauthorizedAccessException)
            {
                State.Error = ex.Message;
                Write("오류: " + ex.Message);
            }
            State.Busy = false;
            if (!Host.IsDisposed) Host.Advance();
        }
    }

    // 5. 완료
    internal sealed class FinishScreen : Screen
    {
        private CheckBox _run;

        public FinishScreen() { StepsVisible = false; NextText = "종료"; Cancelable = false; Prevable = false; }

        public override void OnEnter()
        {
            var p = Host.MainPanel;
            p.SuspendLayout();
            p.Controls.Add(new Banner { Location = new Point(0, 0) });
            string title = State.Error != null ? "작업에 실패했습니다." : State.IsUninstall ? "제거가 완료되었습니다." : "설치가 완료되었습니다.";
            string desc = State.Error != null
                ? "작업 중 오류가 나서 변경을 되돌렸습니다.\n" + State.Error + "\n자세한 내용은 '로그 보기' 버튼으로 확인하세요."
                : (State.IsUninstall ? "선택한 구성요소를 제거했습니다." : "선택한 구성요소를 설치했습니다.") + "\n" + State.Summary + "\n'종료' 버튼을 누르면 프로그램이 닫힙니다.";
            p.Controls.Add(Ui.Label(title, Ui.Arial24B, new Point(430, 48), new Size(520, 40)));
            p.Controls.Add(Ui.Label(desc, Ui.Arial12, new Point(435, 128), new Size(517, 80)));
            if (State.Error == null && !State.IsUninstall)
            {
                _run = new CheckBox { Text = "얼불춤 실행", Font = Ui.Arial12, AutoSize = true, Location = new Point(440, 216), Checked = true };
                p.Controls.Add(_run);
            }
            else if (State.Error != null)
            {
                var b = new Button { Text = "로그 보기", Font = Ui.Arial12, AutoSize = true, Location = new Point(440, 216) };
                b.Click += delegate
                {
                    var path = Path.Combine(Path.GetTempPath(), "ADOFAI-Mod-Installer-log.txt");
                    File.WriteAllText(path, State.Log.ToString(), new System.Text.UTF8Encoding(false));
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                };
                p.Controls.Add(b);
            }
            Screens.AddLinks(p);
            p.ResumeLayout();
        }

        public override void OnLeave()
        {
            if (_run == null || !_run.Checked) return;
            try
            {
                var steam = GameLocator.Find();
                if (steam != null && string.Equals(Path.GetFullPath(steam).TrimEnd('\\'), Path.GetFullPath(State.GamePath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    Process.Start(new ProcessStartInfo { FileName = "steam://rungameid/977950", UseShellExecute = true });
                else
                    Process.Start(new ProcessStartInfo { FileName = Path.Combine(State.GamePath, GameLocator.ExeName), WorkingDirectory = State.GamePath });
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show("게임을 실행하지 못했습니다: " + ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
