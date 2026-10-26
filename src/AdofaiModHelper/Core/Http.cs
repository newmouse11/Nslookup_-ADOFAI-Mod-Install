using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdofaiModHelper.Core
{
    // GitHub 비인증 API는 시간당 60회 한도
    public sealed class RateLimitException : Exception
    {
        public DateTime ResetAt { get; }
        public RateLimitException(DateTime resetAt) : base("GitHub API 요청 한도 초과") { ResetAt = resetAt; }
    }

    public static class Http
    {
        static readonly HttpClient Client = CreateClient();
        static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);

        static HttpClient CreateClient()
        {
            // 구형 윈도10의 .NET 기본값이 TLS1.0일 수 있어 GitHub/Dropbox 접속용으로 1.2를 명시
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
            {
                Timeout = TimeSpan.FromSeconds(30),
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AdofaiModHelper/" + AppInfo.Version);
            return client;
        }

        public static async Task<string> GetStringAsync(string url, CancellationToken ct)
        {
            using (var response = await Client.GetAsync(url, ct).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public static async Task DownloadAsync(string url, string destPath, IProgress<double> progress, CancellationToken ct)
        {
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                timeout.CancelAfter(DownloadTimeout);
                using (var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    var total = response.Content.Headers.ContentLength;
                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = File.Create(destPath))
                    {
                        var buffer = new byte[81920];
                        long done = 0;
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length, timeout.Token).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read, timeout.Token).ConfigureAwait(false);
                            done += read;
                            if (total > 0) progress?.Report((double)done / total.Value);
                        }
                    }
                }
            }
        }

        // 비인증 GitHub API는 시간당 60회: 모드 13개 × 앱 실행 횟수면 금방 닿으므로
        // 15분 안에 받은 응답은 다시 묻지 않고, 그 뒤엔 ETag 조건부 요청, 실패하면 마지막 캐시를 쓴다
        static readonly TimeSpan GitHubCacheFresh = TimeSpan.FromMinutes(15);

        public static async Task<string> GetGitHubJsonAsync(string url, CancellationToken ct)
        {
            Directory.CreateDirectory(AppPaths.CacheDir);
            var key = CacheKey(url);
            var bodyPath = Path.Combine(AppPaths.CacheDir, key + ".json");
            var etagPath = Path.Combine(AppPaths.CacheDir, key + ".etag");
            var cached = File.Exists(bodyPath) ? File.ReadAllText(bodyPath) : null;
            if (cached != null && DateTime.UtcNow - File.GetLastWriteTimeUtc(bodyPath) < GitHubCacheFresh) return cached;

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.ParseAdd("application/vnd.github+json");
            if (cached != null && File.Exists(etagPath)) request.Headers.TryAddWithoutValidation("If-None-Match", File.ReadAllText(etagPath));

            HttpResponseMessage response;
            try
            {
                response = await Client.SendAsync(request, ct).ConfigureAwait(false);
            }
            catch (Exception ex) when (cached != null && !ct.IsCancellationRequested
                                       && (ex is HttpRequestException || ex is TaskCanceledException))
            {
                // TaskCanceledException은 사용자가 취소한 게 아니면 타임아웃
                Log.Warn("GitHub 요청 실패, 캐시 사용: " + url, ex);
                return cached;
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NotModified && cached != null)
                {
                    File.SetLastWriteTimeUtc(bodyPath, DateTime.UtcNow); // 여전히 최신임을 확인했으니 15분 연장
                    return cached;
                }

                if ((response.StatusCode == HttpStatusCode.Forbidden || (int)response.StatusCode == 429)
                    && response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining) && remaining.FirstOrDefault() == "0")
                {
                    if (cached != null) return cached;
                    var reset = response.Headers.TryGetValues("X-RateLimit-Reset", out var r) && long.TryParse(r.FirstOrDefault(), out var unix)
                        ? DateTimeOffset.FromUnixTimeSeconds(unix).LocalDateTime
                        : DateTime.Now.AddHours(1);
                    throw new RateLimitException(reset);
                }

                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                File.WriteAllText(bodyPath, body);
                if (response.Headers.ETag != null) File.WriteAllText(etagPath, response.Headers.ETag.ToString());
                return body;
            }
        }

        static string CacheKey(string url)
        {
            using (var sha = SHA1.Create())
                return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(url)).Select(b => b.ToString("x2")));
        }
    }
}
