using APINet.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace APINet.Data
{
    [ExcludeFromCodeCoverage]
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CuentaBancaria> CuentasBancarias => Set<CuentaBancaria>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CuentaBancaria>().ToTable("CuentaBancaria");
            modelBuilder.Entity<CuentaBancaria>().HasKey(c => c.IdCuenta);
            modelBuilder.Entity<CuentaBancaria>().Property(c => c.Saldo).HasPrecision(18, 2);
        }
    }
}
