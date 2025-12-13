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
            Console.WriteLine("1. Добавить и парсить permaviat");
            Console.WriteLine("2. Парсить Lenta.ru");
            Console.Write("Выберите: ");
            string select = Console.ReadLine();

            if (select == "1")
            {
                Cookie token = await SingIn("user", "user");

                Console.WriteLine("\nДобавление новой записи");

                Console.Write("Заголовок: ");
                string name = Console.ReadLine();

                Console.Write("Текст: ");
                string description = Console.ReadLine();

                Console.Write("Ссылка на изображение: ");
                string src = Console.ReadLine();

                bool success = await AddNews(token, name, src, description);

                if (success)
                {
                    Console.WriteLine("\nОбновлённый список:");
                    string content = await GetContent(token);
                    ParsingHtml(content);
                }
            }
            else if (select == "2")
            {
                await ParseLenta();
            }

            Console.Read();
        }

        public static async Task ParseLenta()
        {
            try
            {
                string url = "https://lenta.ru/";
                var response = await _httpClient.GetAsync(url);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var newsItems = doc.DocumentNode.SelectNodes("//a[contains(@class, 'card')]");
                {
                    int count = 1;
                    foreach (var item in newsItems.Take(10))
                    {
                        var titleNode = item.SelectSingleNode(".//span[contains(@class, 'card-title')]");
                        string title = titleNode?.InnerText?.Trim() ?? item.InnerText.Trim();

                        string link = item.GetAttributeValue("href", "");
                        if (!string.IsNullOrEmpty(link) && !link.StartsWith("http"))
                            link = "https://lenta.ru" + link;

                        Console.WriteLine($"{count}. {title}");
                        Console.WriteLine();
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
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

        public static async Task<bool> AddNews(Cookie token, string name, string src, string description)
        {
            string url = "http://news.permaviat.ru/ajax/add.php";

            try
            {
                Debug.WriteLine($"Выполнение запроса: {url}");

                var postData = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("name", name),
                    new KeyValuePair<string, string>("description", description),
                    new KeyValuePair<string, string>("src", src),
                    new KeyValuePair<string, string>("token", token.Value)
                });

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = postData;
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");

                var response = await _httpClient.SendAsync(request);
                Debug.WriteLine($"Статус выполнения: {response.StatusCode}");

                string responseText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Ответ: {responseText}");

                if (response.IsSuccessStatusCode && !responseText.Contains("<html>"))
                {
                    Console.WriteLine("Запись добавлена!");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
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