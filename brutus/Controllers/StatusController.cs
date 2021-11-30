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

        [HttpGet]
        public IActionResult Get()
        {
            return Content("Alive 🤠");
        }

        [Route("error")]
        public IActionResult Error() => Problem();
    }
}