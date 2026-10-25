using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace AdofaiModInstaller
{
    // catalog.json의 한 항목. 모든 다운로드는 여기 적힌 url + sha256 + size와 일치해야만 설치된다.
    public sealed class Component
    {
        public string id, name, version, author, license, homepage, url, sha256, kind, dir, path, note;
        public long size;
        public bool required, defaultOn;
        public string[] requires = new string[0];
        public string[] conflicts = new string[0];
    }

    public sealed class CatalogDoc
    {
        public int schema;
        public List<Component> components = new List<Component>();
    }

    internal static class Catalog
    {
        // 설치 종류: umm(UnityModManager), doorstop(winhttp.dll 프록시), mod(Mods\<dir>), file(단일 파일)
        public static readonly string[] Kinds = { "umm", "doorstop", "mod", "file" };
        public const long MaxSize = 200L * 1024 * 1024;
        private static readonly Regex SafeName = new Regex(@"^[A-Za-z0-9._-]+$");
        private static readonly Regex Hex64 = new Regex(@"^[A-Fa-f0-9]{64}$");

        public static CatalogDoc Load(string file)
        {
            string json;
            if (file != null)
                json = File.ReadAllText(file, Encoding.UTF8);
            else
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("catalog.json"))
                using (var r = new StreamReader(s, Encoding.UTF8))
                    json = r.ReadToEnd();
            var doc = new JavaScriptSerializer().Deserialize<CatalogDoc>(json);
            Validate(doc);
            return doc;
        }

        // https만 허용한다. 개발·테스트용으로 loopback(127.0.0.1/localhost)의 http만 예외.
        public static bool IsAllowedUrl(Uri u)
        {
            return u != null && (u.Scheme == Uri.UriSchemeHttps || (u.Scheme == Uri.UriSchemeHttp && u.IsLoopback));
        }

        public static void Validate(CatalogDoc doc)
        {
            if (doc == null || doc.schema != 1 || doc.components == null || doc.components.Count == 0)
                throw new InvalidDataException("카탈로그 형식이 올바르지 않습니다 (schema 1 필요).");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in doc.components)
            {
                if (c == null || string.IsNullOrEmpty(c.id) || !SafeName.IsMatch(c.id))
                    throw new InvalidDataException("잘못된 id: " + (c == null ? "(null)" : c.id));
                if (!ids.Add(c.id)) throw new InvalidDataException("중복된 id: " + c.id);
                if (string.IsNullOrWhiteSpace(c.name)) throw new InvalidDataException(c.id + ": name이 없습니다.");
                if (!Kinds.Contains(c.kind)) throw new InvalidDataException(c.id + ": 알 수 없는 kind '" + c.kind + "'");
                Uri uri;
                if (!Uri.TryCreate(c.url, UriKind.Absolute, out uri) || !IsAllowedUrl(uri))
                    throw new InvalidDataException(c.id + ": url은 https여야 합니다.");
                if (string.IsNullOrEmpty(c.sha256) || !Hex64.IsMatch(c.sha256))
                    throw new InvalidDataException(c.id + ": sha256은 64자리 16진수여야 합니다.");
                if (c.size <= 0 || c.size > MaxSize)
                    throw new InvalidDataException(c.id + ": size가 범위를 벗어났습니다.");
                if (c.kind == "mod" && (string.IsNullOrEmpty(c.dir) || !SafeName.IsMatch(c.dir)))
                    throw new InvalidDataException(c.id + ": dir은 폴더 이름 하나여야 합니다.");
                if (c.kind == "file")
                {
                    if (string.IsNullOrEmpty(c.path)) throw new InvalidDataException(c.id + ": path가 없습니다.");
                    Store.SafeCombine(@"C:\game", c.path); // 경로 이탈 검사만 재사용
                }
            }
            foreach (var c in doc.components)
                foreach (var r in c.requires.Concat(c.conflicts))
                    if (!ids.Contains(r)) throw new InvalidDataException(c.id + ": 존재하지 않는 id 참조 '" + r + "'");
        }

        // 선택한 id에 필수 항목과 의존 항목을 더하고 충돌을 검사한다. 결과는 카탈로그 순서.
        public static List<Component> Resolve(CatalogDoc doc, IEnumerable<string> ids)
        {
            var byId = doc.components.ToDictionary(c => c.id, StringComparer.OrdinalIgnoreCase);
            var chosen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ids)
            {
                if (!byId.ContainsKey(id)) throw new InvalidDataException("알 수 없는 구성요소: " + id);
                chosen.Add(byId[id].id);
            }
            foreach (var c in doc.components.Where(c => c.required)) chosen.Add(c.id);
            bool changed;
            do
            {
                changed = false;
                foreach (var c in doc.components.Where(c => chosen.Contains(c.id)).ToList())
                    foreach (var r in c.requires)
                        if (chosen.Add(byId[r].id)) changed = true;
            } while (changed);
            foreach (var c in doc.components.Where(c => chosen.Contains(c.id)))
                foreach (var x in c.conflicts)
                    if (chosen.Contains(x)) throw new InvalidDataException("함께 설치할 수 없습니다: " + c.name + " / " + byId[x].name);
            return doc.components.Where(c => chosen.Contains(c.id)).ToList();
        }
    }
}
