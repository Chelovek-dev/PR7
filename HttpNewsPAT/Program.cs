using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using HtmlAgilityPack;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Cookie token = SingIn("user", "user");
            string Content = GetContent(token);
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
        public static string GetContent(Cookie token)
        {
            string Content = null;
            string Url = "http://news.permaviat.ru/main";
            Debug.WriteLine($"Выполняем запрос: {Url}");

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Url);
            Request.CookieContainer = new CookieContainer();
            Request.CookieContainer.Add(token);

            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            {
                Debug.WriteLine($"Статус выполнения: {Response.StatusCode}");

                Content = new StreamReader(Response.GetResponseStream()).ReadToEnd();
            }
            return Content;
        }
        public static Cookie SingIn(string login, string password)
        {
            Cookie token = null;
            string Uri = "http://news.permaviat.ru/ajax/login.php";
            Debug.WriteLine($"Выполнен запрос: {Uri}");

            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(Uri);
            Request.Method = "POST";
            Request.ContentType = "application/x-www-form-urlencoded";
            Request.CookieContainer = new CookieContainer();
            byte[] Data = Encoding.ASCII.GetBytes($"login={login}&password={password}");
            Request.ContentLength = Data.Length;

            using (Stream stream = Request.GetRequestStream())
            {
                stream.Write(Data, 0, Data.Length);
            }

            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            {
                Debug.WriteLine($"Статус выполнения: {Response.StatusCode}");
                string ResponseFromServer = new StreamReader(Response.GetResponseStream()).ReadToEnd();
                Console.WriteLine(ResponseFromServer);

                token = Response.Cookies["token"];
            }
            return token;
        }
    }
}
