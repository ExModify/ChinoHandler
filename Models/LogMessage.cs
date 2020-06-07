using System;
using Newtonsoft.Json;

namespace ChinoHandler.Models
{
    public class LogMessage
    {
        public string Severity { get; set; }

        [JsonProperty("Type")]
        public string Module { get; set; }
        public string Message { get; set; }

        public ConsoleColor Color { get; set; }
        
        [JsonProperty("Date")]
        public DateTime Time { get; set; }
    }
}