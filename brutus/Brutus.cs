using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace brutus
{
    public class Brutus : IHostedService
    {
        private static string BRUTUS_TOKEN = Environment.GetEnvironmentVariable("BRUTUS_TOKEN");
        private static Version VERSION = new Version(1, 0, 0);

        private ConcurrentDictionary<string, Job> _jobs;
        private DiscordSocketClient _client;

        public Brutus()
        {

        }

        public IEnumerable<(string, Job)> Jobs => _jobs.Select(x => (x.Key, x.Value));

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("starting");

            _jobs = new ConcurrentDictionary<string, Job>();

            _client = new DiscordSocketClient();

            Console.WriteLine($"login in (Token={BRUTUS_TOKEN})");

            await _client.LoginAsync(TokenType.Bot, BRUTUS_TOKEN);

            Console.WriteLine("starting");

            await _client.StartAsync();

            Console.WriteLine("running !");

            _client.MessageReceived += OnMessageReceived;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _client.StopAsync();
            _client.Dispose();
        }

        private async Task OnMessageReceived(SocketMessage message)
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

                    case "version":
                        await message.Channel.SendMessageAsync($"{VERSION}");
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

                    case "save":
                        var serializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .Build();
                        string channelIdStr = message.Channel.Id.ToString();
                        string yaml = serializer.Serialize(_jobs.Where(x => x.Key.Split('§')[0] == channelIdStr).ToDictionary(x => x.Key, x => x.Value));
                        var bytes = Encoding.UTF8.GetBytes(yaml);
                        MemoryStream ms = new MemoryStream(bytes);
                        await message.Channel.SendFileAsync(ms, $"{channelIdStr}_save.yaml");
                        break;

                    case "load":
                        var attachments = message.Attachments;
                        WebClient myWebClient = new WebClient();
                        string url = attachments.ElementAt(0).Url;
                        byte[] buffer = myWebClient.DownloadData(url);
                        string download = Encoding.UTF8.GetString(buffer);
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(UnderscoredNamingConvention.Instance)
                            .Build();
                        var obj = deserializer.Deserialize<Dictionary<string, Job>>(download);
                        _jobs = new ConcurrentDictionary<string, Job>(obj.Select(x => new KeyValuePair<string, Job>(message.Channel.Id + '§' + x.Key, x.Value)));
                        break;

                    case "jobs":
                        if (_jobs.Count == 0)
                        {
                            await message.Channel.SendMessageAsync($"There are no jobs running. Add a job with `!brutus job add MY_JOB_NAME` (no spaces)");
                        }
                        foreach (var pair in _jobs)
                        {
                            await message.Channel.SendMessageAsync($"**Job {pair.Key.Split('§')[1]}**\n" +
                                $"```- Status: {(pair.Value.Paused ? "paused" : "running")}\n" +
                                $"- Url: {pair.Value.Url}\n" +
                                $"- Includes: {pair.Value.Includes}\n" +
                                $"- Excludes: {pair.Value.Exludes}\n" +
                                $"- Delay: {pair.Value.Delay} ms\n" +
                                $"- Last Invokation: {pair.Value.LastInvokation}\n" +
                                $"- Last Error: {pair.Value.LastError?.ToString() ?? "no error"}```".TruncateIfTooLong(1900));
                        }
                        break;

                    case "job":
                        if (split.Length < 3)
                            throw new Exception("job requires at least 1 argument");

                        Job job;

                        string jobName = split[3];
                        string fullJobName = message.Channel.Id + "§" + jobName;

                        switch (split[2].ToLower())
                        {
                            case "add":
                                _jobs.TryAdd(fullJobName, job = new Job(message.Channel.Id));
                                job.Triggered += Job_Triggered;
                                break;

                            case "remove":
                                _jobs.TryRemove(fullJobName, out job);
                                break;

                            case "set":
                                _jobs.TryGetValue(fullJobName, out job);
                                // Recombine value
                                switch (split[4].ToLower())
                                {
                                    case "url":
                                        job.Url = split[5];
                                        break;
                                    case "includes":
                                        job.Includes = string.Join(' ', split[5..]); ;
                                        break;
                                    case "excludes":
                                        job.Exludes = string.Join(' ', split[5..]); ;
                                        break;
                                    case "delay":
                                        job.Delay = int.Parse(split[5]);
                                        break;
                                }
                                break;

                            case "pause":
                                _jobs.TryGetValue(fullJobName, out job);
                                job.Pause();
                                break;

                            case "start":
                                _jobs.TryGetValue(fullJobName, out job);
                                job.Start();
                                break;

                            case "dump":
                                _jobs.TryGetValue(fullJobName, out job);
                                job.RequestDump(async (content) =>
                                {
                                    var bytes = Encoding.UTF8.GetBytes(content);
                                    MemoryStream ms = new MemoryStream(bytes);
                                    await message.Channel.SendFileAsync(ms, $"{jobName}_dump.txt");
                                });
                                break;
                        }

                        await message.Channel.SendMessageAsync("Done!");
                        break;
                }
            }
            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync($"Command failed:\n`{ex}`".TruncateIfTooLong(1900));
            }
        }

        private void Job_Triggered(Job job)
        {
            var channel = _client.GetChannel(job.ChannelId) as IMessageChannel;
            channel.SendMessageAsync("🚨 ALERT 🚨\n" + job.Url.TruncateIfTooLong(1900));
        }
    }
}