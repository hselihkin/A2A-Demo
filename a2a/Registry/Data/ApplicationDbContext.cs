using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Registry.Models;

namespace Registry.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<Servers> Servers { get; set; }
        public DbSet<History> History { get; set; }
        public DbSet<ApiKey> Tokens { get; set; }
    }
}