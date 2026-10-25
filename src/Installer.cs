using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace OneClick
{
    internal sealed class InstallResult
    {
        public int Written, Unchanged, BackedUp;
        public string BackupDir;
        // 항목별 (변경, 동일) 개수 — 표의 상태 열에 쓴다
        public readonly Dictionary<string, int[]> PerItem = new Dictionary<string, int[]>();
    }

    internal static class GameLocator
    {
        public const string ExeName = "A Dance of Fire and Ice.exe";
        private const string Folder = "A Dance of Fire and Ice";

        public static bool IsGameDir(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && File.Exists(Path.Combine(path, ExeName))
                && Directory.Exists(Path.Combine(path, Folder + "_Data"));
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

        // 설치기가 기준으로 삼은 게임 빌드와 같은지 (Assembly-CSharp.dll 해시 비교)
        public static bool MatchesBuild(string gameDir, Manifest m)
        {
            var dll = Path.Combine(gameDir, Folder + "_Data", "Managed", "Assembly-CSharp.dll");
            return File.Exists(dll) && string.Equals(Hash(File.ReadAllBytes(dll)), m.GameHash, StringComparison.OrdinalIgnoreCase);
        }

        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }
    }

    internal static class Installer
    {
        // 페이로드를 게임 폴더에 푼다. 이미 있는 파일이 다르면 백업 폴더에 보관한 뒤 덮어쓴다.
        public static InstallResult Install(string gameDir, Manifest manifest, Action<double> progress)
        {
            var root = Path.GetFullPath(gameDir).TrimEnd('\\') + "\\";
            var expected = manifest.Files.ToDictionary(f => f.Path, f => f, StringComparer.OrdinalIgnoreCase);
            var result = new InstallResult();
            foreach (var item in manifest.Items) result.PerItem[item.Name] = new int[2];
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            result.BackupDir = Path.Combine(root, "ADOFAI-OneClick-Backup", stamp);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                var entries = zip.Entries.Where(e => e.Name.Length > 0).ToList();
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    ManifestFile mf;
                    if (!expected.TryGetValue(entry.FullName, out mf))
                        throw new InvalidDataException("매니페스트에 없는 파일: " + entry.FullName);

                    // Zip Slip 방지: 대상 경로가 게임 폴더 밖이면 거부
                    var rel = entry.FullName.Replace('/', '\\');
                    var dest = Path.GetFullPath(Path.Combine(root, rel));
                    if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("허용되지 않는 경로: " + entry.FullName);

                    byte[] bytes;
                    using (var s = entry.Open())
                    using (var ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        bytes = ms.ToArray();
                    }
                    if (!string.Equals(GameLocator.Hash(bytes), mf.Hash, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException("설치 파일이 손상되었습니다: " + entry.FullName);

                    var counts = result.PerItem[mf.Item];
                    if (File.Exists(dest))
                    {
                        if (string.Equals(GameLocator.Hash(File.ReadAllBytes(dest)), mf.Hash, StringComparison.OrdinalIgnoreCase))
                        {
                            counts[1]++; result.Unchanged++;
                            progress((i + 1.0) / entries.Count);
                            continue;
                        }
                        var backup = Path.Combine(result.BackupDir, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(backup));
                        File.Copy(dest, backup, true);
                        result.BackedUp++;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    File.WriteAllBytes(dest, bytes);
                    counts[0]++; result.Written++;
                    progress((i + 1.0) / entries.Count);
                }
            }

            // 최종 검증: 매니페스트의 모든 파일이 해시까지 일치해야 한다
            foreach (var f in manifest.Files)
            {
                var path = Path.Combine(root, f.Path.Replace('/', '\\'));
                if (!File.Exists(path) || !string.Equals(GameLocator.Hash(File.ReadAllBytes(path)), f.Hash, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("설치 후 검증 실패: " + f.Path);
            }
            return result;
        }
    }
}
