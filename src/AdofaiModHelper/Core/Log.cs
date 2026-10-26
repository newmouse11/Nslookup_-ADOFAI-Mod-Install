using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace AdofaiModHelper.Core
{
    // 앱 로그. 문의할 때 '진단 정보 복사'에 마지막 부분이 첨부된다
    public static class Log
    {
        const long MaxBytes = 1024 * 1024;
        static readonly object Gate = new object();

        public static string FilePath => Path.Combine(AppPaths.DataDir, "log.txt");

        public static void Info(string message) => Write("INFO", message, null);
        public static void Warn(string message, Exception ex = null) => Write("WARN", message, ex);
        public static void Error(string message, Exception ex = null) => Write("ERROR", message, ex);

        static void Write(string level, string message, Exception ex)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            if (ex != null) line += Environment.NewLine + "  " + ex;
            lock (Gate)
            {
                try
                {
                    Directory.CreateDirectory(AppPaths.DataDir);
                    // 무한히 커지지 않게 1MB 넘으면 한 번 비우고 새로 쓴다
                    var file = new FileInfo(FilePath);
                    if (file.Exists && file.Length > MaxBytes) file.Delete();
                    File.AppendAllText(FilePath, line + Environment.NewLine);
                }
                catch (Exception writeError) when (writeError is IOException || writeError is UnauthorizedAccessException)
                {
                    // 로그 파일 자체를 못 쓰는 상황이라 디버거 출력으로만 남긴다
                    Debug.WriteLine("로그 기록 실패: " + writeError.Message + " / 원래 메시지: " + line);
                }
            }
        }

        public static string Tail(int lineCount)
        {
            lock (Gate)
            {
                if (!File.Exists(FilePath)) return "";
                var lines = File.ReadAllLines(FilePath);
                return string.Join(Environment.NewLine, lines.Skip(Math.Max(0, lines.Length - lineCount)));
            }
        }
    }
}
