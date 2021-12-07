﻿using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace me100_kinect {
    static class HttpClientWrapper {
        private static HttpClient client;

        static HttpClient getInstance() {
            if (client == null) client = new HttpClient();
            return client;
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