using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Beggar
{
    class Program
    {
        static void Main(string[] args)
        {
            string beggarPath = Assembly.GetExecutingAssembly().Location;
            string baseDirectory = Path.GetDirectoryName(beggarPath);
            string vindictusPath = Path.Combine(baseDirectory, "Vindictus.exe");
            string tempVindictusPath = Path.Combine(baseDirectory, "Vindictus.exe.bak");
            string batchPath = Path.Combine(baseDirectory, "start_game.bat");
            string hostsString = "127.0.0.1 vindictus.dn.nexoncdn.co.kr";

            if (args.Length == 0)
            {
                Console.WriteLine("        _____________________ ________  ________    _____ __________  ");
                Console.WriteLine(@"======= \______   \_   _____//  _____/ /  _____/   /  _  \\______   \ ");
                Console.WriteLine(@"=======  |    |  _/|    __)_/   \  ___/   \  ___  /  /_\  \|       _/ ");
                Console.WriteLine(@"=======  |    |   \|        \    \_\  \    \_\  \/    |    \    |   \ ");
                Console.WriteLine(@"=======  |______  /_______  /\______  /\______  /\____|__  /____|_  / ");
                Console.WriteLine(@"                \/        \/        \/        \/         \/       \/  ");
                Console.WriteLine("\n-----------------------------------------------------------------------");
                
                File.Move(vindictusPath, tempVindictusPath); // temporary rename original Vindictus.exe to Vindictus.exe.bak
                File.Copy(beggarPath, vindictusPath); // create a copy of Beggar and name it Vindictus.exe

                Console.WriteLine("\nPlease press PLAY button in Nexon Launcher...");

                while(true)
                {
                    if (File.Exists(batchPath))
                        break;
                }

                if (ModifyHostsFile(hostsString))
                {
                    Console.WriteLine(hostsString + " successfully added to hosts file."); // write to hosts file 
                }

                File.Delete(vindictusPath); // delete current Vindictus.exe (Beggar)
                File.Move(tempVindictusPath, vindictusPath); // rename original Vindictus.exe

                Process.Start(batchPath); // start the game with received launch options from Nexon Launcher

                Console.WriteLine("Waiting for Vindictus to start...");

                while(true)
                {
                    Process[] vindictusProcess = Process.GetProcessesByName("Vindictus");

                    if (vindictusProcess.Length > 0)
                    {
                        File.Delete(batchPath); // delete the batch file when Vindictus started successfully
                        break;
                    }
                }

                Environment.Exit(0);
            }
            else
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(batchPath))
                {

                    file.Write("start vindictus.exe " + String.Join(" ", args)); // write launch options to batch file
                }
            }
        }

        public static bool ModifyHostsFile(string entry)
        {
            try
            {
                using (StreamWriter w = File.AppendText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts")))
                {
                    w.WriteLine(entry);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
}
