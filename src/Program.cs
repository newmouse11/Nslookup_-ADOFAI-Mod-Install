using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AdofaiModInstaller
{
    internal static class Program
    {
        // 종료 코드: 0 성공, 1 오류, 2 잘못된 인자/게임 폴더 아님, 3 게임 실행 중, 4 다운로드·해시 검증 실패
        [STAThread]
        private static int Main(string[] args)
        {
            string Opt(string name)
            {
                var a = args.FirstOrDefault(x => x.StartsWith("/" + name + ":", StringComparison.OrdinalIgnoreCase));
                return a == null ? null : a.Substring(name.Length + 2).Trim('"');
            }
            bool Flag(string name) { return args.Any(x => string.Equals(x, "/" + name, StringComparison.OrdinalIgnoreCase)); }

            var log = Opt("log") ?? Path.Combine(Path.GetTempPath(), "ADOFAI-Mod-Installer.log");
            try
            {
                if (Flag("selftest")) return SelfTest.Run(log);
                var catalog = Catalog.Load(Opt("catalog"));
                if (Flag("list")) return Report(log, 0, string.Join("\r\n", catalog.components.Select(c => c.id + "\t" + c.name + " " + c.version + "\t" + c.license)));
                if (Flag("silent") || Flag("uninstall")) return RunSilent(catalog, Opt("target"), Opt("components"), Flag("uninstall"), log);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                var state = new WizardState { Doc = catalog, GamePath = Opt("target") };
                var form = new WizardForm(state);
                var screens = Screens.Build();
                var shot = Opt("shot");
                if (shot != null)
                {
                    // 개발용 미리보기: /shot:png /page:0~4 (0 시작, 1 폴더, 2 요소, 3 설치, 4 완료)
                    int page; int.TryParse(Opt("page") ?? "0", out page);
                    state.GamePath = state.GamePath ?? GameLocator.Find();
                    state.Resolved = catalog.components.Where(c => c.defaultOn || c.required).ToList();
                    state.Summary = "변경 34 · 동일 0";
                    state.Success = true;
                    state.Preview = true; // 화면만 그린다 (실제 다운로드·설치 없음)
                    form.Go(screens[Math.Max(0, Math.Min(4, page))]);
                    form.SaveShot(shot);
                    return 0;
                }
                form.Go(screens[0]);
                Application.Run(form);
                return 0;
            }
            catch (InvalidDataException ex) { return Report(log, 2, "카탈로그/인자 오류: " + ex.Message); }
        }

        private static int Report(string log, int code, string message)
        {
            File.WriteAllText(log, message, new UTF8Encoding(false));
            return code;
        }

        private static int RunSilent(CatalogDoc catalog, string target, string components, bool uninstall, string log)
        {
            var dir = target ?? GameLocator.Find();
            if (!GameLocator.IsGameDir(dir)) return Report(log, 2, "게임 폴더를 찾지 못했습니다: " + dir);
            if (GameLocator.IsGameRunning()) return Report(log, 3, "게임이 실행 중입니다.");
            try
            {
                var ids = components == null ? null : components.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
                if (uninstall) return Report(log, 0, "ok " + Installer.Uninstall(dir, ids));
                // 생략하면 기본 선택(필수가 아닌 defaultOn). 필수·의존 항목은 게임 폴더에 없을 때만 자동으로 추가된다.
                var chosen = ids ?? catalog.components.Where(c => c.defaultOn && !c.required).Select(c => c.id).ToList();
                var comps = Catalog.Resolve(catalog, chosen, c => Catalog.IsPresent(c, dir));
                var report = Installer.InstallAsync(dir, comps, (id, s) => { }, p => { }).GetAwaiter().GetResult();
                return Report(log, 0, "ok written=" + report.Written + " unchanged=" + report.Unchanged + " backedUp=" + report.BackedUp);
            }
            catch (DownloadException ex) { return Report(log, 4, ex.Message); }
            catch (InvalidDataException ex) { return Report(log, 2, ex.Message); }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException) { return Report(log, 1, ex.Message); }
        }
    }
}
