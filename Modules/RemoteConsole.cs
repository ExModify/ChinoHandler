using ChinoHandler.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChinoHandler.Modules
{
    public class RemoteConsole
    {
        TcpListener listener;
        Thread acceptThread;
        List<RemoteConnection> connections;

        public RemoteConsole(Config Config)
        {
            connections = new List<RemoteConnection>();

            if (!IPAddress.TryParse(Config.IPAddress, out IPAddress address))
            {
                address = Dns.GetHostEntry(string.Empty).AddressList.Where(t => t.AddressFamily == AddressFamily.InterNetwork).First();
            }

            listener = new TcpListener(address, Config.Port);
            acceptThread = new Thread(() =>
            {
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();
                    RemoteConnection connection = new RemoteConnection(client, Config);
                    connection.Authenticated += () =>
                    {
                        connections.Add(connection);
                    };
                    connection.Closed += () =>
                    {
                        if (connections.Contains(connection))
                            connections.Remove(connection);
                    };
                    connection.Begin();
                }
            });
        }

        public void Start()
        {
            try
            {
                listener.Start();
                acceptThread.Start();
                IPEndPoint ep = listener.LocalEndpoint as IPEndPoint;
                Logger.Log($"Remote console started! Local IP: { ep.Address.ToString() }:{ ep.Port }", "RemoteConsole", ConsoleColor.Green);
            }
            catch
            {
                Logger.Log("Couldn't start remote client server! Please run the program as administrator so I can reserve the port binding! If you did so, maybe the port is already taken!", "RemoteConsole", ConsoleColor.Red);
            }
        }

        public void Log(LogMessage Message)
        {
            for (int i = 0; i < connections.Count; i++)
            {
                if (!connections[i].SendMessage(JsonConvert.SerializeObject(Message)))
                {
                    connections[i].Close();
                    connections.RemoveAt(i);
                    i--;
                }
            }
        }
    }
}
