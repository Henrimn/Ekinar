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
        private static string _hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
        private static string _copyHostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts-copy");

        static void Main(string[] args)
        {
            string beggarPath = Assembly.GetExecutingAssembly().Location;
            string baseDirectory = Path.GetDirectoryName(beggarPath);
            string vindictusPath = Path.Combine(baseDirectory, "heroes.exe"); // or Vindictus.exe
            string backupVindictusPath = Path.Combine(baseDirectory, "heroes.exe.bak");
            string beggarCopy = Path.Combine(baseDirectory, "Beggar-Copy.exe");
            string batchPath = Path.Combine(baseDirectory, "start_game.bat");
            string[] hostsString = { "127.0.0.1 vindictus.dn.nexoncdn.co.kr", "127.0.0.1 twheroes.arenadownload.nexon.com", "127.0.0.1 heroes.dn.nexoncdn.co.kr" };

            if (args.Length == 0)
            {
                Console.WriteLine("        _____________________ ________  ________    _____ __________  ");
                Console.WriteLine(@"======= \______   \_   _____//  _____/ /  _____/   /  _  \\______   \ ");
                Console.WriteLine(@"=======  |    |  _/|    __)_/   \  ___/   \  ___  /  /_\  \|       _/ ");
                Console.WriteLine(@"=======  |    |   \|        \    \_\  \    \_\  \/    |    \    |   \ ");
                Console.WriteLine(@"=======  |______  /_______  /\______  /\______  /\____|__  /____|_  / ");
                Console.WriteLine(@"                \/        \/        \/        \/         \/       \/  ");
                Console.WriteLine("\n-----------------------------------------------------------------------\n");
                
                if (File.Exists(_copyHostsFilePath))
                {
                    File.Delete(_hostsFilePath);
                    File.Move(_copyHostsFilePath, _hostsFilePath);
                }
                
                if (!File.Exists(backupVindictusPath))
                {
                    File.Move(vindictusPath, backupVindictusPath); // temporary rename original Vindictus.exe to Vindictus.exe.bak
                    File.Copy(beggarPath, vindictusPath); // create a copy of Beggar and name it Vindictus.exe
                }
                
                Console.WriteLine("Please press PLAY button in Nexon Launcher...");

                while (true)
                {
                    if (File.Exists(batchPath))
                        break;
                }

                if (ModifyHostsFile(hostsString))
                {
                    Console.WriteLine(hostsString + " successfully added to hosts file."); // write to hosts file 
                }

                File.Move(vindictusPath, beggarCopy); // rename the copy of Beggar. Note: cannot use File.Delete here because it takes a while for the file to be actually deleted

                File.Move(backupVindictusPath, vindictusPath); // rename original file back to Vindictus.exe

                Process.Start(batchPath); // start the game with received launch options from Nexon Launcher

                File.Delete(beggarCopy); // delete the copy of Beggar

                Console.WriteLine("Waiting for Vindictus to start...");

                while (true)
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

                    file.WriteLine("start vindictus.exe " + String.Join(" ", args)); // write launch options to batch file
                }
            }
        }

        public static bool ModifyHostsFile(string[] servers)
        {
            try
            {
                File.Copy(_hostsFilePath, _copyHostsFilePath); // make a copy of the original hosts file

                using (StreamWriter w = File.AppendText(_hostsFilePath))
                {
                    foreach (var server in servers)
                    {
                        w.WriteLine(server);
                    }
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
