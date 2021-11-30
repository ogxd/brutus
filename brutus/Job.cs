using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace brutus
{
    public class Job
    {
        public string Url { get; set; }
        public string Exludes { get; set; }
        public string Includes { get; set; }
        public int Delay { get; set; } = 5000;


        public event Action<Job> Triggered;

        public bool Paused { get; private set; }
        public ulong ChannelId { get; private set; }

        public Exception LastError { get; private set; }
        public DateTime LastInvokation { get; private set; }


        private CancellationTokenSource _cts;
        private WebClient _client;

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

                    if (!string.IsNullOrEmpty(Includes) && !content.Contains(Includes))
                    {
                        Pause();
                        Triggered?.Invoke(this);
                    }

                    if (!string.IsNullOrEmpty(Exludes) && content.Contains(Exludes))
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

                await Task.Delay(Delay);
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