using SendGrid.Helpers.Mail;
using SendGrid;

namespace DisasterAPI.Services
{
    public class AlertServices
    {
        private readonly IConfiguration _config;
        private readonly ILogger<AlertServices> _logger;

        public AlertServices(IConfiguration config, ILogger<AlertServices> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            var apiKey = _config["SendGrid:ApiKey"];
            var fromEmail = _config["SendGrid:FromEmail"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogError("Please Add ApiKey.");
                throw new Exception("API key is not configured.");
            }
            if (string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogError("Please Add FromEmail.");
                throw new Exception("FromEmail is not configured.");
            }

            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(fromEmail, "Disaster Alert");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, message, message);
            msg.ReplyTo = new EmailAddress("drdirectt@gmail.com", "No Reply");

            try
            {
                var response = await client.SendEmailAsync(msg);
                var responseBody = await response.Body.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("แจ้งเตือนไปยังอีเมล {ToEmail} สำเร็จ. สถานะ: {StatusCode}", toEmail, response.StatusCode);
                }
                else
                {
                    _logger.LogError("เกิดข้อผิดพลาด {ToEmail}. สถานะ: {StatusCode}", toEmail, response.StatusCode);
                    throw new Exception($"เกิดข้อผิดพลาด: {response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                throw;
            }
        }
    }
}
