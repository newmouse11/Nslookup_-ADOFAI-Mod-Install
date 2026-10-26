using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.VisualBasic.FileIO;

namespace AdofaiModHelper.Core
{
    // 모드마다 버전 표기가 제각각("v2.9.3", "1.0.0.45 beta45", "r202_final")이라 숫자 부분만 비교한다
    public static class Versions
    {
        static readonly Regex Numeric = new Regex(@"\d+(?:\.\d+)+|\d+");

        // 점이 들어간 첫 숫자열을 우선(태그 "r202_final"의 202 같은 빌드 번호 회피)
        public static string Extract(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var dotted = Regex.Match(text, @"\d+(?:\.\d+)+");
            if (dotted.Success) return dotted.Value;
            return null;
        }

        public static int Compare(string a, string b)
        {
            var x = Parts(a);
            var y = Parts(b);
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                int xi = i < x.Length ? x[i] : 0, yi = i < y.Length ? y[i] : 0;
                if (xi != yi) return xi.CompareTo(yi);
            }
            return 0;
        }

        static int[] Parts(string v)
        {
            var m = Numeric.Match(v ?? "");
            if (!m.Success) return new int[0];
            return m.Value.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        }
    }

    public static class Json
    {
        static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public static object Parse(string text) => Serializer.DeserializeObject(text.TrimStart('﻿'));

        // UMM은 Info.json 키를 대소문자 무시로 읽으므로 똑같이 맞춘다
        public static object Get(object obj, string key)
        {
            if (!(obj is Dictionary<string, object> dict)) return null;
            if (dict.TryGetValue(key, out var v)) return v;
            var pair = dict.FirstOrDefault(kv => string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase));
            return pair.Value;
        }

        public static string Str(object obj, string key) => Get(obj, key) as string;

        public static string[] StrArray(object obj, string key) =>
            (Get(obj, key) as object[])?.OfType<string>().ToArray() ?? new string[0];

        public static bool Bool(object obj, string key) => Get(obj, key) is bool b && b;
    }

    public static class Pe
    {
        // UMM 설치기와 같은 방식: PE 헤더의 Machine 값으로 32/64비트를 판별한다
        public static bool? Is64Bit(string path)
        {
            try
            {
                using (var r = new BinaryReader(File.OpenRead(path)))
                {
                    r.BaseStream.Seek(0x3C, SeekOrigin.Begin);
                    r.BaseStream.Seek(r.ReadInt32(), SeekOrigin.Begin);
                    if (r.ReadUInt32() != 0x00004550) return null; // "PE\0\0"
                    var machine = r.ReadUInt16();
                    if (machine == 0x8664 || machine == 0x0200) return true;
                    if (machine == 0x014C) return false;
                    return null;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException)
            {
                Log.Warn("실행 파일 비트 판별 실패: " + path, ex);
                return null;
            }
        }
    }

    public static class Files
    {
        // 지우는 건 전부 휴지통으로 (실수해도 되돌릴 수 있게). 테스트에서는 바로 삭제로 바꾼다
        public static Action<string> Discard { get; set; } = SendToRecycleBin;

        static void SendToRecycleBin(string path)
        {
            if (Directory.Exists(path))
                FileSystem.DeleteDirectory(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            else if (File.Exists(path))
                FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
        }

        public static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var dir in Directory.GetDirectories(source, "*", System.IO.SearchOption.AllDirectories))
                Directory.CreateDirectory(Path.Combine(dest, dir.Substring(source.Length).TrimStart('\\')));
            foreach (var file in Directory.GetFiles(source, "*", System.IO.SearchOption.AllDirectories))
                File.Copy(file, Path.Combine(dest, file.Substring(source.Length).TrimStart('\\')), true);
        }

        public static string NewTempDir()
        {
            var dir = Path.Combine(AppPaths.TempDir, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static void TryDeleteTemp(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
                else if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // 임시 파일이라 남아도 기능엔 영향 없음. 기록만 한다
                Log.Warn("임시 파일 정리 실패: " + path, ex);
            }
        }

        // UMM(XmlSerializer + StreamWriter)과 같은 형식: BOM 없는 UTF-8, 두 칸 들여쓰기, CRLF
        public static void SaveXml(System.Xml.Linq.XDocument doc, string path)
        {
            var settings = new System.Xml.XmlWriterSettings { Encoding = new System.Text.UTF8Encoding(false), Indent = true };
            using (var writer = System.Xml.XmlWriter.Create(path, settings)) doc.Save(writer);
        }

        public static bool ContainsAscii(string path, string marker)
        {
            var needle = System.Text.Encoding.ASCII.GetBytes(marker);
            var hay = File.ReadAllBytes(path);
            for (int i = 0; i <= hay.Length - needle.Length; i++)
            {
                int j = 0;
                while (j < needle.Length && hay[i + j] == needle[j]) j++;
                if (j == needle.Length) return true;
            }
            return false;
        }
    }
}
