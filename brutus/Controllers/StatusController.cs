using Microsoft.AspNetCore.Mvc;

namespace brutus.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : ControllerBase
    {
        private readonly Brutus _brutus;

        public StatusController(Brutus brutus)
        {
            _brutus = brutus;
        }

        [HttpGet("jobs")]
        public IActionResult Get()
        {
            return Content("Current jobs: " + _brutus.GetJobsCount);
        }

        [Route("/error")]
        public IActionResult Error() => Problem();
    }
}