using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
namespace web_services_l1
{
    public class MoviesContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Rating> Ratings { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=movie_lens;user=root");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Movie>(entity =>
            {
                entity.HasKey(e => e.MovieID);
                entity.Property(e => e.Title).IsRequired();
                entity.HasMany(e => e.Genres).WithMany(f => f.Movies);
            });
            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.GenreID);
                entity.Property(e => e.Name).IsRequired();
            });
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserID);
                entity.Property(e => e.Name).IsRequired();
            });
            modelBuilder.Entity<Rating>(entity =>
            {
                entity.HasKey(e => e.RatingID);
                entity.Property(e => e.RatingValue).IsRequired();
                entity.HasOne(e => e.RatingUser);
                entity.HasOne(e => e.RatedMovie);
            }
            );
        }
    }
}

