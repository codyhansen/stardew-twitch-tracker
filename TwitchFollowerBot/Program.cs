/*
 * Thank you for using my Stardew Twitch Tracker!
 * I wish you the best of streams! 
 * 
 * Have a great day, everyone.
 * -- Cody
 */

using System;
using System.Threading.Tasks;
using System.Threading;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;
using System.Text.Json;
using System.Net.NetworkInformation;
using System.Net;
using System.Linq;

namespace StardewTwitchTracker
{
    class StardewTwitchTracker
    {
        public static TwitchPubSub PubSub;
        public static string channelId;

        public static TcpListener listener;
        public static StreamWriter sw;

        public static int port;
        public static string path;


        public class Properties
        {
            public int Port { get; set; }
            public string Path { get; set; }
        }



        static void Main(string[] args)
        {
            path = $@"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}\StardewTwitchTracker";
            port = GetOpenPort();
            CheckAndCreateDirectory(path, new string[] { "crops.txt" });

            Console.WriteLine("Please enter the Twitch ID of the channel you would like to monitor:");
            channelId = Console.ReadLine();

            while (!channelId.All(char.IsDigit))
            {
                Console.WriteLine("The ID must be a number!");
                Console.WriteLine("Please enter the Twitch ID of the channel you would like to monitor:");
                channelId = Console.ReadLine();
            }

            Properties conn = new Properties
            {
                Port = port,
                Path = path
            };

            using (StreamWriter w = new StreamWriter("properties.json"))
            {
                w.WriteLine(JsonSerializer.Serialize(conn));
            };

            if (CheckRunningProcess("StardewTwitchTrackerUI"))
            {
                Process[] processes = Process.GetProcessesByName("StardewTwitchTrackerUI");
                foreach (var process in processes)
                {
                    process.Kill();
                }
            }

            Process.Start("StardewTwitchTrackerUI.exe");

            listener = new TcpListener(System.Net.IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("Loading graphics...");
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Graphics loaded!");

            NetworkStream stream = client.GetStream();
            sw = new StreamWriter(client.GetStream());

            new StardewTwitchTracker()
                .MainAsync(args)
                .GetAwaiter()
                .GetResult();
        }

        private static bool CheckRunningProcess(string process)
        {
            Process[] processes = Process.GetProcessesByName(process);

            if (processes.Length == 0)
            {
                return false;
            }

            return true;
        }

        private static void CheckAndCreateDirectory(string path, string[] files)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            foreach (var file in files)
            {
                FileStream fs = File.Create($@"{path}\{file}");
                fs.Close();
            }

        }

        private static int GetOpenPort()
        {
            IPGlobalProperties ipGp = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEp = ipGp.GetActiveTcpListeners();

            int openPort = 1000;
            bool foundOpen = false;

            while (!foundOpen)
            {
                foundOpen = true;

                foreach (var endpoint in ipEp)
                {
                    if (endpoint.Port == openPort)
                    {
                        openPort++;
                        foundOpen = false;
                        break;
                    }
                }
            }

            return openPort;

        }

        private async Task MainAsync(string[] args)
        {
            PubSub = new TwitchPubSub();
            PubSub.OnListenResponse += OnListenResponse;
            PubSub.OnPubSubServiceConnected += OnPubSubServiceConnected;
            PubSub.OnPubSubServiceClosed += OnPubSubServiceClosed;
            PubSub.OnPubSubServiceError += OnPubSubServiceError;

            ListenToFollows(channelId);

            PubSub.Connect();

            await Task.Delay(Timeout.Infinite);
        }

        private void ListenToFollows(string channelId)
        {
            PubSub.OnFollow += PubSub_OnFollow;
            PubSub.ListenToFollows(channelId);
        }

        private void PubSub_OnFollow(object sender, OnFollowArgs e)
        {
            Console.WriteLine($"{e.Username} is now following!");
            sw.WriteLine(e.Username);
            sw.Flush();
        }

        private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine($"{e.Exception.Message}");
        }

        private void OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Console.WriteLine($"Connection closed.");
        }

        private void OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Console.WriteLine($"Listening for follows...");
            PubSub.SendTopics();
        }

        private void OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (!e.Successful)
            {
                Console.WriteLine($"Failed to listen! Response{e.Response}");
            }
        }

    }
}
