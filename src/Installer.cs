using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace AdofaiModInstaller
{
    // 설치 영수증: 우리가 쓴 파일과 (덮어쓴 경우) 백업 위치를 기록한다. 제거는 이 기록만 따른다.
    public sealed class ReceiptFile { public string path, sha256, backup; }
    public sealed class ReceiptComponent
    {
        public string id, version;
        public List<ReceiptFile> files = new List<ReceiptFile>();
    }
    public sealed class Receipt
    {
        public int schema = 1;
        public string installedAt;
        public List<ReceiptComponent> components = new List<ReceiptComponent>();
    }

    internal sealed class InstallReport
    {
        public int Written, Unchanged, BackedUp;
        public readonly Dictionary<string, int[]> PerComponent = new Dictionary<string, int[]>(); // id -> {쓴 파일, 동일}
    }

    internal sealed class PlannedFile
    {
        public string Rel;   // 게임 폴더 기준 상대 경로(\)
        public byte[] Bytes;
    }

    internal static class Store
    {
        public static string StateDir(string game) { return Path.Combine(game, "ADOFAI-Mod-Installer"); }
        public static string ReceiptPath(string game) { return Path.Combine(StateDir(game), "receipt.json"); }

        public static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "");
        }

        // 경로 이탈(../, 절대경로, 드라이브, 대체 스트림) 차단. 통과한 전체 경로만 돌려준다.
        public static string SafeCombine(string root, string rel)
        {
            if (string.IsNullOrEmpty(rel) || rel.Contains(":") || Path.IsPathRooted(rel.Replace('/', '\\')))
                throw new InvalidDataException("허용되지 않는 경로: " + rel);
            var prefix = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            var full = Path.GetFullPath(Path.Combine(prefix, rel.Replace('/', '\\')));
            if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("게임 폴더 밖을 가리키는 경로: " + rel);
            return full;
        }

        public static Receipt LoadReceipt(string game)
        {
            var p = ReceiptPath(game);
            if (!File.Exists(p)) return new Receipt();
            return new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Receipt>(File.ReadAllText(p, Encoding.UTF8)) ?? new Receipt();
        }

        public static void SaveReceipt(string game, Receipt r)
        {
            Directory.CreateDirectory(StateDir(game));
            File.WriteAllText(ReceiptPath(game), new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(r), new UTF8Encoding(false));
        }
    }

    // 다운로드한 파일을 "어디에 무엇을 쓸지" 목록으로 바꾼다. 디스크에는 아직 쓰지 않는다.
    internal static class Planner
    {
        private static readonly string[] UmmFiles = { "0Harmony.dll", "dnlib.dll", "UnityModManager.dll", "UnityModManager.xml" };
        private const string UmmDirInZip = "UnityModManagerInstaller/";

        public static List<PlannedFile> Plan(Component c, string downloaded, string game)
        {
            var list = new List<PlannedFile>();
            if (c.kind == "file")
            {
                list.Add(new PlannedFile { Rel = c.path.Replace('/', '\\'), Bytes = File.ReadAllBytes(downloaded) });
            }
            else
            {
                using (var fs = File.OpenRead(downloaded))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    if (c.kind == "mod") PlanMod(c, zip, game, list);
                    else if (c.kind == "umm") PlanUmm(zip, list);
                    else if (c.kind == "doorstop") PlanDoorstop(zip, game, list);
                }
            }
            foreach (var f in list) Store.SafeCombine(game, f.Rel); // 계획 단계에서 경로 이탈을 모두 걸러낸다
            return list;
        }

        private static byte[] Read(ZipArchiveEntry e)
        {
            using (var s = e.Open())
            using (var ms = new MemoryStream())
            {
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        // Info.json이 zip 루트에 있으면 그대로, 최상위 폴더 하나 안에 있으면 그 폴더를 벗겨서 Mods\<dir>에 푼다.
        private static void PlanMod(Component c, ZipArchive zip, string game, List<PlannedFile> list)
        {
            var names = zip.Entries.Select(e => e.FullName.Replace('\\', '/')).ToList();
            string prefix;
            if (names.Any(n => string.Equals(n, "Info.json", StringComparison.OrdinalIgnoreCase)))
                prefix = "";
            else
            {
                var tops = names.Select(n => Regex.Match(n, @"^([^/]+)/Info\.json$", RegexOptions.IgnoreCase))
                    .Where(m => m.Success).Select(m => m.Groups[1].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (tops.Count != 1) throw new InvalidDataException(c.name + ": zip에서 Info.json 위치를 찾지 못했습니다.");
                prefix = tops[0] + "/";
            }
            foreach (var e in zip.Entries)
            {
                var name = e.FullName.Replace('\\', '/');
                if (name.EndsWith("/") || !name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                var sub = name.Substring(prefix.Length).Replace('/', '\\');
                // 게임 폴더 안이라도 이 모드의 폴더(Mods\<dir>)를 벗어나는 항목(예: ../../winhttp.dll)은 거부
                Store.SafeCombine(Path.Combine(game, "Mods", c.dir), sub);
                list.Add(new PlannedFile { Rel = "Mods\\" + c.dir + "\\" + sub, Bytes = Read(e) });
            }
        }

        private static void PlanUmm(ZipArchive zip, List<PlannedFile> list)
        {
            foreach (var f in UmmFiles)
            {
                var e = zip.GetEntry(UmmDirInZip + f);
                if (e == null) throw new InvalidDataException("UnityModManager zip에 " + f + "가 없습니다.");
                list.Add(new PlannedFile { Rel = GameLocator.DataDir + "\\Managed\\UnityModManager\\" + f, Bytes = Read(e) });
            }
            list.Add(new PlannedFile { Rel = GameLocator.DataDir + "\\Managed\\UnityModManager\\Config.xml", Bytes = BuildUmmConfig(zip) });
        }

        // UMM zip 안의 UnityModManagerConfig.xml에서 얼불춤 항목을 꺼내 게임별 Config.xml로 만든다.
        public static byte[] BuildUmmConfig(ZipArchive zip)
        {
            var entry = zip.GetEntry(UmmDirInZip + "UnityModManagerConfig.xml");
            if (entry == null) throw new InvalidDataException("UnityModManagerConfig.xml이 없습니다.");
            XDocument doc;
            using (var s = entry.Open()) doc = XDocument.Load(s);
            var info = doc.Descendants("GameInfo").FirstOrDefault(x => (string)x.Attribute("Name") == GameLocator.Folder);
            if (info == null) throw new InvalidDataException("UMM 설정에 '" + GameLocator.Folder + "' 항목이 없습니다.");
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema", xsi = "http://www.w3.org/2001/XMLSchema-instance";
            var config = new XElement("Config",
                new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute("Name", GameLocator.Folder),
                info.Elements().Select(x => new XElement(x.Name, x.Value)));
            using (var ms = new MemoryStream())
            {
                var settings = new XmlWriterSettings { Indent = true, IndentChars = "  ", Encoding = new UTF8Encoding(false) };
                using (var w = XmlWriter.Create(ms, settings))
                    new XDocument(new XDeclaration("1.0", "utf-8", null), config).Save(w);
                return ms.ToArray();
            }
        }

        private static void PlanDoorstop(ZipArchive zip, string game, List<PlannedFile> list)
        {
            var arch = GameLocator.IsX64(game) ? "x64" : "x86";
            var e = zip.GetEntry(arch + "/winhttp.dll");
            if (e == null) throw new InvalidDataException("Doorstop zip에 " + arch + "/winhttp.dll이 없습니다.");
            list.Add(new PlannedFile { Rel = "winhttp.dll", Bytes = Read(e) });
            // UMM 가이드가 쓰는 설정 그대로 (줄바꿈 CRLF, 끝 줄바꿈 없음)
            var ini = "[General]\r\nenabled = true\r\ntarget_assembly = " + GameLocator.DataDir + "\\Managed\\UnityModManager\\UnityModManager.dll";
            list.Add(new PlannedFile { Rel = "doorstop_config.ini", Bytes = new UTF8Encoding(false).GetBytes(ini) });
        }
    }

    internal static class Installer
    {
        // 1) 전부 내려받아 해시 확인 -> 2) 경로 검사 -> 3) 적용. 1~2에서 실패하면 게임 폴더는 그대로다.
        public static async Task<InstallReport> InstallAsync(string gameDir, IList<Component> comps, Action<string, string> status, Action<double> progress)
        {
            var root = Path.GetFullPath(gameDir).TrimEnd('\\') + "\\";
            var tmp = Path.Combine(Path.GetTempPath(), "AdofaiModInstaller", Guid.NewGuid().ToString("N"));
            try
            {
                var files = new Dictionary<string, string>();
                for (int i = 0; i < comps.Count; i++)
                {
                    var c = comps[i];
                    int index = i;
                    status(c.id, "다운로드");
                    files[c.id] = await Downloader.FetchAsync(c, tmp, p => progress((index + p) / comps.Count * 0.6)).ConfigureAwait(false);
                    status(c.id, "검증됨");
                }
                var plan = new List<KeyValuePair<Component, List<PlannedFile>>>();
                foreach (var c in comps) plan.Add(new KeyValuePair<Component, List<PlannedFile>>(c, Planner.Plan(c, files[c.id], root)));
                return Apply(root, plan, status, p => progress(0.6 + p * 0.4));
            }
            finally
            {
                try { Directory.Delete(tmp, true); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { /* 임시 폴더 정리 실패는 설치 결과와 무관 */ }
            }
        }

        // 쓰기 전에 기존 파일을 백업하고, 중간에 실패하면 지금까지 쓴 것을 모두 되돌린다.
        internal static InstallReport Apply(string root, List<KeyValuePair<Component, List<PlannedFile>>> plan, Action<string, string> status, Action<double> progress)
        {
            var report = new InstallReport();
            var old = Store.LoadReceipt(root);
            var oldFiles = old.components.SelectMany(c => c.files).GroupBy(f => f.path, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var backupRoot = Path.Combine(Store.StateDir(root), "backup", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            var journal = new List<Tuple<string, string>>(); // (쓴 파일 전체경로, 백업 전체경로 또는 null)
            var receipt = new Receipt { installedAt = DateTime.Now.ToString("s") };
            int total = plan.Sum(p => p.Value.Count), done = 0;

            try
            {
                foreach (var pair in plan)
                {
                    var c = pair.Key;
                    status(c.id, "설치");
                    var rc = new ReceiptComponent { id = c.id, version = c.version };
                    var counts = report.PerComponent[c.id] = new int[2];
                    foreach (var f in pair.Value)
                    {
                        var full = Store.SafeCombine(root, f.Rel);
                        var hash = Store.Hash(f.Bytes);
                        ReceiptFile prev;
                        oldFiles.TryGetValue(f.Rel, out prev);
                        string backupRel = null;
                        if (File.Exists(full))
                        {
                            var current = Store.Hash(File.ReadAllBytes(full));
                            if (string.Equals(current, hash, StringComparison.OrdinalIgnoreCase))
                            {
                                // 이미 같은 파일. 우리가 이전에 쓴 것이면 제거 대상으로 계속 기록한다.
                                counts[1]++; report.Unchanged++;
                                if (prev != null && string.Equals(prev.sha256, hash, StringComparison.OrdinalIgnoreCase)) rc.files.Add(prev);
                                progress((double)++done / total);
                                continue;
                            }
                            // 덮어쓰기 전에 항상 백업(롤백용). 우리가 이전에 쓴 파일이면 최초 원본의 백업 위치를 이어 간다.
                            var newBackupRel = Path.Combine("backup", Path.GetFileName(backupRoot), f.Rel);
                            var backupFull = Path.Combine(Store.StateDir(root), newBackupRel);
                            Directory.CreateDirectory(Path.GetDirectoryName(backupFull));
                            File.Copy(full, backupFull, true);
                            report.BackedUp++;
                            journal.Add(Tuple.Create(full, backupFull));
                            bool ours = prev != null && string.Equals(prev.sha256, current, StringComparison.OrdinalIgnoreCase);
                            backupRel = ours ? prev.backup : newBackupRel;
                        }
                        else journal.Add(Tuple.Create(full, (string)null));
                        Directory.CreateDirectory(Path.GetDirectoryName(full));
                        File.WriteAllBytes(full, f.Bytes);
                        counts[0]++; report.Written++;
                        rc.files.Add(new ReceiptFile { path = f.Rel, sha256 = hash, backup = backupRel });
                        progress((double)++done / total);
                    }
                    receipt.components.Add(rc);
                    status(c.id, "완료");
                }

                // 최종 검증
                foreach (var rc in receipt.components)
                    foreach (var rf in rc.files)
                    {
                        var full = Store.SafeCombine(root, rf.path);
                        if (!File.Exists(full) || !string.Equals(Store.Hash(File.ReadAllBytes(full)), rf.sha256, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidDataException("설치 후 검증 실패: " + rf.path);
                    }

                // 영수증 병합: 이번에 설치한 구성요소는 교체, 나머지는 유지
                var installed = new HashSet<string>(receipt.components.Select(c => c.id), StringComparer.OrdinalIgnoreCase);
                foreach (var rc in old.components.Where(c => !installed.Contains(c.id))) receipt.components.Add(rc);
                Store.SaveReceipt(root, receipt);
                return report;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidDataException)
            {
                Rollback(journal);
                throw new InvalidOperationException("설치 중 오류가 나서 변경을 되돌렸습니다: " + ex.Message, ex);
            }
        }

        private static void Rollback(List<Tuple<string, string>> journal)
        {
            for (int i = journal.Count - 1; i >= 0; i--)
            {
                var full = journal[i].Item1; var backup = journal[i].Item2;
                try
                {
                    if (backup != null && File.Exists(backup)) File.Copy(backup, full, true);
                    else if (File.Exists(full)) File.Delete(full);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    // 되돌리기도 실패하면 호출자에게 최초 오류와 함께 알린다 (백업 폴더는 남아 있음)
                }
            }
        }

        // 영수증에 기록된 파일만 제거한다. 설치 후 사용자가 바꾼 파일(해시 불일치)은 남긴다.
        public static string Uninstall(string gameDir)
        {
            var root = Path.GetFullPath(gameDir).TrimEnd('\\') + "\\";
            var receipt = Store.LoadReceipt(root);
            if (receipt.components.Count == 0) return "제거할 설치 기록이 없습니다.";
            int removed = 0, restored = 0, kept = 0;
            foreach (var c in receipt.components)
                foreach (var f in c.files)
                {
                    var full = Store.SafeCombine(root, f.path);
                    if (!File.Exists(full)) continue;
                    if (!string.Equals(Store.Hash(File.ReadAllBytes(full)), f.sha256, StringComparison.OrdinalIgnoreCase)) { kept++; continue; }
                    var backup = f.backup == null ? null : Store.SafeCombine(Store.StateDir(root), f.backup);
                    if (backup != null && File.Exists(backup)) { File.Copy(backup, full, true); restored++; }
                    else { File.Delete(full); removed++; PruneEmptyDirs(Path.GetDirectoryName(full), root); }
                }
            File.Delete(Store.ReceiptPath(root));
            return "제거 " + removed + " · 복원 " + restored + (kept > 0 ? " · 수정되어 남김 " + kept : "");
        }

        private static void PruneEmptyDirs(string dir, string root)
        {
            while (dir != null && dir.Length > root.Length && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
                dir = Path.GetDirectoryName(dir);
            }
        }
    }
}
