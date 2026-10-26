using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace AdofaiModInstaller
{
    internal static class GameLocator
    {
        public const string ExeName = "A Dance of Fire and Ice.exe";
        public const string Folder = "A Dance of Fire and Ice";
        public const string DataDir = "A Dance of Fire and Ice_Data";

        public static bool IsGameDir(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && File.Exists(Path.Combine(path, ExeName))
                && Directory.Exists(Path.Combine(path, DataDir));
        }

        // 스팀 레지스트리 -> libraryfolders.vdf -> 흔한 드라이브 경로 순으로 찾는다.
        public static string Find()
        {
            return Candidates().FirstOrDefault(IsGameDir);
        }

        private static IEnumerable<string> Candidates()
        {
            var steamRoots = new List<string>();
            foreach (var key in new[] { @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", @"HKEY_LOCAL_MACHINE\SOFTWARE\Valve\Steam" })
            {
                var p = Registry.GetValue(key, "InstallPath", null) as string;
                if (!string.IsNullOrEmpty(p)) steamRoots.Add(p);
            }
            foreach (var root in steamRoots)
            {
                yield return Path.Combine(root, "steamapps", "common", Folder);
                var vdf = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf)) continue;
                foreach (Match m in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\""))
                    yield return Path.Combine(m.Groups[1].Value.Replace("\\\\", "\\"), "steamapps", "common", Folder);
            }
            foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
            {
                yield return Path.Combine(d.Name, "SteamLibrary", "steamapps", "common", Folder);
                yield return Path.Combine(d.Name, "Steam", "steamapps", "common", Folder);
            }
        }

        public static bool IsGameRunning()
        {
            return Process.GetProcessesByName("A Dance of Fire and Ice").Length > 0;
        }

        // 게임 exe가 64비트면 x64 Doorstop을 쓴다. 읽을 수 없으면(테스트용 가짜 exe 등) 64비트로 본다.
        public static bool IsX64(string gameDir)
        {
            try
            {
                var b = File.ReadAllBytes(Path.Combine(gameDir, ExeName));
                int pe = BitConverter.ToInt32(b, 60);
                if (BitConverter.ToUInt32(b, pe) != 0x4550) return true; // "PE\0\0"
                var machine = BitConverter.ToUInt16(b, pe + 4);
                return machine == 0x8664 || machine == 0x0200;
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is IndexOutOfRangeException)
            {
                return true;
            }
        }

        // 게임 빌드 번호(r번호). 게임 코드를 실행하지 않고 상수 값만 읽는다. 실패하면 -1.
        public static int GetRelease(string gameDir)
        {
            try
            {
                var dll = Path.Combine(gameDir, DataDir, "Managed", "Assembly-CSharp.dll");
                if (!File.Exists(dll)) return -1;
                ResolveEventHandler none = (s, e) => null; // 의존 DLL은 불러오지 않는다 (상수만 읽음)
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += none;
                try
                {
                    var asm = Assembly.ReflectionOnlyLoad(File.ReadAllBytes(dll));
                    foreach (var typeName in new[] { "GCNS", "Releases" })
                    {
                        var t = asm.GetType(typeName);
                        var f = t == null ? null : t.GetField("releaseNumber", BindingFlags.Static | BindingFlags.Public);
                        if (f != null) return (int)f.GetRawConstantValue();
                    }
                    return -1;
                }
                finally { AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= none; }
            }
            catch (Exception ex) when (ex is IOException || ex is BadImageFormatException || ex is FileLoadException || ex is TypeLoadException || ex is InvalidCastException)
            {
                return -1;
            }
        }
    }
}
