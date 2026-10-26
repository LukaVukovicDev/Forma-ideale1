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

    public DbSet<Machine> Machines => Set<Machine>();

    public DbSet<PlannedProduction> PlannedProductions =>
        Set<PlannedProduction>();

    public DbSet<ProductionRecord> ProductionRecords =>
        Set<ProductionRecord>();

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

        modelBuilder.Entity<Machine>(entity =>
        {
            entity.ToTable("Masine");

            entity.HasKey(machine => machine.Id);

            entity.Property(machine => machine.SifraMasine)
                .HasMaxLength(40)
                .IsRequired();

            entity.HasIndex(machine => machine.SifraMasine)
                .IsUnique();

            entity.Property(machine => machine.Naziv)
                .HasMaxLength(200)
                .IsRequired();
        });

        modelBuilder.Entity<PlannedProduction>(entity =>
        {
            entity.ToTable("PlaniraniProizvodi", table =>
                table.HasCheckConstraint(
                    "CK_PlaniraniProizvodi_PlannedQuantity_Positive",
                    "\"PlannedQuantity\" > 0"));

            entity.HasKey(plan => plan.Id);

            entity.HasIndex(plan => plan.ProductId);
            entity.HasIndex(plan => plan.MachineId);
            entity.HasIndex(plan => plan.PlannedDate);

            entity.HasOne(plan => plan.Product)
                .WithMany()
                .HasForeignKey(plan => plan.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(plan => plan.Machine)
                .WithMany(machine => machine.PlannedProductions)
                .HasForeignKey(plan => plan.MachineId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductionRecord>(entity =>
        {
            entity.ToTable("Uradjeno", table =>
                table.HasCheckConstraint(
                    "CK_Uradjeno_Quantity_Positive",
                    "\"Quantity\" > 0"));

            entity.HasKey(record => record.Id);

            entity.HasIndex(record => record.PlannedProductionId);
            entity.HasIndex(record => record.RecordedAtUtc);

            entity.HasOne(record => record.PlannedProduction)
                .WithMany(plan => plan.ProductionRecords)
                .HasForeignKey(record => record.PlannedProductionId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}