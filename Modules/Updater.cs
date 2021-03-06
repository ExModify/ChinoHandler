using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.ObjectModel;
using ChinoHandler.Models;
using System.Net.Http;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Reflection;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace ChinoHandler.Modules
{
    public class Updater
    {
        string TempFolder;
        string BotDownloadUrl;
        string HandlerDownloadUrl;

        public bool IsUpdate { get; set; }

        public Updater(Config Config)
        {
            TempFolder = Config.TempFolder;
            if (Directory.Exists(TempFolder))
            {
                Directory.Delete(TempFolder, true);
            }
            Directory.CreateDirectory(TempFolder);

            BotDownloadUrl = Config.BotProjectUrl + "/archive/master.zip";
            HandlerDownloadUrl = Config.HandlerProjectUrl + "/archive/master.zip";
        }

        public void Update(bool IsBot)
        {
            Logger.Log("Downloading...", "Updater", ConsoleColor.Magenta);
            try
            {
                Download(IsBot);
                Logger.Log("Downloaded!", "Updater", ConsoleColor.Magenta);
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't download archive! Error: " + e.Message, "Updater", ConsoleColor.Red);
                return;
            }
            Logger.Log("Extracting...", "Updater", ConsoleColor.Magenta);
            string Path;
            try
            {
                Path = Extract().Replace("\\", "/");
                Logger.Log("Extracted!", "Updater", ConsoleColor.Magenta);
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't extract archive! Error: " + e.Message, "Updater", ConsoleColor.Red);
                return;
            }

            Logger.Log("Compiling... ", "Updater", ConsoleColor.Magenta);
            try
            {
                Path = Compile(Path).Replace("\\", "/");
                Logger.Log("Compiled!", "Updater", ConsoleColor.Magenta);
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't compile the project! Error: " + e.Message, "Updater", ConsoleColor.Red);
                return;
            }
            Logger.Log("Swapping...", "Updater", ConsoleColor.Magenta);
            try
            {
                Swap(IsBot, Path);
                Logger.Log("Swapped!", "Updater", ConsoleColor.Magenta);
            }
            catch (Exception e)
            {
                Logger.Log("Couldn't swap! Error: " + e.Message, "Updater", ConsoleColor.Red);
                return;
            }
        }

        void Download(bool IsBot)
        {
            HttpClient client = new HttpClient();
            FileStream fs = new FileStream(TempFolder + "/content.zip", FileMode.CreateNew);
            string link = BotDownloadUrl;
            if (!IsBot)
            {
                link = HandlerDownloadUrl;
            }
            Stream s = client.GetStreamAsync(link).Result;
            s.CopyTo(fs);
            s.Close();
            fs.Close();
        }
        string Extract()
        {
            string file = TempFolder + "/content.zip";
            ZipFile.ExtractToDirectory(file, TempFolder, true);
            File.Delete(file);
            return Directory.EnumerateDirectories(TempFolder).First();
        }
        string Compile(string Folder)
        {
            string csporj = Path.GetFileName(Directory.EnumerateFiles(Folder, "*.csproj").First());
            StartAndWait("dotnet restore " + csporj, Folder);
            string copyFolder = Folder + "/bin/Release/netcoreapp3.1/";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                copyFolder += "win-x64/";
                StartAndWait("dotnet publish " + csporj + " -c Release -f netcoreapp3.1 -r win-x64 -p:PublishSingleFile=true", Folder);
            }
            else
            {
                copyFolder += "linux-x64/";
                StartAndWait("dotnet publish " + csporj + " -c Release -f netcoreapp3.1 -r linux-x64 -p:PublishSingleFile=true", Folder);
            }

            return copyFolder + "publish";
        }
        void Swap(bool IsBot, string Folder)
        {
            string file = Directory.EnumerateFiles(Folder).First(t => !t.EndsWith(".pdb"));
            if (IsBot)
            {
                string location = Program.BotHandler.Location;
                IsUpdate = true;
                Program.BotHandler.Quit();
                File.Delete(location);
                File.Copy(file, location, true);
                Program.BotHandler.Start();
                Directory.Delete(TempFolder, true);
                Directory.CreateDirectory(TempFolder);
            }
            else
            {
                string location = Process.GetCurrentProcess().MainModule.FileName;
                string updateFile = "update";
                int pid = Process.GetCurrentProcess().Id;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    updateFile += ".bat";
                    string content = ":loop\r\n"
                            + "tasklist | find \"" + pid + "\"  >nul 2>&1\r\n"
                            + "if %errorlevel% == 0 (\r\n"
                            + "timeout /t 5 /nobreak\r\n"
                            + "goto loop\r\n)\r\n"
                            + $"copy /y \"{ file }\" \"{ location }\"\r\n"
                            + $"start cmd /c \"{ location }\"\r\n"
                            + "del \"%~f0\"";
                            
                    File.WriteAllText(updateFile, content);
                    ProcessStartInfo info;
                    info = new ProcessStartInfo()
                    {
                        FileName = "cmd",
                        Arguments = $"/c { updateFile }",
                        UseShellExecute = true
                    };
                    Process.Start(info);
                }
                else
                {
                    File.Delete(location);
                    File.Copy(file, location);
                }
                Program.BotHandler.Quit();
                Environment.Exit(0);
            }
        }


        string StartAndWait(string Command, string WorkingDirectory = null)
        {
            string[] cmd = Command.Split(' ');
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = cmd[0],
                Arguments = string.Join(" ", cmd.Skip(1)),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            if (WorkingDirectory != null) info.WorkingDirectory = WorkingDirectory;
            Process p = Process.Start(info);
            p.WaitForExit();

            return p.StandardOutput.ReadToEnd() + "\n\n" + p.StandardError.ReadToEnd();
        }
    }
}
