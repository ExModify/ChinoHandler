using System;

namespace ChinoHandler.Models
{
    public class ChinoResponse
    {
        public string Module { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public ConsoleColor Color { get; set; }
    }
}