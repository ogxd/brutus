using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace brutus.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class JobsController : ControllerBase
    {
        private readonly Brutus _brutus;

        public JobsController(Brutus brutus)
        {
            _brutus = brutus;
        }

        [HttpGet]
        public IActionResult Get()
        {
            StringBuilder strbldr = new StringBuilder();
            foreach (var pair in _brutus.Jobs)
            {
                strbldr.AppendLine($"Job {pair.Item1.Split('§')[1]}");
                strbldr.AppendLine($"Channel {pair.Item1.Split('§')[0]}");
                strbldr.AppendLine($"- Status: {(pair.Item2.Paused ? "paused" : "running")}");
                strbldr.AppendLine($"- Url: {pair.Item2.Url}");
                strbldr.AppendLine($"- Includes: {pair.Item2.Includes}");
                strbldr.AppendLine($"- Excludes: {pair.Item2.Exludes}");
                strbldr.AppendLine($"- Delay: {pair.Item2.Delay} ms");
                strbldr.AppendLine($"- Last Invokation: {pair.Item2.LastInvokation}");
                strbldr.AppendLine($"- Last Error: {pair.Item2.LastError?.ToString() ?? "no error"}");
                strbldr.AppendLine("");
            }
            return Content(strbldr.ToString());
        }

        [Route("error")]
        public IActionResult Error() => Problem();
    }
}