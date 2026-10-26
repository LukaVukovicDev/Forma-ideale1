using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Models;

namespace ProductionPlanning.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Proizvodi");

            entity.HasKey(p => p.Id);

            entity.Property(p => p.SifraProizvoda)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(p => p.SifraProizvoda)
                .IsUnique();

            entity.Property(p => p.Naziv)
                .HasMaxLength(200)
                .IsRequired();
        });
    }
}