using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE {
	public class SubeContext : DbContext {
		public SubeContext() { }

		public SubeContext(DbContextOptions<SubeContext> options) : base(options) { }

		public DbSet<Boleto> Boletos { get; set; }
		public DbSet<Colectivo> Colectivos { get; set; }
		public DbSet<Tarjeta> Tarjetas { get; set; }
		public DbSet<TipoTarjeta> TiposTarjetas { get; set; }

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

			modelBuilder.Entity<Tarjeta>(entity => {
				entity.ToTable("Tarjeta");
				entity.HasKey(e => e.Id);

				entity.HasOne(e => e.TipoTarjeta)
					.WithMany()
					.HasForeignKey(e => e.id_beneficio);
			});

			modelBuilder.Entity<TipoTarjeta>()
				.ToTable("TipoTarjeta")
				.UseTphMappingStrategy();

			var tiposDerivados = typeof(TipoTarjeta).Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(TipoTarjeta)));
			foreach (var tipo in tiposDerivados) {
				modelBuilder.Entity(tipo);
			}

			modelBuilder.Entity<TipoTarjeta>(e => {
				e.HasKey(x => x.id);
				e.Property(x => x.nombre).IsRequired();
				e.Property(x => x.porcentaje_descuento).IsRequired();
			});
		}
	}
}
