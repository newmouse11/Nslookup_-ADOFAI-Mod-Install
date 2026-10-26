using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace AdofaiModInstaller
{
    internal sealed class DownloadException : Exception
    {
        public DownloadException(string message) : base(message) { }
    }

    internal static class Downloader
    {
        private static readonly HttpClient Http = Create();

        private static HttpClient Create()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 })
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AdofaiModInstaller/1.0");
            return client;
        }

        // 카탈로그에 적힌 크기를 넘으면 중단하고, 끝나면 SHA-256이 같아야 파일을 돌려준다.
        public static async Task<string> FetchAsync(Component c, string tempDir, Action<double> progress)
        {
            Directory.CreateDirectory(tempDir);
            var file = Path.Combine(tempDir, c.id + ".bin");
            try
            {
                using (var resp = await Http.GetAsync(c.url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (!resp.IsSuccessStatusCode)
                        throw new DownloadException(c.name + ": 다운로드 실패 (HTTP " + (int)resp.StatusCode + ")");
                    // 리다이렉트로 비보안 주소로 끌려가는 경우 거부
                    if (!Catalog.IsAllowedUrl(resp.RequestMessage.RequestUri))
                        throw new DownloadException(c.name + ": 허용되지 않는 주소로 이동했습니다 (" + resp.RequestMessage.RequestUri.Host + ")");
                    var declared = resp.Content.Headers.ContentLength;
                    if (declared.HasValue && declared.Value != c.size)
                        throw new DownloadException(c.name + ": 크기가 카탈로그와 다릅니다 (서버 " + declared.Value + " / 카탈로그 " + c.size + ")");

                    long total = 0;
                    using (var src = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var dst = File.Create(file))
                    {
                        var buf = new byte[81920];
                        int n;
                        while ((n = await src.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false)) > 0)
                        {
                            total += n;
                            if (total > c.size) throw new DownloadException(c.name + ": 카탈로그보다 큰 파일입니다.");
                            dst.Write(buf, 0, n);
                            if (progress != null) progress((double)total / c.size);
                        }
                    }
                    if (total != c.size) throw new DownloadException(c.name + ": 파일이 완전히 받아지지 않았습니다.");
                }

                string actual;
                using (var sha = SHA256.Create())
                using (var s = File.OpenRead(file))
                    actual = BitConverter.ToString(sha.ComputeHash(s)).Replace("-", "");
                if (!string.Equals(actual, c.sha256, StringComparison.OrdinalIgnoreCase))
                    throw new DownloadException(c.name + ": SHA-256이 카탈로그와 다릅니다. 배포처 파일이 바뀌었을 수 있어 설치하지 않습니다.");
                return file;
            }
            catch (HttpRequestException ex)
            {
                throw new DownloadException(c.name + ": 네트워크 오류 - " + ex.Message);
            }
            catch (TaskCanceledException)
            {
                throw new DownloadException(c.name + ": 시간이 초과되었습니다.");
            }
            catch (DownloadException)
            {
                File.Delete(file);
                throw;
            }
        }
    }
}
