using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UrlShortener.Services; // Adjust namespace as needed
using Microsoft.AspNetCore.Cors;

namespace UrlShortener.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly EmailService _emailService;
        private readonly ILogger<EmailController> _logger;

        public EmailController(EmailService emailService, ILogger<EmailController> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        // Allow CORS for your client app and personal website
        [HttpPost("send")]
        [EnableCors("ClientAndPersonalWebsitePolicy")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var email_template = $"<h2>New message from {request.Name}</h2><p>{request.Body}</p><p>Contact Email: {request.Email}</p>";
                await _emailService.SendEmailAsync(request.Subject, email_template, request.Email);
                return Ok(new { message = "Email sent successfully." });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error sending email");
                return StatusCode(500, "An error occurred while sending the email.");
            }
        }
    }

    public class EmailRequest
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Email { get; set; }
    }
}