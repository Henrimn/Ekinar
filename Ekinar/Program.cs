using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Reflection;

namespace Ekinar
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("___________ ____  __..___  _______      _____   __________ ");
            Console.WriteLine(@"\_   _____/|    |/ _||   | \      \    /  _  \  \______   \");
            Console.WriteLine(@" |    __)_ |      <  |   | /   |   \  /  /_\  \  |       _/");
            Console.WriteLine(@" |        \|    |  \ |   |/    |    \/    |    \ |    |   \");
            Console.WriteLine(@"/_______  /|____|__ \|___|\____|__  /\____|__  / |____|_  /");
            Console.WriteLine(@"        \/         \/             \/         \/         \/ ");
            Console.WriteLine("\n-----------------------------------------------------------");

            string[] prefixes = { "http://127.0.0.1/Game/Vindictus/en-US/EndPoint.txt/", "http://127.0.0.1/EndPoint.txt/" ,
                                  "http://127.0.0.1./Vindictus/en-EU/EndPoint.txt/",  "http://127.0.0.1/ko-KR/EndPoint.txt/" };

            WebServer webServer = new WebServer(WebServerEndPoint, "http://127.0.0.1/Game/Vindictus/en-US/EndPoint.txt/");
            webServer.Run();
            new EkinarServer().Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27015), new IPEndPoint(IPAddress.Parse("34.218.172.146"), 27015));
            Console.ReadKey();
        }

        public static string WebServerEndPoint(HttpListenerRequest request)
        {
            Console.WriteLine("Received web request from client.");
            string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string endpointPath = Path.Combine(baseDirectory, "EndPoint.txt");
            string content = File.ReadAllText(endpointPath);
            return content;
        }
    }
}
