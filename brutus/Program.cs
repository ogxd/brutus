using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace brutus
{
    class Program
    {
        private static string BRUTUS_TOKEN = Environment.GetEnvironmentVariable("BRUTUS_TOKEN");
        private const ulong NOTIFICATION_CHANNEL_ID = 913921054177624188;

        private static ConcurrentDictionary<string, Job> jobs;
        private static DiscordSocketClient client;

        static void Main(string[] args)
        {
            Console.WriteLine("starting");
            Run().Wait();
        }

        private static async Task Run()
        {
            jobs = new ConcurrentDictionary<string, Job>();

            client = new DiscordSocketClient();

            Console.WriteLine("login in");

            await client.LoginAsync(TokenType.Bot, BRUTUS_TOKEN);

            Console.WriteLine("starting");

            await client.StartAsync();

            Console.WriteLine("running !");

            client.MessageReceived += OnMessageReceived;

            await Task.Delay(Timeout.Infinite); // Await until close
        }

        private static async Task OnMessageReceived(SocketMessage message)
        {
            if (!message.Content.StartsWith("!brutus "))
                return;

            Console.WriteLine("command received");

            try
            {
                var split = message.Content.Split(' ');
                if (split.Length < 2)
                    throw new Exception("brutus requires at least 1 argument");

                switch (split[1].ToLower())
                {
                    case "status":
                        await message.Channel.SendMessageAsync("I'm alive! ✨");
                        break;

                    case "jobs":
                        if (jobs.Count == 0)
                        {
                            await message.Channel.SendMessageAsync($"There are no jobs running. Add a job with !brutus job add MY_JOB_NAME (no spaces)");
                        }
                        foreach (var pair in jobs)
                        {
                            await message.Channel.SendMessageAsync($"Job '{pair.Key}': {(pair.Value.Paused ? "paused" : "running")} {pair.Value.LastError?.ToString()}");
                        }
                        break;

                    case "job":
                        if (split.Length < 3)
                            throw new Exception("job requires at least 1 argument");

                        Job job;

                        switch (split[2].ToLower())
                        {
                            case "add":
                                jobs.TryAdd(split[3], job = new Job());
                                job.Triggered += Job_Triggered;
                                break;

                            case "remove":
                                jobs.TryRemove(split[3], out job);
                                break;

                            case "set":
                                jobs.TryGetValue(split[3], out job);
                                switch (split[4].ToLower())
                                {
                                    case "url":
                                        job.url = split[4];
                                        break;
                                    case "includes":
                                        job.includes = split[4];
                                        break;
                                    case "excludes":
                                        job.exludes = split[4];
                                        break;
                                    case "delay":
                                        job.delay = int.Parse(split[4]);
                                        break;
                                }
                                break;

                            case "pause":
                                jobs.TryGetValue(split[3], out job);
                                job.Pause();
                                break;

                            case "start":
                                jobs.TryGetValue(split[3], out job);
                                job.Start();
                                break;
                        }

                        await message.Channel.SendMessageAsync("Done!");
                        break;
                }
            }
            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync("Command failed: " + ex);
            }
        }

        private static void Job_Triggered(Job job)
        {
            var channel = client.GetChannel(NOTIFICATION_CHANNEL_ID) as IMessageChannel;
            channel.SendMessageAsync("ALERT! => " + job.url);
        }
    }
}
