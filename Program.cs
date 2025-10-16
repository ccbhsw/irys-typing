using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Nethereum.Util;

namespace Irys_Spritetype
{
    internal class Program
    {
        public static readonly object _lock = new();
        public static List<AccountInfo> AccountInfoList = new();
        public static int ScriptRunCount = 5;
        public static int RunMode = 1;

        static async Task Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            LoadPrivateKeyAndProxyAndLog();

            Console.Write("请输入每个账号执行多少次脚本：");
            var inputCount = Console.ReadLine();
            if (int.TryParse(inputCount, out int count) && count > 0)
                ScriptRunCount = count;

            Console.Write("请选择运行模式（1.安全模式 2.极速模式）：");
            var inputMode = Console.ReadLine();
            if (int.TryParse(inputMode, out int mode) && (mode == 1 || mode == 2))
                RunMode = mode;

            while (true)
            {
                foreach (var account in AccountInfoList)
                {
                    if (DateTime.Now >= account.NextExecutionTime)
                    {
                        try
                        {
                            await Script(account);
                        }
                        catch (Exception ex)
                        {
                            ShowMsg($"执行异常: {ex.Message}", 3);
                            account.NextExecutionTime = DateTime.Now.AddSeconds(10);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }

        public static async Task<string> Spritetype(AccountInfo accountInfo)
        {
            HttpClientHandler httpClientHandler = new();
            if (accountInfo.Proxy is not null)
                httpClientHandler.Proxy = accountInfo.Proxy;

            HttpClient client = new(httpClientHandler);
            HttpRequestMessage request = new(HttpMethod.Post, "https://spritetype.irys.xyz/api/submit-result");

            request.Headers.Add("accept", "*/*");
            request.Headers.Add("accept-language", "zh-CN,zh;q=0.9,zh-TW;q=0.8,ja;q=0.7,en;q=0.6");
            request.Headers.Add("origin", "https://spritetype.irys.xyz");
            request.Headers.Add("priority", "u=1, i");
            request.Headers.Add("referer", "https://spritetype.irys.xyz/");
            request.Headers.Add("sec-ch-ua", "\"Not)A;Brand\";v=\"8\", \"Chromium\";v=\"138\", \"Google Chrome\";v=\"138\"");
            request.Headers.Add("sec-ch-ua-mobile", "?0");
            request.Headers.Add("sec-ch-ua-platform", "\"Windows\"");
            request.Headers.Add("sec-fetch-dest", "empty");
            request.Headers.Add("sec-fetch-mode", "cors");
            request.Headers.Add("sec-fetch-site", "same-origin");
            request.Headers.Add("user-agent", accountInfo.UserAgent);

            var rand = new Random();
            int wpm = rand.Next(70, 81);
            int time = 15;
            int totalChars = wpm * 5 * time / 60;
            int incorrectChars = rand.Next(0, Math.Max(1, totalChars / 20));
            int correctChars = totalChars - incorrectChars;
            int accuracy = totalChars == 0 ? 100 : (int)Math.Round(100.0 * correctChars / totalChars);
            int[] progressData = new int[] { };

            string antiCheatHash = ComputeAntiCheatHash(accountInfo.Address, wpm, accuracy, time, correctChars, incorrectChars);

            var payload = new
            {
                walletAddress = accountInfo.Address,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                gameStats = new
                {
                    accuracy,
                    correctChars,
                    incorrectChars,
                    progressData,
                    time,
                    wpm
                },
                antiCheatHash
            };
            var payloadJson = JsonConvert.SerializeObject(payload);
            request.Content = new StringContent(payloadJson);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response;
            try
            {
                response = await client.SendAsync(request);
            }
            catch (Exception ex)
            {
                ShowMsg($"提交失败: {ex.Message}", 3);
                return "提交失败";
            }

            string responseBody = await response.Content.ReadAsStringAsync();

            // 检查是否触发限制
            if (response.StatusCode == HttpStatusCode.BadRequest &&
                (responseBody.Contains("Hourly submission limit reached") || responseBody.Contains("Please wait")))
            {
                int waitMinutes = 70; // 默认 70 分钟
                var match = System.Text.RegularExpressions.Regex.Match(responseBody, @"Please wait (\d+) seconds");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int sec))
                {
                    waitMinutes = (int)Math.Ceiling(sec / 60.0);
                }
                accountInfo.NextExecutionTime = DateTime.Now.AddMinutes(waitMinutes + 10); // +10分钟缓冲
                ShowMsg($"⚠ 接口限制，账号 {accountInfo.Index} 暂停 {waitMinutes + 10} 分钟后重试", 2);
                return "一小时限制";
            }

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                ShowMsg($"异常信息: {ex.Message}", 2);
                return "提交失败: " + responseBody;
            }

            var json = System.Text.Json.JsonDocument.Parse(responseBody);
            json.RootElement.TryGetProperty("message", out var nonceElement);
            string? ret = nonceElement.GetString();
            if (!string.IsNullOrEmpty(ret))
                return ret;

            throw new Exception("Spritetype失败");
        }

        static string ComputeAntiCheatHash(string e, int t, int a, int r, int s, int i)
        {
            int l = s + i;
            long n = 0 + 23 * t + 89 * a + 41 * r + 67 * s + 13 * i + 97 * l;
            long o = 0;
            for (int idx = 0; idx < e.Length; idx++)
                o += e[idx] * (idx + 1);

            var c = Math.Floor((double)0x178ba57548d * (n += 31 * o) % 9007199254740991);
            string result = $"{e.ToLower()}_{t}_{a}_{r}_{s}_{i}_{c}";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(result);
                byte[] hash = sha256.ComputeHash(bytes);
                string hashstring = BitConverter.ToString(hash).Replace("-", "").ToLower().Substring(0, 32);
                return hashstring;
            }
        }

