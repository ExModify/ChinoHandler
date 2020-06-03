using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ChinoHandler.Models;
using Newtonsoft.Json;

namespace ChinoHandler.Modules
{
    public class BotHandler
    {
        public string Location { get; private set; }
        public Process ChinoProcess;
        public bool Running { get; private set; } = false;

        public event Action Exit;

        ProcessStartInfo Info;

        StreamWriter Writer;
        StreamReader Reader;

        public BotHandler(Config Config)
        {
            if (!Directory.Exists(Config.BotLocation))
            {
                Directory.CreateDirectory(Config.BotLocation);
            }
            Location = Config.BotLocation + "/Chino-chan";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Location += ".exe";

            Info = new ProcessStartInfo()
            {
                FileName = Location,
                Arguments = "1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };
            CreateProcess();
        }
        public void Start()
        {
            if (!File.Exists(Location))
            {
                Logger.Log("Chino-chan not found! Downloading...");
                Program.Updater.Update(true);
            }

            if (!Running)
            {
                CreateProcess();
                ChinoProcess.Start();
                Running = true;
                ChinoProcess.Exited += (s, e) =>
                {
                    Running = false;
                    System.Console.WriteLine("Chino-chan exited with code: " + ChinoProcess.ExitCode);
                    Exit?.Invoke();
                };
                Writer = ChinoProcess.StandardInput;
                Reader = ChinoProcess.StandardOutput;
                Thread th = new Thread(() =>
                {
                    while (!ChinoProcess.HasExited)
                    {
                        ProcessChinoOutput(Reader.ReadLine());
                    }
                });
                th.Start();
            }
            
        }

        public void Quit()
        {
            if (Running)
                ChinoProcess.Kill();
        }
        
        public void Send(string Message)
        {
            Writer.WriteLine(Message);
        }

        private void ProcessChinoOutput(string Line)
        {
            if (Line.TrimStart().StartsWith("Information:"))
            {
                string data = Line.Substring(12);
                string[] info = data.Split('|');
                double memUsage = (GetMemoryUsage() + Program.GetMemoryUsage()) / 1048576.0;
                Console.Title = $"Chino-chan | Servers: { info[0] }"
                + $" | Executing { info[1] } commands"
                + $" | Voice: { info[2] }"
                + $" | Uptime: { TimeSpan.FromMilliseconds(double.Parse(info[3])).ToString(@"hh\:mm\:ss") }"
                + $" | Memory usage: { memUsage.ToString("N2") } MB";
            }
            else
            {
                try
                {
                    ChinoResponse Response = JsonConvert.DeserializeObject<ChinoResponse>(Line);
                    Logger.Log(Response.Message, Response.Module, Response.Color, Response.Type);
                }
                catch
                {
                    Console.WriteLine(Line);
                    string Lower = Line.ToLower();
                    Program.HandleCommand(Lower);
                }
            }
        }

        private void CreateProcess()
        {
            ChinoProcess = new Process()
            {
                StartInfo = Info
            };
        }
        private long GetMemoryUsage()
        {
            Process currentProcess = ChinoProcess;
            return currentProcess.NonpagedSystemMemorySize64 + currentProcess.PagedMemorySize64;
        }

        

    }

    
}
