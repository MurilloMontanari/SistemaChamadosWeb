using Microsoft.EntityFrameworkCore;
using SistemaChamadosWeb.Models;

namespace SistemaChamadosWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opts) : base(opts) { }

        public DbSet<Usuario> Usuarios { get; set; } = null!;
        public DbSet<Chamado> Chamados { get; set; } = null!;
        public DbSet<ChamadoComentario> Comentarios { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Chamado -> Usuario (1:N)  
            modelBuilder.Entity<Chamado>()
                .HasOne(c => c.Usuario)
                .WithMany(u => u.Chamados)
                .HasForeignKey(c => c.UsuarioId);

            // Comentario -> Chamado (N:1)
            modelBuilder.Entity<ChamadoComentario>()
                .HasOne(cc => cc.Chamado)
                .WithMany()
                .HasForeignKey(cc => cc.ChamadoId);

            // Comentario -> Usuario (N:1)
            modelBuilder.Entity<ChamadoComentario>()
                .HasOne(cc => cc.Usuario)
                .WithMany()
                .HasForeignKey(cc => cc.UsuarioId);



            modelBuilder.Entity<Chamado>()
                .Property(c => c.DataAbertura)
                .HasColumnType("timestamp without time zone");

            modelBuilder.Entity<ChamadoComentario>()
                .Property(c => c.DataHora)
                .HasColumnType("timestamp without time zone");
        }

    }
}

