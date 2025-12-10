using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace HttpNewsPAT
{
    internal class Program
    {
        static void Main(string[] args)
        {
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
            Console.Read();
        }
    }
}
