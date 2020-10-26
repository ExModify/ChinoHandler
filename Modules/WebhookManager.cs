using System.Linq;
using System.Security.Cryptography;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ChinoHandler.Models;
using System.Net.Sockets;

namespace ChinoHandler.Modules
{
    public class WebhookManager
    {
        TcpListener Listener;

        CancellationTokenSource Source;
        CancellationToken Token;
        public string Secret;
        
        public WebhookManager(Config Config)
        {
            int port = int.Parse(string.Join("", Config.WebhookUrl.Split(':').Last().Where(t => char.IsDigit(t))));

            Listener = new TcpListener(IPAddress.Any, port);

            Secret = Config.WebhookSecret;
            Source = new CancellationTokenSource();
            Token = Source.Token;
        }
        public bool Start()
        {
            try
            {
                Listener.Start();
                Task t = new Task(Handle, Token);
                t.Start();
                return true;
            }
            catch
            {
                Logger.Log("Couldn't start webhook manager!" + Environment.NewLine
                        + "- Please run the program as Administrator, " +
                            "becuase it doesn't have access to reserve the webhook link." + Environment.NewLine
                        + "- Optionally, if you don't want auto-update from Github, you can skip this step.",
                    "Webhook manager", ConsoleColor.Red);
                return false;
            }
            
        }
        public void Stop()
        {
            Source.Cancel();
            Listener.Stop();
        }

        private void Handle()
        {
            while (!Token.IsCancellationRequested)
            {
                TcpClient client = Listener.AcceptTcpClient();

                StreamReader reader = new StreamReader(client.GetStream());
                StreamWriter writer = new StreamWriter(client.GetStream());

                string content = GetData(reader, Secret);
                Respond(writer);

                writer.Close();
                client.Close();

                if (content != null && content.Contains("commits"))
                {
                    bool handler = content.Contains("\"name\":\"ChinoHandler\"") || content.Contains("\"name\": \"ChinoHandler\"");
                    Program.TriggerNewUpdateEvent(!handler);
                }
            }
        }

        
        private string GetData(StreamReader reader, string secret)
        {
            int length = 0;
            string line = reader.ReadLine();

            if (!line.StartsWith("POST"))
            {
                return null;
            }

            string hash = "";

            do
            {
                line = reader.ReadLine();
                Console.WriteLine(line);
                if (line.ToLower().StartsWith("content-length:"))
                {
                    length = int.Parse(line.Split(':')[1].Trim());
                }

                if (line.ToLower().StartsWith("x-hub-signature:"))
                {
                    hash = line.Split('=', 2)[1].ToLower();
                }
            }
            while (!string.IsNullOrWhiteSpace(line));

            char[] buffer = new char[length];

            reader.ReadBlock(buffer, 0, length);

            string content = string.Join("", buffer);

            if (GetHash(secret, content) != hash)
            {
                return null;
            }

            return content;
        }
        
        private static void Respond(StreamWriter writer)
        {
            string[] response = new string[13]
            {
                "HTTP/1.1 200 OK",
                $"Date: " + DateTime.Now.ToUniversalTime().ToString("r"),
                "Server: Chino-chan",
                "Status: 200 Accepted",
                "Last-Modified: " + DateTime.Now.ToUniversalTime().ToString("r"),
                "Content-Length: 2",
                "Content-Type: text/html;charset=utf-8",
                "Connection: keep-alive",
                "X-Content-Type-Options: nosniff",
                "X-Frame-Options: SAMEORIGIN",
                "X-Xss-Protection: 1; mode=block",
                "",
                "OK"
            };
            foreach (string line in response)
            {
                writer.WriteLine(line);
            }
            writer.Flush();
        }

        private static string GetHash(string Secret, string Content)
        {
            string hashString;
            byte[] payloadBytes = Encoding.ASCII.GetBytes(Content);
            byte[] secretBytes = Encoding.ASCII.GetBytes(Secret);
            using (var hmSha1 = new HMACSHA1(secretBytes))
            {
                byte[] hash = hmSha1.ComputeHash(payloadBytes);

                StringBuilder sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    sb.AppendFormat("{0:x2}", b);
                }

                hashString = sb.ToString();
            }

            return hashString.ToLower();
        }
    }
}
