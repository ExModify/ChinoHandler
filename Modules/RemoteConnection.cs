using System.IO;
using System.Threading;
using System;
using System.Net.Sockets;
using ChinoHandler.Models;
using Newtonsoft.Json;

namespace ChinoHandler.Modules
{
    class RemoteConnection
    {
        TcpClient TcpClient { get; set; }

        public event Action Authenticated;
        public event Action Closed;


        Thread ListenThread;
        StreamWriter Writer;
        StreamReader Reader;

        public RemoteConnection(TcpClient Client, Config Config)
        {
            TcpClient = Client;
            NetworkStream stream = Client.GetStream();
            Writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
            Reader = new StreamReader(stream);

            ListenThread = new Thread(() =>
            {
                while (TcpClient.Connected)
                {
                    string line = Reader.ReadLine();
                    if (line == null || !line.Contains(':'))
                    {
                        Close();
                        Closed?.Invoke();
                    }
                    else
                    {
                        string[] split = line.Split(':', 2);
                        string type = split[0];
                        string data = split[1];

                        switch(type)
                        {
                            case "credentials":
                                try
                                {
                                    if (Login(data))
                                    {
                                        Authenticated?.Invoke();
                                        if (!SendMessage("Successful_Auth", true))
                                            break;
                                        
                                        for (int i = 0; i < Logger.Logs.Count; i++)
                                        {
                                            if (!SendMessage(JsonConvert.SerializeObject(Logger.Logs[i]), true))
                                                break;
                                        }
                                        continue;
                                    }
                                }
                                catch { }

                            break;
                            default:
                                if (!SendMessage("@_@", true))
                                    break;
                            break;
                        }
                    }
                }
            });
        }

        public void Begin()
        {
            ListenThread.Start();
            SendMessage("Credentials", true);
        }

        public bool SendMessage(string Message, bool CloseOnFail = false)
        {
            try
            {
                Writer.WriteLine(Message);
                return true;
            }
            catch
            {
                if (CloseOnFail)
                {
                    Close();
                    Closed?.Invoke();
                }
                return false;
            }
        }

        public void Close()
        {
            try
            {
                TcpClient.Close();
            }
            catch { }
        }

        private bool Login(string Content)
        {
            Credentials response = JsonConvert.DeserializeObject<Credentials>(Content);

            Credentials credentials = Program.Config.RemoteConsoleCredentials;
            if (credentials.Username == response.Username && credentials.Password == response.Password)
            {
                return true;
            }
            return false;
        }
        
    }
}