        public static async Task Script(AccountInfo accountInfo)
        {
            ShowMsg($"当前时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", 0);
            ShowMsg("当前执行账号:" + accountInfo.Index + " - " + accountInfo.Address, 0);

            for (int i = 0; i < ScriptRunCount; i++)
            {
                string SpritetypeResult = await Spritetype(accountInfo);

                if (SpritetypeResult.Contains("一小时限制"))
                {
                    TimeSpan waitTime = accountInfo.NextExecutionTime - DateTime.Now;
                    if (waitTime.TotalMilliseconds > 0)
                    {
                        ShowMsg($"账号 {accountInfo.Index} 遇到限制，等待 {waitTime.Hours}小时{waitTime.Minutes}分{waitTime.Seconds}秒后继续提交", 2);
                        Thread.Sleep((int)waitTime.TotalMilliseconds);
                    }
                }
                else
                {
                    ShowMsg($"第{i + 1}次-成绩提交:" + SpritetypeResult, 1);
                    if (!SpritetypeResult.Contains("Successfully submitted to leaderboard!"))
                        i = i - 1;

                    if (RunMode == 2)
                        Thread.Sleep(1000);
                    else
                    {
                        ShowMsg("35秒后进行下一轮", 1);
                        Thread.Sleep(35000);
                    }
                }
            }
        }

        public static void LoadPrivateKeyAndProxyAndLog()
        {
            if (!File.Exists("Address.txt"))
                File.Create("Address.txt").Close();
            if (!File.Exists("Proxy.txt"))
                File.Create("Proxy.txt").Close();

            string[] address = File.ReadAllLines("Address.txt");
            string[] proxy = File.ReadAllLines("Proxy.txt");

            if (address.Length == 0)
            {
                ShowMsg("未写Address信息，程序即将退出！！！", 3);
                Thread.Sleep(3000);
                Environment.Exit(0);
            }

            AccountInfoList.Clear();
            int index = 1;
            foreach (var line in address)
            {
                string key = line.Trim();
                key = AddressUtil.Current.ConvertToChecksumAddress(key);
                if (string.IsNullOrWhiteSpace(key)) continue;

                try
                {
                    AccountInfoList.Add(new AccountInfo
                    {
                        Index = index,
                        Address = key,
                    });
                    index++;
                }
                catch (Exception ex)
                {
                    ShowMsg($"私钥无效: {key} ({ex.Message})", 3);
                }
            }

            if (AccountInfoList.Count == 0)
            {
                ShowMsg("没有有效的私钥，程序即将退出！", 3);
                Thread.Sleep(3000);
                Environment.Exit(0);
            }

            ShowMsg($"已加载 {AccountInfoList.Count} 个地址", 1);

            // 添加代理
            int proxyLine = proxy.Length;
            for (int i = 0; i < AccountInfoList.Count && i < proxyLine; i++)
            {
                var line = proxy[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    if (line.StartsWith("socks", StringComparison.OrdinalIgnoreCase))
                    {
                        ShowMsg($"不支持 SOCKS 代理，请改用Http或Https代理， {line}", 2);
                        continue;
                    }

                    var uri = new Uri(line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                                      line.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                        ? line : $"http://{line}");

                    var webProxy = new WebProxy(uri);

                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        var userInfo = uri.UserInfo.Split(':');
                        if (userInfo.Length == 2)
                            webProxy.Credentials = new NetworkCredential(userInfo[0], userInfo[1]);
                    }
                    AccountInfoList[i].Proxy = webProxy;
                }
                catch (Exception ex)
                {
                    ShowMsg($"代理格式错误: {line} ({ex.Message})", 3);
                }
            }

            int proxyCount = AccountInfoList.Count(x => x.Proxy is not null);
            ShowMsg($"已加载 {proxyCount} 条代理", proxyCount > 0 ? 1 : 2);
        }

        public static string GetRandomUserAgent()
        {
            Random random = new();
            int revisionVersion = random.Next(1, 8000);
            int tailVersion = random.Next(1, 150);
            return $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.{revisionVersion}.{tailVersion} Safari/537.36";
        }

        public static void ShowMsg(string msg, int logLevel)
        {
            string logFile = $"Log.txt";
            string logText = $"{DateTime.Now} - {msg}\n";
            ConsoleColor color = ConsoleColor.White;
            switch (logLevel)
            {
                case 1:
                    color = ConsoleColor.Green;
                    msg = " ✔   " + msg;
                    break;
                case 2:
                    color = ConsoleColor.DarkYellow;
                    msg = " ⚠   " + msg;
                    break;
                case 3:
                    color = ConsoleColor.Red;
                    msg = " ❌   " + msg;
                    break;
            }
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
                File.AppendAllText(logFile, logText);
            }
        }

        public class AccountInfo
        {
            public int Index { get; set; }
            public string Address { get; set; } = string.Empty;
            public WebProxy? Proxy { get; set; }
            public string UserAgent { get; set; } = GetRandomUserAgent();
            public int FailTime { get; set; }
            public DateTime NextExecutionTime { get; set; } = DateTime.MinValue;
        }
    }
}
