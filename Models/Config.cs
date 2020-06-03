using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace ChinoHandler.Models
{
    public class Config
    {
        public string BotLocation { get; set; } = "Bot";

        public string WebhookUrl { get; set; } = "http://momo.exmodify.com:8080/";
        public string BotProjectUrl { get; set; } = "https://github.com/ExModify/Chino-chan";
        public string HandlerProjectUrl { get; set; } = "https://github.com/ExModify/ChinoHandler";
        public string TempFolder { get; set; } = "Projects";
        public string WebhookSecret { get; set; }
        
        public bool BypassMenu { get; set; } = false;



        [JsonIgnore]
        public string Path { get; set; }
        public void SaveConfig()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
