using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE {
	public class SubeContext : DbContext {
		public SubeContext() { }

		public SubeContext(DbContextOptions<SubeContext> options) : base(options) { }

		public DbSet<Boleto> Boletos { get; set; }
		public DbSet<Colectivo> Colectivos { get; set; }
		public DbSet<Tarjeta> Tarjetas { get; set; }

		protected override void OnConfiguring(DbContextOptionsBuilder options) {
			if (!options.IsConfigured) {
				options.UseSqlite(@"Data Source=./TiendaSUBE.db");
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder) {
			modelBuilder.Entity<Boleto>(entity => {
				entity.ToTable("Boleto");
				entity.HasKey(e => e.Id);

				entity.HasOne(e => e.Colectivo)
					.WithMany()
					.HasForeignKey("ColectivoId");

				entity.HasOne(e => e.Tarjeta)
					.WithMany()
					.HasForeignKey("TarjetaId");
			});

			modelBuilder.Entity<Colectivo>()
				.ToTable("Colectivo")
				.HasKey(e => e.NroInterno);

			modelBuilder.Entity<Tarjeta>()
				.ToTable("Tarjeta")
				.HasKey(e => e.Id);
		}
	}
}
