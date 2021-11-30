using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace brutus
{
    public class Job
    {
        public string Url { get; set; }

        public string Exludes { get; set; }

        public string Includes { get; set; }

        public int Delay { get; set; } = 5000;


        public event Action<Job> Triggered;

        [YamlIgnore]
        public ulong ChannelId { get; set; }

        [YamlIgnore]
        public bool Paused { get; private set; }

        [YamlIgnore]
        public Exception LastError { get; private set; }

        [YamlIgnore]
        public DateTime LastInvokation { get; private set; }


        private CancellationTokenSource _cts;
        private WebClient _client;

        public Job()
        {
            _client = new WebClient();
            Start();
        }

        public Job(ulong channelId)
        {
            ChannelId = channelId;
            _client = new WebClient();
            Start();
        }

        public void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                throw new Exception("job is already started");

            _cts = new CancellationTokenSource();

            var task = StartInternalAsync();
        }

        private async Task StartInternalAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    LastInvokation = DateTime.Now;
                    var content = await _client.DownloadStringTaskAsync(Url);

                    if (_dumpAsyncCallback != null && !string.IsNullOrEmpty(content))
                    {
                        await _dumpAsyncCallback(content);
                        _dumpAsyncCallback = null;
                    }

                    content = content.ToLowerInvariant();

                    if (!string.IsNullOrEmpty(Includes) && !content.Contains(Includes.ToLowerInvariant()))
                    {
                        Pause();
                        Triggered?.Invoke(this);
                    }

                    if (!string.IsNullOrEmpty(Exludes) && content.Contains(Exludes.ToLowerInvariant()))
                    {
                        Pause();
                        Triggered?.Invoke(this);
                    }

                    LastError = null;
                }
                catch(Exception err)
                {
                    LastError = err;
                }

                await Task.Delay(Delay + Extensions.Random.Next(0, Delay) /* Act like it's not a bot */);
            }
        }

        private Func<string, Task> _dumpAsyncCallback;

        public void RequestDump(Func<string, Task> asyncCallback)
        {
            _dumpAsyncCallback = asyncCallback;
        }

        public void Pause()
        {
            if (_cts.IsCancellationRequested)
                throw new Exception("job is already paused");

            _cts.Cancel();
        }
    }
}