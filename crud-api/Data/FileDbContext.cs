using Microsoft.EntityFrameworkCore;
using crud_api.models;
using crud_api.common;
namespace crud_api.Data
{
    public class FileDbContext(DbContextOptions<FileDbContext> options) : DbContext(options)
    {
        public DbSet<models.File> Files {get;set;}
        public DbSet<User> Users {get; set;}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<models.File>()
            .Property(u => u.FileId)
            .ValueGeneratedOnAdd();

             modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedOnAdd();

            modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        }
    }
}