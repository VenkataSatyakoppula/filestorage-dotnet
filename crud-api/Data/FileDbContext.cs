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

            List<User> users =
            [
                new User
                {
                    Id =1,
                    Name="Venkata",
                    Password= Utilities.ComputeSHA256("1234"),
                    Email="Venkata@test.com"
                },
                new User
                {
                    Id =2,
                    Name="Satya",
                    Password= Utilities.ComputeSHA256("1234"),
                    Email="satya@test.com"
                },
                new User
                {
                    Id =3,
                    Name="test",
                    Password=Utilities.ComputeSHA256("1234"),
                    Email="test@test.com"
                },
                new User
                {
                    Id =4,
                    Name="test2",
                    Password=Utilities.ComputeSHA256("1234"),
                    Email="test2@test.com"
                }
            ];

            List<models.File> files = [
                new models.File {
                    FileId = 1,
                    FileName = "Random_Turtle.jpg",
                    FilePath = "storage/Random_Turtle.jpg",
                    UserId = 1
                }
            ];
        modelBuilder.Entity<User>().HasData(users);
        modelBuilder.Entity<models.File>().HasData(files);
        }
    }
}