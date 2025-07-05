using DisasterAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DisasterAPI.Connect
{
    public class DisasterDbContext : DbContext
    {
        public DisasterDbContext(DbContextOptions<DisasterDbContext> options) : base(options){ }
        public DbSet<Regions> Regions { get; set; }
        public DbSet<AlertSetting> AlertSettings { get; set; }
        public DbSet<Alert> Alerts { get; set; }
    }
}
