using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using ChinoHandler.Models;

namespace ChinoHandler.Modules
{
    public class Logger
    {
        public static List<LogMessage> Logs;
        static StreamWriter CurrentLog;
        public static List<LogMessage> HungLogs; 
        public static void SetupLogger()
        {
            Logs = new List<LogMessage>();
            HungLogs = new List<LogMessage>();

            if (!Directory.Exists("log"))
            {
                Directory.CreateDirectory("log");
            }
            for (int i = 0; i < int.MaxValue; i++)
            {
                string filename = "log/log." + i + ".log";
                if (!File.Exists(filename))
                {
                    FileStream fs = new FileStream(filename, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite);
                    CurrentLog = new StreamWriter(fs)
                    {
                        AutoFlush = true
                    };
                    break;
                }
            }
        }
        public static void Log(string Message = "", string Source = "Handler", ConsoleColor Color = ConsoleColor.White, string Severity = "")
        {
            bool hang = Program.ShowMenu;

            string logLine = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            if (!hang)
                WriteSection(logLine, ConsoleColor.Cyan);
            if (!string.IsNullOrWhiteSpace(Source))
            {
                logLine += $" [{ Source }]";
                if (!hang)
                    WriteSection(Source, Color);
            }
            if (!string.IsNullOrWhiteSpace(Severity))
            {
                logLine += $" [{ Severity }]";
                if (!hang)
                    WriteSection(Severity, Color);
            }
            if (!hang)
                Console.WriteLine("\b: " + Message);
            logLine += (": " + Message);


            LogMessage msg = new LogMessage()
            {
                Message = Message,
                Module = Source,
                Color = Color,
                Type = Severity
            };
            if (hang) HungLogs.Add(msg);

            Logs.Add(msg);
            
            if (Program.RemoteConsole != null) Program.RemoteConsole.Log(msg);
            if (CurrentLog != null) CurrentLog.WriteLine(logLine);
        }
        static void WriteSection(string Message, ConsoleColor Color)
        {
            Console.ForegroundColor = Color;
            Console.Write("[{0}] ", Message);
            Console.ResetColor();
        }
    }
}
