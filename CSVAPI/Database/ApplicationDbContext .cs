using CSVAPI.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CSVAPI.Database
{
  
        public class ApplicationDbContext : DbContext
        {
            public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

            public DbSet<Person> Persons { get; set; }
        }
    
}
