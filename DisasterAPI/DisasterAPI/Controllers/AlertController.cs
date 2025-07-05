using DisasterAPI.Connect;
using DisasterAPI.Models;
using DisasterAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisasterAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlertController : ControllerBase
    {
        private static List<Alert> alerts = new();
        private readonly AlertServices _alertService;
        private readonly ILogger<AlertController> _logger;
        private readonly DisasterDbContext _context;

        public AlertController(AlertServices alertService, ILogger<AlertController> logger, DisasterDbContext context)
        {
            _alertService = alertService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendAlerts([FromBody] List<DisasterRiskReport> highRiskReports)
        {
            var alertsToAdd = new List<Alert>();

            foreach (var report in highRiskReports.Where(r => r.AlertTriggered))
            {
                var alert = new Alert
                {
                    AlertID = Guid.NewGuid().ToString(),
                    RegionID = report.RegionID,
                    DisasterType = report.DisasterType,
                    RiskLevel = report.RiskLevel,
                    AlertMessage = $"ควรระมัดระวัง",
                    Timestamp = DateTime.Now,
                    Email = "drdirectt@gmail.com",
                    CreateAt = DateTime.UtcNow
                };

                alertsToAdd.Add(alert);

                _logger.LogWarning("ส่งข้อความ : {@Alert}", alert);

                await _alertService.SendEmailAsync(
                       alert.Email,
                       "แจ้งเตือนภัยพิบัติ (Disaster Alert)",
                       $"บริเวณ: {alert.RegionID}\r\n" +
                       $"ประเภทภัยพิบัติ: {alert.DisasterType}\r\n" +
                       $"ระดับความเสี่ยง: {alert.RiskLevel}\r\n" +
                       $"ข้อความแจ้งเตือน: {alert.AlertMessage}\r\n" +
                       $"Timestamp: {alert.Timestamp}\r\n"
                    );
            }

            if (alertsToAdd.Any())
            {
                await _context.Alerts.AddRangeAsync(alertsToAdd);
                await _context.SaveChangesAsync();
            }

            return Ok(alertsToAdd);
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts()
        {
            var alerts = await _context.Alerts
                .OrderByDescending(a => a.Timestamp)
                .Take(100)
                .ToListAsync();

            return Ok(alerts);
        }
    }
}
