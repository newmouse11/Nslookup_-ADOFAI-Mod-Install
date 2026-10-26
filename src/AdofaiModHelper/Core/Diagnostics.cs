using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AdofaiModHelper.Core
{
    // 디스코드 문의에 그대로 붙여넣을 수 있는 상태 요약
    public static class Diagnostics
    {
        public static string Build(Game game)
        {
            var sb = new StringBuilder();
            sb.AppendLine("```");
            sb.AppendLine($"ADOFAI Mod Helper {AppInfo.Version} / {Environment.OSVersion} / .NET {Environment.Version}");
            if (game == null)
            {
                sb.AppendLine("Game folder: not found");
                sb.AppendLine("```");
                return sb.ToString();
            }

            sb.AppendLine("Game folder: " + game.Root + (game.PathHasNonAscii ? "  (non-ASCII path)" : ""));
            sb.AppendLine("Steam build: " + (game.ReadBuildId() ?? "unknown") + " / running: " + Game.IsRunning());

            var umm = Umm.Detect(game);
            sb.AppendLine($"UMM: {umm.State} {umm.Version}" + (umm.Problems.Count > 0 ? " / problems: " + string.Join(", ", umm.Problems) : ""));
            if (umm.OtherLoader != null) sb.AppendLine("Other loader: " + umm.OtherLoader);
            var ini = Path.Combine(game.Root, "doorstop_config.ini");
            if (File.Exists(ini)) sb.AppendLine("doorstop_config.ini: " + File.ReadAllText(ini).Replace("\r\n", " | "));
            var proxy = Path.Combine(game.Root, "winhttp.dll");
            if (File.Exists(proxy)) sb.AppendLine("winhttp.dll version: " + (Umm.DoorstopVersion(proxy)?.ToString() ?? "unknown"));

            var scan = Mods.Scan(game);
            sb.AppendLine($"Mods ({scan.Mods.Count}):");
            foreach (var m in scan.Mods)
                sb.AppendLine($"  {(m.Enabled ? "[on] " : "[off]")} {m.Id} {m.Version} ({Path.GetFileName(m.Folder)})");
            foreach (var p in scan.Problems)
                sb.AppendLine($"  problem: {p.Kind} {p.ModId} {p.Path?.Replace(game.Root, "<game>")}");

            AppendTail(sb, "UMM Log.txt (last 40 lines)", Path.Combine(game.UmmDir, "Log.txt"), 40);
            // 게임이 시작조차 못 할 때의 원인은 UMM 로그가 아니라 유니티 Player.log에 남는다
            AppendTail(sb, "Player.log (last 30 lines)", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "7th Beat Games", "A Dance of Fire and Ice", "Player.log"), 30);
            sb.AppendLine("--- Helper log (last 20 lines)");
            sb.AppendLine(Log.Tail(20));
            sb.AppendLine("```");
            return sb.ToString();
        }

        static void AppendTail(StringBuilder sb, string title, string path, int lines)
        {
            sb.AppendLine("--- " + title);
            if (!File.Exists(path)) { sb.AppendLine("(none)"); return; }
            try
            {
                // 게임이 켜져 있으면 UMM이 파일을 잡고 있으므로 공유 모드로 연다
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(stream))
                {
                    var all = reader.ReadToEnd().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    foreach (var line in all.Skip(Math.Max(0, all.Length - lines))) sb.AppendLine(line);
                }
            }
            catch (IOException ex)
            {
                sb.AppendLine("(read failed: " + ex.Message + ")");
            }
        }
    }
}
