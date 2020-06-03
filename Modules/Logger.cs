using System;
using System.IO;
using System.Net;
using System.Text;

namespace ChinoHandler.Modules
{
    public class Logger
    {
        public static void Log(string Message = "", string Source = "Handler", ConsoleColor Color = ConsoleColor.White, string Severity = "")
        {
            WriteSection(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), ConsoleColor.Cyan);
            if (!string.IsNullOrWhiteSpace(Source))
            {
                WriteSection(Source, Color);
            }
            if (!string.IsNullOrWhiteSpace(Severity))
            {
                WriteSection(Severity, Color);
            }
            Console.WriteLine("\b: " + Message);
        }
        static void WriteSection(string Message, ConsoleColor Color)
        {
            Console.ForegroundColor = Color;
            Console.Write("[{0}] ", Message);
            Console.ResetColor();
        }
    }
}
