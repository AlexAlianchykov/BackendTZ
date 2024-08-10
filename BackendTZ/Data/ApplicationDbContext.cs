using BackendTZ.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendTZ.Data
{
    public class ApplicationDbContext: DbContext 
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)  // создаём предустановку для хранения определённого типа сущностей
            : base(options)
        { 
        
        }
        public DbSet<User> Users { get; set; } // коллекция сущностей типа User, которая представляет табл в бд 
    }
}
