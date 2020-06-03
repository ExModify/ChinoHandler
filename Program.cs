﻿using System;
using System.Diagnostics;
using System.IO;
using ChinoHandler.Models;
using ChinoHandler.Modules;
using Newtonsoft.Json;
using System.Linq;

namespace ChinoHandler
{
    public class Program
    {
        public static string ConfigPath = "Config.json";
        public static Config Config;
        public static WebhookManager Manager;
        public static BotHandler BotHandler;
        public static Updater Updater;
        public static MenuHandler MenuHandler;

        public static event Action BotUpdate;
        public static event Action HandlerUpdate;

        static bool Running { get; set; } = true;

        static bool ShowMenu { get; set; }

        static void Main(string[] args)
        {
            Logger.Log("Chino-chan handler loading...");

            Logger.Log("Loading config...");
            Config = LoadConfig(ConfigPath);
            Logger.Log("Config loaded!", Color: ConsoleColor.Green);

            Logger.Log("Checking libraries...");
            if (!CheckLibraries())
            {
                Logger.Log("Please install the libraries / runtimes mentioned above! Press enter to exit!", Color: ConsoleColor.Red);
                Console.ReadLine();
                Environment.Exit(1);
            }
            Logger.Log("Checking libraries done!", Color: ConsoleColor.Green);

            Logger.Log("Initializing Webhook manager...");
            Manager = new WebhookManager(Config);
            Logger.Log("Webhook manager initialized!", Color: ConsoleColor.Green);

            Logger.Log("Initializing Bot handler...");
            BotHandler = new BotHandler(Config);
            Logger.Log("Handler initialized!", Color: ConsoleColor.Green);

            Logger.Log("Initializing updater...");
            Updater = new Updater(Config);
            Logger.Log("Updater initialized!", Color: ConsoleColor.Green);

            
            Logger.Log("Initializing menu...");
            MenuHandler = new MenuHandler();
            MenuHandler.Add("Start", () =>
            {
                BotHandler.Start();
            });
            MenuHandler.Add("Exit", () =>
            {
                BotHandler.Quit();
                Environment.Exit(0);
            });
            Console.Clear();
            
            Logger.Log("Starting Webhook manager...");
            if (Manager.Start())
            {
                Logger.Log("Webhook manager successfully started!", Color: ConsoleColor.Green);
                Console.Clear();
            }
            Console.WriteLine();
            HandleCommands();
        }

        static void HandleCommands()
        {
            ShowMenu = !Config.BypassMenu;
            BotHandler.Exit += () =>
            {
                ShowMenu = true;
            };
            while (Running)
            {
                if (ShowMenu)
                {
                    MenuHandler.Display();
                }
                else
                {
                    BotHandler.Start();
                    ShowMenu = false;
                }
                string input = Console.ReadLine();

                HandleCommand(input);
            }
        }

        public static void HandleCommand(string input)
        {
            if (ShowMenu) // handle menu options
                {
                    MenuOption option = MenuHandler.GetOption(input);
                    if (option == null)
                    {
                        Console.Clear();
                        System.Console.WriteLine("Unknown command!");
                    }
                    else
                    {
                        Console.Clear();
                        option.Action.Invoke();
                    }
                }
                else // handle generic commands / send to bot
                {
                    if (input.ToLower() == "menu")
                    {
                        ShowMenu = true;
                    }
                    else
                    {
                        BotHandler.Send(input);
                    }

                }
        }
        public static long GetMemoryUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            return currentProcess.NonpagedSystemMemorySize64 + currentProcess.PagedMemorySize64;
        }
        public static void TriggerNewUpdateEvent(bool IsBot)
        {
            if (IsBot)
            {
                BotUpdate?.Invoke();
            }
            else
            {
                HandlerUpdate?.Invoke();
            }
        }
        static bool CheckLibraries()
        {
            bool r = true;
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = "dotnet",
                ArgumentList = { "--list-sdks" },
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            Process p = Process.Start(info);
            p.WaitForExit();
            string[] data = p.StandardOutput.ReadToEnd().Trim().Split("\n").Select(t => t.Trim()).ToArray();
            bool foundSDK = false;
            foreach (string line in data)
            {
                if (line.StartsWith("3.1"))
                    foundSDK = true;
            }
            if (!foundSDK)
            {
                Logger.Log("Please install .NET Core 3.1 SDK!");
                r = false;
            }
            return r;
        }
        static Config LoadConfig(string Path)
        {
            Config c;
            if (!File.Exists(Path))
            {
                c = new Config();
                c.Path = Path;
                c.SaveConfig();
                return c;
            }
            c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path));
            c.Path = Path;
            return c;
        }
    }
}
