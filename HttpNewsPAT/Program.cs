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
            Trace.Listeners.Add(new TextWriterTraceListener("debug.log"));
            Trace.AutoFlush = true;
            Trace.WriteLine($"=== Запуск программы: {DateTime.Now} ===");

            Console.WriteLine("1. Добавить и парсить permaviat");
            Console.WriteLine("2. Парсить Old Boy Barbershop");
            Console.Write("Выберите: ");
            string select = Console.ReadLine();

            Trace.WriteLine($"Выбран вариант: {select}");
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

                Trace.WriteLine($"Введены данные: name={name}, src={src}");
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
                await ParseOldBoyBarbershop();
            }

            Trace.WriteLine($"=== Завершение программы: {DateTime.Now} ===");
            Trace.Flush();
            Console.Read();
        }

        public static async Task ParseOldBoyBarbershop()
        {
            Trace.WriteLine("Начало парсинга OldBoyBarbershop");

            try
            {
                string url = "https://oldboybarbershop.com/services";

                var response = await _httpClient.GetAsync(url);
                string html = await response.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var services = doc.DocumentNode.SelectNodes("//div[contains(@class, 'service')] | //article | //div[contains(@class, 'card')]");

                if (services == null)
                {
                    services = doc.DocumentNode.SelectNodes("//div[@data-service] | //section//div");
                }

                if (services != null && services.Count > 0)
                {
                    Console.WriteLine("Услуги Old Boy Barbershop:");
                    Console.WriteLine("==========================");

                    int count = 1;
                    foreach (var service in services.Take(6))
                    {
                        string fullText = service.InnerText.Trim();

                        var lines = fullText.Split('\n')
                            .Select(line => line.Trim())
                            .Where(line => !string.IsNullOrEmpty(line))
                            .Where(line => line.Length > 2)
                            .ToArray();

                        if (lines.Length > 0)
                        {
                            string title = lines[0];
                            string description = lines.Length > 1 ? lines[1] : "";

                            Console.WriteLine($"{count}. {title}");
                            if (!string.IsNullOrEmpty(description))
                            {
                                Console.WriteLine($"   {description}");
                            }
                            Console.WriteLine();
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            Trace.WriteLine("Конец парсинга OldBoyBarbershop");
        }
        public static void ParsingHtml(string htmlCode)
        {
            Trace.WriteLine("Начало ParsingHtml");
            var Html = new HtmlDocument();
            Html.LoadHtml(htmlCode);

            var Document = Html.DocumentNode;
            IEnumerable<HtmlNode> DivNews = Document.Descendants(0).Where(x => x.HasClass("news"));

            foreach (var DivNew in DivNews)
            {
                var src = DivNew.ChildNodes[1].GetAttributeValue("src", "node");
                var name = DivNew.ChildNodes[3].InnerHtml;
                var description = DivNew.ChildNodes[5].InnerHtml;

                Trace.WriteLine($"Парсинг: name={name}, src={src}");
                Console.WriteLine($"{name} \nИзображение: {src} \nОписание: {description}");
            }
            Trace.WriteLine("Конец ParsingHtml");
        }

        public static async Task<bool> AddNews(Cookie token, string name, string src, string description)
        {
            string url = "http://10.111.20.114/ajax/add.php";

            try
            {
                Trace.WriteLine($"AddNews запрос: {url}");
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
                Trace.WriteLine($"AddNews статус: {response.StatusCode}");

                string responseText = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"AddNews ответ: {responseText}");
                Console.WriteLine($"Ответ: {responseText}");

                if (response.IsSuccessStatusCode && !responseText.Contains("<html>"))
                {
                    Trace.WriteLine("Запись успешно добавлена");
                    Console.WriteLine("Запись добавлена!");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка AddNews: {ex.Message}");
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }

        public static async Task<string> GetContent(Cookie token)
        {
            string url = "http://10.111.20.114/main";
            Trace.WriteLine($"GetContent запрос: {url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);

            if (token != null)
            {
                request.Headers.Add("Cookie", $"{token.Name}={token.Value}");
            }

            try
            {
                var response = await _httpClient.SendAsync(request);
                Trace.WriteLine($"GetContent статус: {response.StatusCode}");

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка GetContent: {ex.Message}");
                Console.WriteLine($"Ошибка получения контента: {ex.Message}");
                return null;
            }
        }
        public static async Task<Cookie> SingIn(string login, string password)
        {
            string uri = "http://10.111.20.114/ajax/login.php";
            Trace.WriteLine($"SingIn запрос: {uri}");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("login", login),
                new KeyValuePair<string, string>("password", password)
            });

            try
            {
                var response = await _httpClient.PostAsync(uri, content);
                Trace.WriteLine($"SingIn статус: {response.StatusCode}");

                string responseFromServer = await response.Content.ReadAsStringAsync();
                Trace.WriteLine($"SingIn ответ: {responseFromServer}");
                Console.WriteLine(responseFromServer);

                if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                {
                    var tokenCookie = cookies.FirstOrDefault(c => c.Contains("token="));
                    if (!string.IsNullOrEmpty(tokenCookie))
                    {
                        var cookieValue = tokenCookie.Split('=')[1].Split(';')[0];
                        Trace.WriteLine($"Токен получен: {cookieValue}");
                        return new Cookie("token", cookieValue, "/", "10.111.20.114");
                    }
                }
                Trace.WriteLine("Токен не найден");
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Ошибка SingIn: {ex.Message}");
                Console.WriteLine($"Ошибка авторизации: {ex.Message}");
            }

            return null;
        }
    }
}