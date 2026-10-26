using System;
using System.IO;
using System.Xml.Serialization;

namespace AdofaiModHelper.Core
{
    public static class AppInfo
    {
        public static string Version { get; } = typeof(AppInfo).Assembly.GetName().Version.ToString(3);
    }

    public static class AppPaths
    {
        // 테스트에서 실제 사용자 폴더를 건드리지 않도록 바꿀 수 있게 열어둔다
        public static string DataDir { get; set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AdofaiModHelper");

        public static string CacheDir => Path.Combine(DataDir, "cache");
        public static string TempDir => Path.Combine(Path.GetTempPath(), "AdofaiModHelper");
    }

    // 사용자 설정. %LocalAppData%\AdofaiModHelper\settings.xml
    public sealed class Settings
    {
        public string GamePath { get; set; }
        public string Language { get; set; } = "ko";
        public bool WizardDone { get; set; }
        // 게임 업데이트 감지용: 마지막으로 확인한 Steam 빌드 번호
        public string LastBuildId { get; set; }

        static string FilePath => Path.Combine(AppPaths.DataDir, "settings.xml");

        public static Settings Load()
        {
            if (!File.Exists(FilePath)) return new Settings();
            try
            {
                using (var stream = File.OpenRead(FilePath))
                    return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is IOException)
            {
                // 설정이 깨졌으면 기본값으로 시작한다 (게임 파일과 무관하니 안전)
                Log.Warn("설정 파일을 읽지 못해 기본값으로 시작: " + FilePath, ex);
                return new Settings();
            }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.DataDir);
                using (var stream = File.Create(FilePath))
                    new XmlSerializer(typeof(Settings)).Serialize(stream, this);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Log.Error("설정 저장 실패: " + FilePath, ex);
            }
        }
    }
}
