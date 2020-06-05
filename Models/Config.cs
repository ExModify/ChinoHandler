using System.ComponentModel;
using System.Reflection;
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
        public Credentials RemoteConsoleCredentials { get; set; } = new Credentials();
        public string IPAddress { get; set; } = "";
        public int Port { get; set; } = 2465;
        public bool BypassMenu { get; set; } = false;



        [JsonIgnore]
        public string Path { get; set; }
        public void SaveConfig()
        {
            File.WriteAllText(Path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }


        public bool IsNewConfig()
        {
            return IsNewConfig(this);
        }
        private bool IsNewConfig(object obj)
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (PropertyInfo property in properties)
            {
                object o = property.GetValue(obj);
                if (o == null) return true;
                else if (!Program.NotClass.Contains(property.PropertyType.Name))
                {
                    if (IsNewConfig(o))
                        return true;
                }
            }
            return false;
        }
    }
}
