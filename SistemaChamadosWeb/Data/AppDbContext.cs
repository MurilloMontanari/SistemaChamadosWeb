using Microsoft.EntityFrameworkCore;
using SistemaChamadosWeb.Models;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace SistemaChamadosWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Chamado> Chamados { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Chamado>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Chamados)
                .HasForeignKey(c => c.UsuarioId);
        }

    }
}
