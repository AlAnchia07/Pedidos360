using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pedidos360.Models;

namespace Pedidos360.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Producto> Productos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<PedidoDetalle> PedidoDetalles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Producto>()
                .Property(p => p.Stock)
                .IsConcurrencyToken();

            modelBuilder.Entity<Producto>()
                .Property(p => p.ImpuestoPorc)
                .HasPrecision(5, 2);

            modelBuilder.Entity<Producto>()
                .Property(p => p.StockMinimo)
                .HasDefaultValue(0);

            modelBuilder.Entity<Producto>()
                .Property(p => p.ImagenDatos)
                .HasColumnType("varbinary(max)");

            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Subtotal).HasPrecision(18, 2);

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Impuestos).HasPrecision(18, 2);

            modelBuilder.Entity<Pedido>()
                .Property(p => p.Total).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(d => d.PrecioUnit).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(d => d.Descuento).HasPrecision(18, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(d => d.ImpuestoPorc).HasPrecision(5, 2);

            modelBuilder.Entity<PedidoDetalle>()
                .Property(d => d.TotalLinea).HasPrecision(18, 2);
        }
    }
}
