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

            Console.WriteLine($"login in (Token={BRUTUS_TOKEN})");

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

                    case "help":
                        await message.Channel.SendMessageAsync($"**Here are some commands:**");
                        await message.Channel.SendMessageAsync($"`!brutus status` *check brutus status*");
                        await message.Channel.SendMessageAsync($"`!brutus jobs` *check brutus registered jobs*");
                        await message.Channel.SendMessageAsync($"`!brutus job add MY_JOB` *add and start a new job*");
                        await message.Channel.SendMessageAsync($"`!brutus job remove MY_JOB` *removes a job*");
                        await message.Channel.SendMessageAsync($"`!brutus job set MY_JOB url http://google.fr` *changes the url of a job*");
                        await message.Channel.SendMessageAsync($"`!brutus job set MY_JOB includes MY EXPRESSION` *changes the includes check of a job*");
                        await message.Channel.SendMessageAsync($"`!brutus job pause MY_JOB` *pauses a job*");
                        await message.Channel.SendMessageAsync($"`!brutus job start MY_JOB` *start a paused job*");
                        break;

                    case "jobs":
                        if (jobs.Count == 0)
                        {
                            await message.Channel.SendMessageAsync($"There are no jobs running. Add a job with `!brutus job add MY_JOB_NAME` (no spaces)");
                        }
                        foreach (var pair in jobs)
                        {
                            await message.Channel.SendMessageAsync($"**Job '{pair.Key.Split('§')[1]}**'\n" +
                                $"- Status: {(pair.Value.Paused ? "paused" : "running")}\n" +
                                $"- Url: `{pair.Value.url}`\n" +
                                $"- Includes: `{pair.Value.includes}`\n" +
                                $"- Excludes: `{pair.Value.exludes}`\n" +
                                $"- Delay: {pair.Value.delay} ms\n" +
                                $"- Last Invokation: {pair.Value.LastInvokation}\n" +
                                $"- Last Error: `{pair.Value.LastError?.ToString() ?? "no error"}`".TruncateIfTooLong(1900));
                        }
                        break;

                    case "job":
                        if (split.Length < 3)
                            throw new Exception("job requires at least 1 argument");

                        Job job;

                        string jobName = message.Channel.Id + "§" + split[3];

                        switch (split[2].ToLower())
                        {
                            case "add":
                                jobs.TryAdd(jobName, job = new Job(message.Channel.Id));
                                job.Triggered += Job_Triggered;
                                break;

                            case "remove":
                                jobs.TryRemove(jobName, out job);
                                break;

                            case "set":
                                jobs.TryGetValue(jobName, out job);
                                // Recombine value
                                var value = string.Join(' ', split[5..-1]);
                                switch (split[4].ToLower())
                                {
                                    case "url":
                                        job.url = split[5];
                                        break;
                                    case "includes":
                                        job.includes = split[5];
                                        break;
                                    case "excludes":
                                        job.exludes = split[5];
                                        break;
                                    case "delay":
                                        job.delay = int.Parse(split[5]);
                                        break;
                                }
                                break;

                            case "pause":
                                jobs.TryGetValue(jobName, out job);
                                job.Pause();
                                break;

                            case "start":
                                jobs.TryGetValue(jobName, out job);
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
            var channel = client.GetChannel(job.ChannelId) as IMessageChannel;
            channel.SendMessageAsync("🚨 ALERT 🚨\n" + job.url.TruncateIfTooLong(1900));
        }
    }

    public static class Extensions
    {
        public static string TruncateIfTooLong(this string str, int THRESHOLD)
        {
            if (!string.IsNullOrEmpty(str) && str.Length > THRESHOLD)
                return str.Substring(0, THRESHOLD) + "...";
            return str;
        }
    }
}
