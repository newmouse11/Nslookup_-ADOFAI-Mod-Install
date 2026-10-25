using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OneClick
{
    internal static class Program
    {
        // 종료 코드: 0 성공, 1 오류, 2 게임 폴더 아님/빌드 불일치(/force 없음), 3 게임 실행 중
        [STAThread]
        private static int Main(string[] args)
        {
            string Opt(string name) { var a = args.FirstOrDefault(x => x.StartsWith("/" + name + ":", StringComparison.OrdinalIgnoreCase)); return a == null ? null : a.Substring(name.Length + 2).Trim('"'); }
            bool Flag(string name) { return args.Any(x => string.Equals(x, "/" + name, StringComparison.OrdinalIgnoreCase)); }

            var target = Opt("target");
            if (Flag("silent")) return RunSilent(target, Flag("force"), Opt("log"));

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new InstallerForm(target);
            var shot = Opt("shot");
            if (shot != null) { form.SaveShot(shot); return 0; }
            Application.Run(form);
            return 0;
        }

        private static int RunSilent(string target, bool force, string logPath)
        {
            var log = logPath ?? Path.Combine(Path.GetTempPath(), "ADOFAI-OneClick.log");
            try
            {
                var manifest = Manifest.LoadEmbedded();
                var dir = target ?? GameLocator.Find();
                if (!GameLocator.IsGameDir(dir)) { File.WriteAllText(log, "game folder not found: " + dir); return 2; }
                if (GameLocator.IsGameRunning()) { File.WriteAllText(log, "game is running"); return 3; }
                if (!force && !GameLocator.MatchesBuild(dir, manifest)) { File.WriteAllText(log, "game build mismatch (use /force)"); return 2; }
                var r = Installer.Install(dir, manifest, p => { });
                File.WriteAllText(log, string.Format("ok written={0} unchanged={1} backedUp={2} backup={3}", r.Written, r.Unchanged, r.BackedUp, r.BackupDir));
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText(log, "error: " + ex);
                return 1;
            }
        }
    }
}
