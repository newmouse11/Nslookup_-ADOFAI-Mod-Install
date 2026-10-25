using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace AdofaiModInstaller
{
    // /selftest: 네트워크 없이(로컬 서버만) 보안·롤백 로직을 검사한다. CI에서 실행한다.
    internal static class SelfTest
    {
        private static readonly List<string> Lines = new List<string>();
        private static int _failed;

        private static string _log;
        private static void Check(string name, bool ok)
        {
            var line = (ok ? "PASS " : "FAIL ") + name;
            Lines.Add(line); if (!ok) _failed++;
            File.AppendAllText(_log + ".progress", line + "\r\n", new UTF8Encoding(false)); // 멈춰도 마지막 통과 지점을 알 수 있다
        }

        private static bool Throws<T>(Action a) where T : Exception
        {
            try { a(); return false; } catch (T) { return true; }
        }

        public static int Run(string logPath)
        {
            _log = logPath; File.WriteAllText(_log + ".progress", "", new UTF8Encoding(false));
            var work = Path.Combine(Path.GetTempPath(), "AdofaiModInstaller-selftest", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(work);
            try
            {
                TestCatalog();
                TestPaths(work);
                TestPlanner(work);
                TestInstallRollbackUninstall(work);
                TestDownload(work);
                TestOverlay(work);
                TestPresentAndPartial(work);
            }
            finally
            {
                try { Directory.Delete(work, true); }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { /* 임시 폴더 정리 실패는 무시 */ }
            }
            Lines.Add(_failed == 0 ? "ALL PASSED" : _failed + " FAILED");
            File.WriteAllText(logPath, string.Join("\r\n", Lines), new UTF8Encoding(false));
            return _failed == 0 ? 0 : 1;
        }

        private static Component Comp(string id, string kind = "mod", string url = "https://example.com/a.zip", string sha = null)
        {
            return new Component { id = id, name = id, version = "1", kind = kind, dir = id, url = url, sha256 = sha ?? new string('a', 64), size = 10 };
        }

        private static CatalogDoc Doc(params Component[] cs) { return new CatalogDoc { schema = 1, components = cs.ToList() }; }

        private static void TestCatalog()
        {
            Check("catalog: 기본 카탈로그 로드·검증", !Throws<InvalidDataException>(() => Catalog.Load(null)));
            Check("catalog: http(비 loopback) 거부", Throws<InvalidDataException>(() => Catalog.Validate(Doc(Comp("a", url: "http://example.com/a.zip")))));
            Check("catalog: loopback http 허용", !Throws<InvalidDataException>(() => Catalog.Validate(Doc(Comp("a", url: "http://127.0.0.1:1/a.zip")))));
            Check("catalog: 잘못된 sha256 거부", Throws<InvalidDataException>(() => Catalog.Validate(Doc(Comp("a", sha: "zz")))));
            Check("catalog: dir 경로 이탈 거부", Throws<InvalidDataException>(() => { var c = Comp("a"); c.dir = "..\\x"; Catalog.Validate(Doc(c)); }));
            Check("catalog: 중복 id 거부", Throws<InvalidDataException>(() => Catalog.Validate(Doc(Comp("a"), Comp("a")))));
            Check("catalog: 없는 id 참조 거부", Throws<InvalidDataException>(() => { var c = Comp("a"); c.requires = new[] { "nope" }; Catalog.Validate(Doc(c)); }));
            var req = Comp("lib"); var app = Comp("app"); app.requires = new[] { "lib" }; var a2 = Comp("a2"); var a3 = Comp("a3"); a2.conflicts = new[] { "a3" }; a3.conflicts = new[] { "a2" };
            var d = Doc(req, app, a2, a3);
            Check("resolve: 의존 항목 자동 추가", Catalog.Resolve(d, new[] { "app" }).Select(c => c.id).SequenceEqual(new[] { "lib", "app" }));
            Check("resolve: 충돌 거부", Throws<InvalidDataException>(() => Catalog.Resolve(d, new[] { "a2", "a3" })));
        }

        private static void TestPaths(string work)
        {
            var root = Path.Combine(work, "game"); Directory.CreateDirectory(root);
            Check("path: 정상 상대경로 허용", Store.SafeCombine(root, "Mods\\X\\a.dll").StartsWith(root));
            foreach (var bad in new[] { "..\\evil.txt", "Mods\\..\\..\\evil.txt", "C:\\evil.txt", "\\evil.txt", "Mods\\a.txt:stream", "/etc/x" })
                Check("path: 거부 '" + bad + "'", Throws<InvalidDataException>(() => Store.SafeCombine(root, bad)));
        }

        private static string MakeZip(string path, params KeyValuePair<string, string>[] entries)
        {
            using (var fs = File.Create(path))
            using (var z = new ZipArchive(fs, ZipArchiveMode.Create))
                foreach (var e in entries)
                    using (var w = new StreamWriter(z.CreateEntry(e.Key).Open(), new UTF8Encoding(false))) w.Write(e.Value);
            return path;
        }

        private static KeyValuePair<string, string> E(string k, string v) { return new KeyValuePair<string, string>(k, v); }

        private static void TestPlanner(string work)
        {
            var game = Path.Combine(work, "pg"); Directory.CreateDirectory(game);
            var flat = MakeZip(Path.Combine(work, "flat.zip"), E("Info.json", "{}"), E("a.dll", "1"));
            var nested = MakeZip(Path.Combine(work, "nested.zip"), E("My/Info.json", "{}"), E("My/sub/b.dll", "2"));
            var none = MakeZip(Path.Combine(work, "none.zip"), E("readme.txt", "x"));
            var slip = MakeZip(Path.Combine(work, "slip.zip"), E("Info.json", "{}"), E("../../evil.txt", "x"));
            var c = Comp("m");
            Check("plan: 루트에 Info.json이면 그대로", Planner.Plan(c, flat, game).Select(f => f.Rel).OrderBy(x => x).SequenceEqual(new[] { "Mods\\m\\a.dll", "Mods\\m\\Info.json" }));
            Check("plan: 최상위 폴더는 벗겨서 풀기", Planner.Plan(c, nested, game).Select(f => f.Rel).OrderBy(x => x).SequenceEqual(new[] { "Mods\\m\\Info.json", "Mods\\m\\sub\\b.dll" }));
            Check("plan: Info.json 없으면 거부", Throws<InvalidDataException>(() => Planner.Plan(c, none, game)));
            Check("plan: Zip Slip 항목 거부", Throws<InvalidDataException>(() => Planner.Plan(c, slip, game)));
        }

        private static void TestInstallRollbackUninstall(string work)
        {
            var game = Path.Combine(work, "game2"); Directory.CreateDirectory(game);
            var existing = Path.Combine(game, "Mods", "m", "a.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(existing)); File.WriteAllText(existing, "ORIGINAL");
            var c = Comp("m");
            Action<string, string> st = (id, s) => { }; Action<double> pr = p => { };

            var plan = new List<KeyValuePair<Component, List<PlannedFile>>> { new KeyValuePair<Component, List<PlannedFile>>(c, new List<PlannedFile> {
                new PlannedFile { Rel = "Mods\\m\\a.dll", Bytes = Encoding.UTF8.GetBytes("NEW") },
                new PlannedFile { Rel = "Mods\\m\\Info.json", Bytes = Encoding.UTF8.GetBytes("{}") } }) };
            var report = Installer.Apply(game, plan, st, pr);
            Check("apply: 기존 파일 덮어쓰고 백업 1개", report.Written == 2 && report.BackedUp == 1 && File.ReadAllText(existing) == "NEW");
            Check("apply: 영수증 기록", Store.LoadReceipt(game).components.Single().files.Count == 2);

            // 제거: 설치 후 사용자가 수정한 파일은 남기고, 원본은 복원
            File.WriteAllText(Path.Combine(game, "Mods", "m", "Info.json"), "USER-EDITED");
            var msg = Installer.Uninstall(game);
            Check("uninstall: 원본 복원", File.ReadAllText(existing) == "ORIGINAL");
            Check("uninstall: 수정된 파일은 남김", File.ReadAllText(Path.Combine(game, "Mods", "m", "Info.json")) == "USER-EDITED" && msg.Contains("수정되어 남김"));
            Check("uninstall: 영수증 삭제", !File.Exists(Store.ReceiptPath(game)));

            // 롤백: 두 번째 파일 경로를 폴더가 막고 있어 쓰기가 실패하면 첫 파일도 되돌려진다
            var game3 = Path.Combine(work, "game3"); Directory.CreateDirectory(game3);
            var f1 = Path.Combine(game3, "Mods", "m", "one.txt"); Directory.CreateDirectory(Path.GetDirectoryName(f1)); File.WriteAllText(f1, "KEEP");
            Directory.CreateDirectory(Path.Combine(game3, "Mods", "m", "two.txt")); // 파일이 들어갈 자리에 폴더
            var plan3 = new List<KeyValuePair<Component, List<PlannedFile>>> { new KeyValuePair<Component, List<PlannedFile>>(c, new List<PlannedFile> {
                new PlannedFile { Rel = "Mods\\m\\one.txt", Bytes = Encoding.UTF8.GetBytes("CHANGED") },
                new PlannedFile { Rel = "Mods\\m\\two.txt", Bytes = Encoding.UTF8.GetBytes("x") } }) };
            Check("rollback: 실패 시 예외", Throws<InvalidOperationException>(() => Installer.Apply(game3, plan3, st, pr)));
            Check("rollback: 첫 파일 원상복구", File.ReadAllText(f1) == "KEEP" && !File.Exists(Store.ReceiptPath(game3)));
        }


        // 이미 설치된 필수·의존 항목은 다시 받지 않고(exact 제외), 구성요소 하나만 부분 제거할 수 있다
        private static void TestPresentAndPartial(string work)
        {
            var lib = Comp("lib"); var req = Comp("core"); req.required = true;
            var app = Comp("app"); app.requires = new[] { "lib" };
            var doc = Doc(req, lib, app);
            Func<Component, bool> libPresent = c => c.id == "lib" || c.id == "core";
            Check("resolve: 설치된 필수·의존 항목은 제외", Catalog.Resolve(doc, new[] { "app" }, libPresent).Select(c => c.id).SequenceEqual(new[] { "app" }));
            Check("resolve: 설치되어 있지 않으면 필수·의존 항목 추가", Catalog.Resolve(doc, new[] { "app" }, c => false).Select(c => c.id).SequenceEqual(new[] { "core", "lib", "app" }));
            app.exact = true;
            Check("resolve: exact는 설치되어 있어도 의존 항목을 함께 설치", Catalog.Resolve(doc, new[] { "app" }, libPresent).Select(c => c.id).SequenceEqual(new[] { "lib", "app" }));
            Check("catalog: homepage가 https가 아니면 거부", Throws<InvalidDataException>(() => { var c = Comp("h"); c.homepage = "file:///C:/Windows/System32/calc.exe"; Catalog.Validate(Doc(c)); }));

            var game = Path.Combine(work, "game-partial"); Directory.CreateDirectory(game);
            var plan = new List<KeyValuePair<Component, List<PlannedFile>>> {
                new KeyValuePair<Component, List<PlannedFile>>(Comp("one"), new List<PlannedFile> { new PlannedFile { Rel = "Mods\\one\\a.txt", Bytes = Encoding.UTF8.GetBytes("1") } }),
                new KeyValuePair<Component, List<PlannedFile>>(Comp("two"), new List<PlannedFile> { new PlannedFile { Rel = "Mods\\two\\b.txt", Bytes = Encoding.UTF8.GetBytes("2") } }) };
            Installer.Apply(game, plan, (id, s) => { }, p => { });
            Installer.Uninstall(game, new[] { "one" });
            Check("partial uninstall: 고른 것만 제거", !File.Exists(Path.Combine(game, "Mods", "one", "a.txt")) && File.Exists(Path.Combine(game, "Mods", "two", "b.txt")));
            Check("partial uninstall: 나머지 기록 유지", Store.LoadReceipt(game).components.Select(c => c.id).SequenceEqual(new[] { "two" }));
            Installer.Uninstall(game, new[] { "two" });
            Check("partial uninstall: 모두 제거하면 영수증 삭제", !File.Exists(Store.ReceiptPath(game)));
        }

        // 뒤 구성요소가 같은 경로를 쓰면 앞 구성요소 몫은 빠지고, 제거하면 파일이 남지 않는다
        private static void TestOverlay(string work)
        {
            var game = Path.Combine(work, "game-overlay"); Directory.CreateDirectory(game);
            var basePack = Comp("base"); var patch = Comp("patch", "file");
            var plan = new List<KeyValuePair<Component, List<PlannedFile>>> {
                new KeyValuePair<Component, List<PlannedFile>>(basePack, new List<PlannedFile> {
                    new PlannedFile { Rel = "Mods\\base\\a.dll", Bytes = Encoding.UTF8.GetBytes("OFFICIAL") },
                    new PlannedFile { Rel = "Mods\\base\\Info.json", Bytes = Encoding.UTF8.GetBytes("{}") } }),
                new KeyValuePair<Component, List<PlannedFile>>(patch, new List<PlannedFile> {
                    new PlannedFile { Rel = "Mods\\BASE\\A.dll", Bytes = Encoding.UTF8.GetBytes("PATCHED") } }) };
            Installer.ResolveOverlaps(plan);
            Check("overlay: 앞 구성요소의 같은 경로 항목 제거(대소문자 무시)", plan[0].Value.Count == 1 && plan[1].Value.Count == 1);
            var report = Installer.Apply(game, plan, (id, s) => { }, p => { });
            Check("overlay: 패치 파일이 최종 내용", File.ReadAllText(Path.Combine(game, "Mods", "base", "a.dll")) == "PATCHED" && report.BackedUp == 0);
            Installer.Uninstall(game);
            Check("overlay: 제거하면 파일·폴더가 남지 않음", !Directory.Exists(Path.Combine(game, "Mods")));
        }

        private static void TestDownload(string work)
        {
            var good = Encoding.UTF8.GetBytes("hello-installer");
            string sha;
            using (var h = System.Security.Cryptography.SHA256.Create()) sha = BitConverter.ToString(h.ComputeHash(good)).Replace("-", "");
            var port = 40000 + new Random(Environment.TickCount).Next(2000);
            using (var listener = new HttpListener())
            {
                listener.Prefixes.Add("http://127.0.0.1:" + port + "/");
                listener.Start();
                var serve = Task.Run(() =>
                {
                    while (listener.IsListening)
                    {
                        HttpListenerContext ctx;
                        try { ctx = listener.GetContext(); } catch (HttpListenerException) { return; } catch (ObjectDisposedException) { return; }
                        var body = ctx.Request.Url.AbsolutePath == "/tampered" ? Encoding.UTF8.GetBytes("hello-installer!") : good;
                        ctx.Response.ContentLength64 = body.Length;
                        ctx.Response.OutputStream.Write(body, 0, body.Length);
                        ctx.Response.Close();
                    }
                });
                var baseUrl = "http://127.0.0.1:" + port;
                var dir = Path.Combine(work, "dl");
                var ok = new Component { id = "ok", name = "ok", url = baseUrl + "/good", sha256 = sha, size = good.Length };
                Check("download: 해시 일치 시 성공", File.ReadAllBytes(Downloader.FetchAsync(ok, dir, p => { }).GetAwaiter().GetResult()).Length == good.Length);
                var bad = new Component { id = "bad", name = "bad", url = baseUrl + "/good", sha256 = new string('0', 64), size = good.Length };
                Check("download: 해시 불일치 거부", Throws<DownloadException>(() => Downloader.FetchAsync(bad, dir, p => { }).GetAwaiter().GetResult()) && !File.Exists(Path.Combine(dir, "bad.bin")));
                var wrongSize = new Component { id = "ws", name = "ws", url = baseUrl + "/tampered", sha256 = sha, size = good.Length };
                Check("download: 크기 불일치 거부", Throws<DownloadException>(() => Downloader.FetchAsync(wrongSize, dir, p => { }).GetAwaiter().GetResult()));
                listener.Stop();
            }
        }
    }
}
