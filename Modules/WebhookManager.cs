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
                    string sentSecret = request.Headers.Get("X-Hub-Signature");
                    if (sentSecret == Secret)
                    {
                        int length = (int)request.ContentLength64;
                        byte[] data = new byte[length];
                        request.InputStream.Read(data, 0, length);
                        string content = Encoding.UTF8.GetString(data, 0, length);
                        if (content.Contains("commits"))
                        {
                            Program.TriggerNewUpdateEvent(content.Contains(@"""name"": ""ChinoHandler"""));
                        }
                    }
                }
            }
        }
        

    }
}
