using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

namespace me100_kinect {
    static class HttpClientWrapper {
        public const string PYTHON_API_ADDRESS = "http://localhost:2021";
        public const string ESP32_IP = "http://192.168.144.213/27";

        private static HttpClient client;

        static HttpClient getInstance() {
            if (client == null) client = new HttpClient();
            return client;
        }

        public static Task<System.Net.Http.HttpResponseMessage> get(string url) {
            return getInstance().GetAsync(url);
    }

        public static async Task<System.Net.Http.HttpResponseMessage> post(string url, string body) {
            HttpContent content = new StringContent(body, Encoding.UTF8);
            return await getInstance().PostAsync(url, content);
        }

        public static async Task<System.Net.Http.HttpResponseMessage> postFile(
            string url, string filename, byte [] fileBytes) {

            HttpContent bytesContent = new ByteArrayContent(fileBytes);

            using (var formData = new MultipartFormDataContent()) {
                // <input type="file" name="file" />
                formData.Add(bytesContent, "file", filename);
                var response = await getInstance().PostAsync(url, formData);
                return response;
            }
        }
    }
}
