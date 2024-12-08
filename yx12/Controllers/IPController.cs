using System.Buffers.Text;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace yx12.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IPController : ControllerBase
    {

        private readonly ILogger<IPController> _logger;

        public IPController(ILogger<IPController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            try
            {
                List<string> list = new List<string>();
                var yxipResult = await ExtractNodes();
                if (yxipResult != null && yxipResult.code == 0)
                {

                    foreach (var n in yxipResult.data)
                    {
                        n.title = HttpUtility.UrlEncode(n.title + n.area);
                        var ss_url = Convert.ToBase64String(System.Text.UTF8Encoding.UTF8.GetBytes($"aes-256-cfb:{n.password}@{n.ip}:{n.port}"));
                        list.Add($"ss://{ss_url}#{n.title}");
                    }
                }
                return string.Join("\n", list);
            }
            catch
            {
                return "";
            }
        }


        private byte[] DecryptData(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Key = key;
                aesAlg.IV = iv;
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.None;

                using (var decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV))
                using (var msDecrypt = new MemoryStream(encryptedData))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new StreamReader(csDecrypt))
                {
                    var decrypted = srDecrypt.ReadToEnd();
                    // Remove padding
                    byte paddingLength = (byte)decrypted[decrypted.Length - 1];
                    return Encoding.UTF8.GetBytes(decrypted.Substring(0, decrypted.Length - paddingLength));
                }
            }
        }
        private async Task<YxipResult> ExtractNodes()
        {
            try
            {
                string url = "http://api.skrapp.net/api/serverlist";
                var client = new HttpClient();
            //    var content = new FormUrlEncodedContent(new[]
            //    {
            //    new KeyValuePair<string, string>("data", "4265a9c353cd8624fd2bc7b5d75d2f18b1b5e66ccd37e2dfa628bcb8f73db2f14ba98bc6a1d8d0d1c7ff1ef0823b11264d0addaba2bd6a30bdefe06f4ba994ed")
            //});
            //    client.DefaultRequestHeaders.Add("Accept-Language", "zh-Hans-CN;q=1, en-CN;q=0.9");
            //    client.DefaultRequestHeaders.Add("AppVersion", "1.3.1");
            //    client.DefaultRequestHeaders.Add("User-Agent", "SkrKK/1.3.1 (iPhone; iOS 13.5; Scale/2.00)");
            //    client.DefaultRequestHeaders.Add("Cookie", "PHPSESSID=fnffo1ivhvt0ouo6ebqn86a0d4");
    
                
                var response = await client.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    var encryptedText = response.Content.ReadAsStringAsync().Result;
                    var encryptedData = Enumerable.Range(0, encryptedText.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(encryptedText.Substring(x, 2), 16))
                         .ToArray();

                    var key = Encoding.UTF8.GetBytes("65151f8d966bf596");
                    var iv = Encoding.UTF8.GetBytes("88ca0f0ea1ecf975");

                    var decryptedData = DecryptData(encryptedData, key, iv);
                    var result = System.Text.Encoding.UTF8.GetString(decryptedData);
                    YxipResult yxipResult = System.Text.Json.JsonSerializer.Deserialize<YxipResult>(result);
                        return await Task.FromResult(yxipResult);
                }
                return null;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public class YxipResult
        { 
            public int code { get; set; }
            public string msg { get; set; }
            public List<YxNode> data { get; set; }

        }
        public class YxNode
        {
            public string id { get; set; }
            public string title { get; set; }

            public string ip { get; set; }
            public string port { get; set; }
            public string password { get; set; }
            public string encrypt { get; set; }
            public string area { get; set; }
            public string areaen { get; set; }
        }
    }
}
