using DisasterAPI.Connect;
using DisasterAPI.Models;
using DisasterAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DisasterAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class RegionsController : ControllerBase
    {
        private readonly DisasterServices _disasterService;
        private readonly ILogger<RegionsController> _logger;
        private readonly DisasterDbContext _context;

        public RegionsController(DisasterServices disasterService, ILogger<RegionsController> logger, DisasterDbContext context)
        {
            _disasterService = disasterService;
            _logger = logger;
            _context = context;
        }

        [HttpPost("regions")]
        public async Task<IActionResult> AddRegion([FromBody] List<Regions> newRegions)
        {
            foreach (var newRegion in newRegions)
            {
                if (await _context.Regions.AnyAsync(r => r.RegionID == newRegion.RegionID))
                    return Conflict($"Region with ID '{newRegion.RegionID}' already exists.");
                _context.Regions.Add(newRegion);
            }
            await _context.SaveChangesAsync();
            return Ok(await _context.Regions.ToListAsync());
        }

        [HttpPost("alert-settings")]
        public async Task<IActionResult> AddAlertSetting([FromBody] List<AlertSetting> settings)
        {
            await _context.AlertSettings.AddRangeAsync(settings);
            await _context.SaveChangesAsync();
            return Ok(await _context.AlertSettings.ToListAsync());
        }

        [HttpGet("disaster-risks")]
        public async Task<IActionResult> GetDisasterRisks()
        {
            try
            {
                var regions = await _context.Regions.ToListAsync();
                var alertSettings = await _context.AlertSettings.ToListAsync();

                if (regions == null || regions.Count == 0)
                {
                    _logger.LogWarning("No regions found when requesting disaster risks.");
                    return NotFound("No regions have been added yet.");
                }

                var results = new List<DisasterRiskReport>();
                foreach (var region in regions)
                {
                    if (region.DisasterTypes == null || region.DisasterTypes.Count == 0)
                    {
                        _logger.LogWarning($"Region {region.RegionID} has no disaster types configured.");
                        continue;
                    }

                    foreach (var type in region.DisasterTypes)
                    {
                        int riskScore = await _disasterService.CalculateRiskScore(region, type);
                        var setting = alertSettings.FirstOrDefault(s => s.RegionID == region.RegionID && s.DisasterType == type);
                        int threshold = setting?.ThresholdScore ?? 70;
                        var riskLevel = _disasterService.GetRiskLevel(riskScore);
                        bool alert = riskScore >= threshold;
                        results.Add(new DisasterRiskReport
                        {
                            RegionID = region.RegionID,
                            DisasterType = type,
                            RiskScore = riskScore,
                            RiskLevel = riskLevel,
                            AlertTriggered = alert
                        });
                    }
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while getting disaster risks.");
                return StatusCode(500, "Error while your request.");
            }
        }
    }
}
