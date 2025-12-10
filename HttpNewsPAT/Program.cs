using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
            SingIn("user", "user");
            /*
            WebRequest request = WebRequest.Create("http://news.permaviat.ru/main");
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())

            {
                Console.WriteLine(response.StatusDescription);
                using (Stream dataStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(dataStream))
                    {
                        string responseFromServer = reader.ReadToEnd();
                        Console.WriteLine(responseFromServer);

                    }
                }
                
            }
            */
            Console.Read();
        }
        public static void SingIn(string login, string password)
        {
            string Uri = "http://news.pernaviat.ru/ajax/login.php";
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
            }
        }
    }
}
