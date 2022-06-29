using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.DI;
using BreakVideoLoop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace BreakVideoLoop.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class VideoBreakLoopController : ControllerBase
    {
        private readonly ILogger<VideoBreakLoopController> _logger;
        private readonly IServiceProvider _serviceProvider;

        public VideoBreakLoopController(ILogger<VideoBreakLoopController> logger,
                                        IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        [HttpPost("VideoBreakLoop/{url}")]
        public async Task<IActionResult> Post(string url, [FromBody] EnumLanguage enumLanguage, CancellationToken ct)
        {
            using (var mp4ToWav = Mp4ToWavFactory.GetFactory(enumLanguage, _serviceProvider))
                return Ok(await mp4ToWav.TransformVideo(url));
        }
    }
}