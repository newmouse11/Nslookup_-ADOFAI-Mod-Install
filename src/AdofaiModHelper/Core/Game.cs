using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AdofaiModHelper.Core
{
    // 얼불춤 설치 폴더 하나와, 거기서 파생되는 경로들
    public sealed class Game
    {
        public const string SteamAppId = "977950";
        public const string ExeName = "A Dance of Fire and Ice.exe";
        public const string DataFolderName = "A Dance of Fire and Ice_Data";
        const string ProcessName = "A Dance of Fire and Ice";

        public string Root { get; }
        public string ManagedDir => Path.Combine(Root, DataFolderName, "Managed");
        public string UmmDir => Path.Combine(ManagedDir, "UnityModManager");
        public string ModsDir => Path.Combine(Root, "Mods");

        Game(string root) { Root = root; }

        // 유저가 exe나 하위 폴더를 골라도 게임 루트로 올라가서 맞춰준다
        public static Game FromUserPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (File.Exists(path)) path = Path.GetDirectoryName(path);
            for (var dir = new DirectoryInfo(path); dir != null; dir = dir.Parent)
            {
                if (IsGameRoot(dir.FullName)) return new Game(dir.FullName);
            }
            return null;
        }

        static bool IsGameRoot(string dir) =>
            File.Exists(Path.Combine(dir, ExeName)) && Directory.Exists(Path.Combine(dir, DataFolderName, "Managed"));

        public static bool IsRunning()
        {
            var procs = Process.GetProcessesByName(ProcessName);
            foreach (var p in procs) p.Dispose();
            return procs.Length > 0;
        }

        public static void LaunchViaSteam() => Process.Start("steam://rungameid/" + SteamAppId);

        // UMM 안내문: 경로에 영문 외 글자가 있으면 문제가 날 수 있다
        public bool PathHasNonAscii => Root.Any(c => c > 127);

        // Steam 라이브러리 구조(<lib>\steamapps\common\<게임>)일 때만 빌드 번호를 읽을 수 있다
        public string ReadBuildId()
        {
            var acf = Path.GetFullPath(Path.Combine(Root, "..", "..", "appmanifest_" + SteamAppId + ".acf"));
            if (!File.Exists(acf)) return null;
            var m = Regex.Match(File.ReadAllText(acf), "\"buildid\"\\s+\"(\\d+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        // Steam 레지스트리 → libraryfolders.vdf → appmanifest 순으로 찾는다
        public static Game FindSteamInstall()
        {
            foreach (var library in SteamLibraries())
            {
                var steamapps = Path.Combine(library, "steamapps");
                var installDir = ReadInstallDir(Path.Combine(steamapps, "appmanifest_" + SteamAppId + ".acf")) ?? "A Dance of Fire and Ice";
                var game = FromUserPath(Path.Combine(steamapps, "common", installDir));
                if (game != null) return game;
            }
            return null;
        }

        static IEnumerable<string> SteamLibraries()
        {
            var steam = ReadRegistry(Registry.CurrentUser, @"Software\Valve\Steam", "SteamPath")
                ?? ReadRegistry(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath");
            if (steam == null) return Enumerable.Empty<string>();

            steam = steam.Replace('/', '\\');
            var libraries = new List<string> { steam };
            var vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdf))
            {
                // vdf 안의 경로는 "D:\\SteamLibrary"처럼 역슬래시가 이스케이프돼 있다
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"(.+?)\""))
                    libraries.Add(m.Groups[1].Value.Replace(@"\\", @"\"));
            }
            return libraries.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        static string ReadInstallDir(string acf)
        {
            if (!File.Exists(acf)) return null;
            var m = Regex.Match(File.ReadAllText(acf), "\"installdir\"\\s+\"(.+?)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        static string ReadRegistry(RegistryKey hive, string key, string name)
        {
            using (var k = hive.OpenSubKey(key))
                return k?.GetValue(name) as string;
        }
    }
}
