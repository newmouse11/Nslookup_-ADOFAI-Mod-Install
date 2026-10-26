using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AdofaiModHelper.Core
{
    public enum UmmState { NotInstalled, Installed, Broken }

    public sealed class UmmStatus
    {
        public UmmState State { get; set; }
        public Version Version { get; set; }
        // 문자열 리소스 키 (UI가 언어에 맞게 표시)
        public List<string> Problems { get; } = new List<string>();
        // 같은 게임에 깔린 다른 모드 로더 (MelonLoader / BepInEx)
        public string OtherLoader { get; set; }
    }

    // UMM 공식 설치기(UnityModManagerApp/Form.cs)의 DoorstopProxy 설치를 그대로 재현한다
    public static class Umm
    {
        public const string DownloadUrl = "https://www.dropbox.com/s/wz8x8e4onjdfdbm/UnityModManager.zip?dl=1";
        public const string VersionFeedUrl = "https://raw.githubusercontent.com/newman55/unity-mod-manager/master/Repository_beta_13.json";
        const string GameName = "A Dance of Fire and Ice";
        const string StarterMarker = "UnityModManagerStarter";
        const string DoorstopDll = "winhttp.dll";
        const string DoorstopIni = "doorstop_config.ini";

        // 공식 zip(0.33.2)의 UnityModManagerConfig.xml에 든 ADOFAI 항목과 동일. zip에서 못 읽을 때만 쓴다
        const string FallbackGameInfo =
            "<GameInfo Name=\"A Dance of Fire and Ice\">" +
            "<Folder>ADOFAI</Folder><ModsDirectory>Mods</ModsDirectory><ModInfo>Info.json</ModInfo>" +
            "<GameExe>A Dance of Fire and Ice.exe</GameExe>" +
            "<EntryPoint>[UnityEngine.CoreModule.dll]UnityEngine.MonoBehaviour.cctor:Before</EntryPoint>" +
            "<StartingPoint>[Assembly-CSharp.dll]ADOStartup.Startup:Before</StartingPoint>" +
            "<UIStartingPoint>[Assembly-CSharp.dll]ADOStartup.Startup:After</UIStartingPoint>" +
            "<MinimalManagerVersion>0.22.14</MinimalManagerVersion>" +
            "<Comment>Required minimum game version 2.7.0</Comment></GameInfo>";

        // 예전 Assembly 방식이 패치했을 수 있는 dll (현재/구버전 EntryPoint)
        static readonly string[] AssemblyPatchTargets = { "UnityEngine.CoreModule.dll", "UnityEngine.UIModule.dll" };
        static readonly string[] RequiredLibs = { "0Harmony.dll", "dnlib.dll", "UnityModManager.dll", "UnityModManager.xml" };

        static string ManagerDll(Game g) => Path.Combine(g.UmmDir, "UnityModManager.dll");
        static string TargetAssembly => Game.DataFolderName + @"\Managed\UnityModManager\UnityModManager.dll";

        public static UmmStatus Detect(Game game)
        {
            var status = new UmmStatus { OtherLoader = DetectOtherLoader(game) };
            var dll = ManagerDll(game);
            var ini = Path.Combine(game.Root, DoorstopIni);
            var proxy = Path.Combine(game.Root, DoorstopDll);
            bool pointsToUmm = File.Exists(ini) && IniTargetsUmm(ini);

            if (!File.Exists(dll))
            {
                status.State = pointsToUmm ? UmmState.Broken : UmmState.NotInstalled;
                if (pointsToUmm) status.Problems.Add("P.UmmFilesMissing");
                return status;
            }

            try
            {
                status.Version = AssemblyName.GetAssemblyName(dll).Version;
            }
            catch (Exception ex) when (ex is BadImageFormatException || ex is IOException)
            {
                Log.Warn("UnityModManager.dll 버전 읽기 실패", ex);
                status.Problems.Add("P.UmmFilesMissing");
            }

            if (!File.Exists(proxy) || !pointsToUmm) status.Problems.Add("P.DoorstopMissing");
            else if (DoorstopVersion(proxy) is Version dv && dv < new Version(4, 4))
                status.Problems.Add("P.DoorstopOld"); // 4.4 미만은 게임 r141부터 크래시 (커뮤니티 보고)
            if (!File.Exists(Path.Combine(game.UmmDir, "Config.xml"))) status.Problems.Add("P.UmmConfigMissing");
            if (RequiredLibs.Any(f => !File.Exists(Path.Combine(game.UmmDir, f))) && !status.Problems.Contains("P.UmmFilesMissing"))
                status.Problems.Add("P.UmmFilesMissing");
            if (FindPatchedAssemblies(game).Any()) status.Problems.Add("P.AssemblyPatched");

            status.State = status.Problems.Count == 0 ? UmmState.Installed : UmmState.Broken;
            return status;
        }

        public static Version DoorstopVersion(string proxyPath)
        {
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(proxyPath);
            return Version.TryParse(info.FileVersion ?? "", out var v) ? v : null;
        }

        static string DetectOtherLoader(Game game)
        {
            if (Directory.Exists(Path.Combine(game.Root, "MelonLoader")) || File.Exists(Path.Combine(game.Root, "version.dll")))
                return "MelonLoader";
            if (Directory.Exists(Path.Combine(game.Root, "BepInEx")))
                return "BepInEx";
            return null;
        }

        static bool IniTargetsUmm(string iniPath)
        {
            var m = Regex.Match(File.ReadAllText(iniPath), @"^\s*target_assembly\s*=\s*(.+?)\s*$", RegexOptions.Multiline);
            return m.Success && m.Groups[1].Value.Replace('/', '\\').EndsWith(@"UnityModManager\UnityModManager.dll", StringComparison.OrdinalIgnoreCase);
        }

        // 게임 업데이트로 dll이 교체되면 마커가 사라지므로, 마커가 있을 때만 '패치됨'으로 본다
        static IEnumerable<string> FindPatchedAssemblies(Game game) =>
            AssemblyPatchTargets.Select(n => Path.Combine(game.ManagedDir, n))
                .Where(p => File.Exists(p) && Files.ContainsAscii(p, StarterMarker));

        public static async Task<string> GetLatestVersionAsync(CancellationToken ct)
        {
            var feed = Json.Parse(await Http.GetStringAsync(VersionFeedUrl, ct).ConfigureAwait(false));
            var release = (Json.Get(feed, "Releases") as object[])?.FirstOrDefault();
            return Json.Str(release, "Version");
        }

        public static async Task InstallAsync(Game game, IProgress<double> progress, CancellationToken ct)
        {
            var temp = Files.NewTempDir();
            try
            {
                var zip = Path.Combine(temp, "umm.zip");
                Log.Info("UMM 다운로드 시작: " + DownloadUrl);
                await Http.DownloadAsync(DownloadUrl, zip, progress, ct).ConfigureAwait(false);
                var extracted = Path.Combine(temp, "x");
                ZipFile.ExtractToDirectory(zip, extracted);
                InstallFromExtracted(game, extracted);
            }
            finally
            {
                Files.TryDeleteTemp(temp);
            }
        }

        // 네트워크 없이 테스트할 수 있게 압축 해제 이후 단계를 분리
        public static void InstallFromExtracted(Game game, string extractedRoot)
        {
            var src = FindInstallerDir(extractedRoot)
                ?? throw new InvalidDataException("UMM 압축 파일에서 UnityModManager.dll을 찾지 못함: " + extractedRoot);
            if (File.Exists(Path.Combine(game.Root, "GameAssembly.dll")))
                throw new NotSupportedException("IL2CPP 빌드라 UMM을 쓸 수 없음");

            bool is64 = Pe.Is64Bit(Path.Combine(game.Root, Game.ExeName)) ?? true;
            var proxySource = Path.Combine(src, is64 ? "winhttp_x64.dll" : "winhttp_x86.dll");
            var missing = RequiredLibs.Select(f => Path.Combine(src, f)).Append(proxySource).Where(p => !File.Exists(p)).ToList();
            if (missing.Count > 0)
                throw new InvalidDataException("UMM 압축 파일 구성이 예상과 다름. 없는 파일: " + string.Join(", ", missing.Select(Path.GetFileName)));

            // Assembly 방식 흔적이 있으면 원본으로 되돌린다 (Doorstop과 함께 있으면 UMM이 두 번 로드됨)
            RestorePatchedAssemblies(game);

            Directory.CreateDirectory(game.UmmDir);
            foreach (var lib in RequiredLibs)
                File.Copy(Path.Combine(src, lib), Path.Combine(game.UmmDir, lib), true);
            // 공식 설치기와 동일: 게임에 이미 System.Xml.dll이 있으면 넣지 않는다
            var systemXml = Path.Combine(src, "System.Xml.dll");
            if (!File.Exists(Path.Combine(game.ManagedDir, "System.Xml.dll")) && File.Exists(systemXml))
                File.Copy(systemXml, Path.Combine(game.UmmDir, "System.Xml.dll"), true);

            WriteGameConfig(game, ReadGameInfo(src));
            File.Copy(proxySource, Path.Combine(game.Root, DoorstopDll), true);
            // 공식 설치기와 바이트 단위로 같게: CRLF, 마지막 줄바꿈 없음, BOM 없는 UTF-8
            File.WriteAllText(Path.Combine(game.Root, DoorstopIni),
                "[General]\r\nenabled = true\r\ntarget_assembly = " + TargetAssembly, new UTF8Encoding(false));
            Directory.CreateDirectory(game.ModsDir);
            Log.Info($"UMM 설치 완료: {game.Root} ({(is64 ? "x64" : "x86")})");
        }

        static string FindInstallerDir(string root)
        {
            if (File.Exists(Path.Combine(root, "UnityModManager.dll"))) return root;
            return Directory.GetDirectories(root).FirstOrDefault(d => File.Exists(Path.Combine(d, "UnityModManager.dll")));
        }

        static XElement ReadGameInfo(string installerDir)
        {
            var config = Path.Combine(installerDir, "UnityModManagerConfig.xml");
            try
            {
                var info = XDocument.Load(config).Root?.Elements("GameInfo")
                    .FirstOrDefault(e => (string)e.Attribute("Name") == GameName);
                if (info != null) return info;
                Log.Warn("UnityModManagerConfig.xml에 ADOFAI 항목이 없어 내장 값 사용");
            }
            catch (Exception ex) when (ex is IOException || ex is System.Xml.XmlException)
            {
                Log.Warn("UnityModManagerConfig.xml 읽기 실패, 내장 값 사용", ex);
            }
            return XElement.Parse(FallbackGameInfo);
        }

        // 게임 안 UMM이 읽는 Config.xml (공식 설치기의 GameInfo.ExportToGame과 같은 모양)
        static void WriteGameConfig(Game game, XElement gameInfo)
        {
            XNamespace xsd = "http://www.w3.org/2001/XMLSchema", xsi = "http://www.w3.org/2001/XMLSchema-instance";
            var root = new XElement("Config",
                new XAttribute(XNamespace.Xmlns + "xsd", xsd),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute("Name", GameName),
                gameInfo.Elements().Select(e => new XElement(e.Name.LocalName, e.Value)));
            Files.SaveXml(new XDocument(new XDeclaration("1.0", "utf-8", null), root), Path.Combine(game.UmmDir, "Config.xml"));
        }

        static void RestorePatchedAssemblies(Game game)
        {
            foreach (var patched in FindPatchedAssemblies(game).ToList())
            {
                var original = patched + ".original_";
                if (!File.Exists(original))
                    throw new InvalidOperationException("UMM이 수정한 게임 파일의 원본 백업이 없음: " + patched);
                File.Copy(original, patched, true);
                File.Delete(original);
                Log.Info("Assembly 방식 패치 원복: " + patched);
            }
        }

        // 공식 제거와 같은 범위: 프록시·설정·UMM dll만 치우고 Params.xml과 Mods는 남긴다 (재설치 시 그대로 복원)
        public static void Uninstall(Game game)
        {
            var ini = Path.Combine(game.Root, DoorstopIni);
            // 다른 로더(BepInEx 등)의 Doorstop이면 건드리지 않는다
            if (File.Exists(ini) && IniTargetsUmm(ini))
            {
                Files.Discard(Path.Combine(game.Root, DoorstopDll));
                Files.Discard(ini);
            }
            if (Directory.Exists(game.UmmDir))
            {
                foreach (var file in Directory.GetFiles(game.UmmDir, "*.dll").Append(Path.Combine(game.UmmDir, "UnityModManager.xml")).Append(Path.Combine(game.UmmDir, "Config.xml")))
                    if (File.Exists(file)) Files.Discard(file);
            }
            RestorePatchedAssemblies(game);
            Log.Info("UMM 제거 완료: " + game.Root);
        }
    }
}
