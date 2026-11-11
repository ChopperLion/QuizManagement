using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using QuizManagement.Properties;

namespace QuizManagement.BUS
{
    public static class ExamLinkShareService
    {
        private static readonly object TunnelSync = new object();
        private static Process tunnelProcess;
        private static string activeTunnelUrl;

        public static string GetConfiguredPublicBaseUrl()
        {
            string configured = NormalizeBaseUrl(Settings.Default.CloudflarePublicBaseUrl);

            if (IsTryCloudflareUrl(configured))
            {
                string live = TryReadCloudflareUrlFromRunningProcess();
                if (!string.IsNullOrWhiteSpace(live))
                    return NormalizeBaseUrl(live);

                ClearConfiguredPublicBaseUrl();
                return "";
            }

            return configured;
        }

        public static void SaveConfiguredPublicBaseUrl(string baseUrl)
        {
            Settings.Default.CloudflarePublicBaseUrl = NormalizeBaseUrl(baseUrl);
            Settings.Default.Save();
        }

        public static void ClearConfiguredPublicBaseUrl()
        {
            Settings.Default.CloudflarePublicBaseUrl = "";
            Settings.Default.Save();
        }

        public static string BuildLanExamUrl(string roomCode)
        {
            return StudentExamWebServer.BuildExamUrl(roomCode);
        }

        public static string BuildPublicExamUrl(string roomCode)
        {
            string baseUrl = GetConfiguredPublicBaseUrl();
            return string.IsNullOrWhiteSpace(baseUrl) ? "" : baseUrl + "/exam/" + Uri.EscapeDataString((roomCode ?? "").Trim());
        }

        public static bool IsTemporaryPublicBaseUrl(string baseUrl)
        {
            return IsTryCloudflareUrl(NormalizeBaseUrl(baseUrl));
        }

        public static string BuildDurationWarning(string baseUrl, int durationMinutes)
        {
            if (!IsTemporaryPublicBaseUrl(baseUrl)) return "";

            if (durationMinutes >= 90)
                return "Đây là link Cloudflare tạm thời. Với bài dài khoảng 90 phút trở lên, nên ưu tiên domain hoặc tunnel cố định để tránh rớt link giữa chừng.";
            if (durationMinutes >= 45)
                return "Đây là link Cloudflare tạm thời. Với bài khá dài, bạn không nên tạo tunnel mới trong lúc sinh viên đang làm bài.";

            return "Link Internet này là tunnel tạm thời. Hạn chế đổi link trong khi đang thi.";
        }

        public static string NormalizeBaseUrl(string baseUrl)
        {
            string value = (baseUrl ?? "").Trim();
            if (string.IsNullOrWhiteSpace(value)) return "";

            if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                value = "https://" + value;
            }

            return value.TrimEnd('/');
        }

        public static bool IsTryCloudflareUrl(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf("trycloudflare.com", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryStartCloudflareTunnel(out string publicBaseUrl, out string message)
        {
            publicBaseUrl = "";
            message = "";

            lock (TunnelSync)
            {
                string existing = TryReadCloudflareUrlFromRunningProcess();
                if (!string.IsNullOrWhiteSpace(existing))
                {
                    publicBaseUrl = NormalizeBaseUrl(existing);
                    SaveConfiguredPublicBaseUrl(publicBaseUrl);
                    message = "Đã dùng tunnel Cloudflare đang chạy sẵn.";
                    return true;
                }

                try
                {
                    string logPath = Path.Combine(Path.GetTempPath(), "quizmanagement-cloudflared.log");
                    try
                    {
                        if (File.Exists(logPath)) File.Delete(logPath);
                    }
                    catch { }

                    var psi = new ProcessStartInfo
                    {
                        FileName = "cloudflared",
                        Arguments = "tunnel --url http://localhost:" + StudentExamWebServer.GetCurrentPort() + " --no-autoupdate --logfile \"" + logPath + "\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = new Process { StartInfo = psi };
                    process.Start();

                    string foundUrl = WaitForTryCloudflareUrl(process, logPath, 10000);
                    if (string.IsNullOrWhiteSpace(foundUrl))
                    {
                        try
                        {
                            if (!process.HasExited) process.Kill();
                        }
                        catch { }
                        message = "Đã chạy cloudflared nhưng chưa lấy được link công khai tự động. Bạn vẫn có thể chạy cloudflared thủ công rồi dán link vào ô chia sẻ.";
                        return false;
                    }

                    try
                    {
                        if (tunnelProcess != null && !ReferenceEquals(tunnelProcess, process) && !tunnelProcess.HasExited)
                            tunnelProcess.Kill();
                    }
                    catch { }

                    tunnelProcess = process;
                    activeTunnelUrl = foundUrl;
                    publicBaseUrl = NormalizeBaseUrl(foundUrl);
                    SaveConfiguredPublicBaseUrl(publicBaseUrl);
                    message = "Đã tạo Cloudflare Tunnel tự động.";
                    return true;
                }
                catch (Exception ex)
                {
                    message = "Không thể khởi động cloudflared. Hãy chắc rằng cloudflared đã được cài và có trong PATH.\n" + ex.Message;
                    return false;
                }
            }
        }

        private static string TryReadCloudflareUrlFromRunningProcess()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(activeTunnelUrl) && tunnelProcess != null && !tunnelProcess.HasExited)
                    return activeTunnelUrl;

                if (tunnelProcess == null || tunnelProcess.HasExited)
                {
                    activeTunnelUrl = "";
                    return "";
                }

                return activeTunnelUrl ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string WaitForTryCloudflareUrl(Process process, string logPath, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<string>();
            DataReceivedEventHandler onData = (sender, args) =>
            {
                string found = ExtractTryCloudflareUrl(args.Data);
                if (!string.IsNullOrWhiteSpace(found))
                {
                    activeTunnelUrl = found;
                    tcs.TrySetResult(found);
                }
            };

            process.OutputDataReceived += onData;
            process.ErrorDataReceived += onData;

            try
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
                while (DateTime.Now < deadline)
                {
                    if (tcs.Task.Wait(250))
                        return tcs.Task.Result;

                    string foundFromLog = TryReadUrlFromLog(logPath);
                    if (!string.IsNullOrWhiteSpace(foundFromLog))
                    {
                        activeTunnelUrl = foundFromLog;
                        return foundFromLog;
                    }

                    if (process.HasExited) break;
                }
                return "";
            }
            finally
            {
                process.OutputDataReceived -= onData;
                process.ErrorDataReceived -= onData;
            }
        }

        private static string ExtractTryCloudflareUrl(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            Match m = Regex.Match(text, @"https://[a-zA-Z0-9\-\.]+trycloudflare\.com");
            return m.Success ? m.Value : "";
        }

        private static string TryReadUrlFromLog(string logPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath)) return "";
                return ExtractTryCloudflareUrl(File.ReadAllText(logPath));
            }
            catch
            {
                return "";
            }
        }
    }
}
