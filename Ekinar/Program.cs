using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Ekinar
{
    class Program
    {
        private static EventHandler _handler;

        static void Main(string[] args)
        {
            Console.WriteLine("___________ ____  __..___  _______      _____   __________ ");
            Console.WriteLine(@"\_   _____/|    |/ _||   | \      \    /  _  \  \______   \");
            Console.WriteLine(@" |    __)_ |      <  |   | /   |   \  /  /_\  \  |       _/");
            Console.WriteLine(@" |        \|    |  \ |   |/    |    \/    |    \ |    |   \");
            Console.WriteLine(@"/_______  /|____|__ \|___|\____|__  /\____|__  / |____|_  /");
            Console.WriteLine(@"        \/         \/             \/         \/         \/ ");
            Console.WriteLine("\n-----------------------------------------------------------\n");

            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);
            Random rand = new Random();
            string[] prefixes = { "http://127.0.0.1/Game/Vindictus/en-US/EndPoint.txt/", "http://127.0.0.1/EndPoint.txt/" ,"http://127.0.0.1/ko-KR/EndPoint.txt/" };
            string[] naServerIp = {"34.218.172.146", "34.218.175.106", "34.218.178.68", "34.218.182.12", "34.218.182.202", "34.218.188.180", "34.218.190.181" };
            string[] euServerIp = {"18.184.128.66", "18.184.151.140", "18.184.195.53", "18.184.196.86", "18.184.197.129", "18.184.198.133" };
            string[] twServerIp = { "124.108.151.3", "124.108.151.4", "124.108.151.5", "124.108.151.6", "124.108.151.7" };

            
            int index = rand.Next(naServerIp.Length);
            // int index = rand.Next(euServerIp.Length);
            // int index = rand.Next(twServerIp.Length);

            WebServer webServer = new WebServer(WebServerEndPointResponse, prefixes);
            webServer.Run();
            
            new EkinarServer().Start(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 27015), new IPEndPoint(IPAddress.Parse(naServerIp[index]), 27015));
            Console.ReadKey();
        }

        #region Web Server Response
        public static string WebServerEndPointResponse(HttpListenerRequest request)
        {
            Console.WriteLine("[Ekinar] Received web request from client.");
            string baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string endpointPath = Path.Combine(baseDirectory, "EndPoint.txt");
            string content = File.ReadAllText(endpointPath);
            return content;
        }
        #endregion

        #region Cleanup Hosts File On Exit
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        private static bool Handler(CtrlType sig)
        {
            switch (sig)
            {
                case CtrlType.CTRL_C_EVENT:
                    CleanupHostsFile();
                    return true;
                case CtrlType.CTRL_LOGOFF_EVENT:
                    CleanupHostsFile();
                    return true;
                case CtrlType.CTRL_SHUTDOWN_EVENT:
                    CleanupHostsFile();
                    return true;
                case CtrlType.CTRL_CLOSE_EVENT:
                    CleanupHostsFile();
                    return true;
                default:
                    return false;
            }
        }

        private static void CleanupHostsFile()
        {
            string hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
            string copyHostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts-copy");

            if (File.Exists(copyHostsFilePath))
            {
                try
                {
                    File.Delete(hostsFilePath);
                    File.Move(copyHostsFilePath, hostsFilePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        #endregion
    }
}
