using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        static async Task Main(string[] args)
        {
            Cookie token = await SingIn("user", "user");
            string Content = await GetContent(token);
            ParsingHtml(Content);

            Console.Read();
        }
        public static void ParsingHtml(string htmlCode)
        {
            var Html = new HtmlDocument();
            Html.LoadHtml(htmlCode);

            var Document = Html.DocumentNode;
            IEnumerable<HtmlNode> DivNews = Document.Descendants(0).Where(x => x.HasClass("news"));

            foreach (var DivNew in DivNews)
            {
                var src = DivNew.ChildNodes[1].GetAttributeValue("src", "node");
                var name = DivNew.ChildNodes[3].InnerHtml;
                var description = DivNew.ChildNodes[5].InnerHtml;

                Console.WriteLine($"{name} \nИзображение: {src} \nОписание: {description}");
            }
        }

        public static async Task<string> GetContent(Cookie token)
        {
            string url = "http://news.permaviat.ru/main";
            Debug.WriteLine($"Выполняем запрос: {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (token != null)
            {
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка получения контента: {ex.Message}");
                return null;
            }
        }
        public static async Task<Cookie> SingIn(string login, string password)
        {
            string uri = "http://news.permaviat.ru/ajax/login.php";
            Debug.WriteLine($"Выполнен запрос: {uri}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await _httpClient.PostAsync(uri, content);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                string responseFromServer = await response.Content.ReadAsStringAsync();
                Console.WriteLine(responseFromServer);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var tokenCookie = cookies.FirstOrDefault(c => c.Contains("token="));
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        var cookieValue = tokenCookie.Split('=')[1].Split(';')[0];
                        return new Cookie("token", cookieValue, "/", "news.permaviat.ru");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
            }

            return null;
        }
    }
}