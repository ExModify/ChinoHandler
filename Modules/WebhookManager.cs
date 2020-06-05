using System.Security.Cryptography;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using ChinoHandler.Models;

namespace ChinoHandler.Modules
{
    public class WebhookManager
    {
        HttpListener Listener;

        CancellationTokenSource Source;
        CancellationToken Token;
        public string Secret;
        
        public WebhookManager(Config Config)
        {
            Listener = new HttpListener();
            Listener.Prefixes.Add(Config.WebhookUrl);


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
                HttpListenerContext context = Listener.GetContext();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod != "POST")
                {
                    Console.WriteLine("Not POST!");
                    HttpListenerResponse response = context.Response;

                    string content = 
                    @"<html>
                        <head>
                            <script>
                                document.location = ""https://chino.exmodify.com/"";
                            </script>
                        </head>
                        <body>
                        </body>
                    </html>";

                    byte[] buffer = Encoding.UTF8.GetBytes(content);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                else
                {
                    int length = (int)request.ContentLength64;
                    byte[] data = new byte[length];
                    request.InputStream.Read(data, 0, length);
                    string content = Encoding.UTF8.GetString(data, 0, length);

                    string sentSecret = request.Headers.Get("X-Hub-Signature").Substring(5);
                    var secret = Encoding.ASCII.GetBytes(Secret);
                    var payloadBytes = Encoding.ASCII.GetBytes(content);
                    string hashString;

                    using (var hmSha1 = new HMACSHA1(secret))
                    {
                        byte[] hash = hmSha1.ComputeHash(payloadBytes);

                        StringBuilder sb = new StringBuilder(hash.Length * 2);

                        foreach (byte b in hash)
                        {
                            sb.AppendFormat("{0:x2}", b);
                        }

                        hashString = sb.ToString();
                    }
                    if (hashString == sentSecret)
                    {
                        if (content.Contains("commits"))
                        {
                            bool handler = content.Contains("\"name\": \"ChinoHandler\"");
                            Console.WriteLine(handler);
                            Program.TriggerNewUpdateEvent(!handler);
                        }
                        context.Response.StatusCode = 200;
                        context.Response.Close();
                    }
                    else
                    {
                        Console.WriteLine("Mismatch - \"{0}\" - \"{1}\"", sentSecret, hashString);
                        context.Response.StatusCode = 500;
                        context.Response.Close();
                    }
                }
            }
        }
        

    }
}
