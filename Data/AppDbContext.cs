using Microsoft.EntityFrameworkCore;
using ASPCoreWebMVC.Models;

namespace ASPCoreWebMVC.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<UploadedImage> UploadedImages { get; set; }
    }
}
