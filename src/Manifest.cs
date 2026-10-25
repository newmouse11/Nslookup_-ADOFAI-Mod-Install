using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace OneClick
{
    internal sealed class ManifestItem
    {
        public string Name, Version;
        public int FileCount;
    }

    internal sealed class ManifestFile
    {
        public string Item, Hash, Path;
        public long Size;
    }

    // make-payload.ps1이 만든 manifest.txt를 읽는다. 탭 구분: meta/item/file 줄.
    internal sealed class Manifest
    {
        public string GameHash, BuiltAt;
        public readonly List<ManifestItem> Items = new List<ManifestItem>();
        public readonly List<ManifestFile> Files = new List<ManifestFile>();

        public static Manifest LoadEmbedded()
        {
            using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("manifest.txt"))
            using (var r = new StreamReader(s, Encoding.UTF8))
                return Parse(r.ReadToEnd());
        }

        public static Manifest Parse(string text)
        {
            var m = new Manifest();
            foreach (var line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var p = line.Split('\t');
                switch (p[0])
                {
                    case "meta":
                        if (p[1] == "gameHash") m.GameHash = p[2];
                        else if (p[1] == "builtAt") m.BuiltAt = p[2];
                        break;
                    case "item":
                        m.Items.Add(new ManifestItem { Name = p[1], Version = p[2], FileCount = int.Parse(p[3]) });
                        break;
                    case "file":
                        m.Files.Add(new ManifestFile { Item = p[1], Hash = p[2], Size = long.Parse(p[3]), Path = p[4] });
                        break;
                }
            }
            return m;
        }
    }
}
