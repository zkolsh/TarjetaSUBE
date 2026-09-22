using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests {
	[TestFixture]
	public class SubeContextMappingTests {
		private string _dbPath = null!;
		private SubeContext _context = null!;

		[SetUp]
		public void SetUp() {
			string? searchDir = TestContext.CurrentContext.TestDirectory;
			string? foundPath = null;
			while (searchDir != null) {
				string candidate = Path.Combine(searchDir, "TiendaSUBE.db");
				if (File.Exists(candidate) && new FileInfo(candidate).Length > 0) {
					foundPath = candidate;
					break;
				}
				searchDir = Path.GetDirectoryName(searchDir)!;
			}

			if (foundPath == null) {
				throw new FileNotFoundException($"Could not locate TiendaSUBE.db from {TestContext.CurrentContext.TestDirectory}");
			}

			_dbPath = foundPath;

			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseSqlite($"Data Source={_dbPath}")
				.Options;

			_context = new SubeContext(options);
			_context.Database.BeginTransaction();
		}

		[TearDown]
		public void TearDown() {
			if (_context.Database.CurrentTransaction != null) {
				_context.Database.RollbackTransaction();
			}
			_context.Dispose();
		}

		[Test]
		public void DatabaseFile_IsValid_AndContainsAllExpectedTables() {
			Assert.That(File.Exists(_dbPath), Is.True, "Database file does not exist.");

			using var connection = new SqliteConnection($"Data Source={_dbPath};Mode=ReadOnly");
			connection.Open();

			using var command = connection.CreateCommand();
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
			using var reader = command.ExecuteReader();

			var tables = new List<string>();
			while (reader.Read()) {
				tables.Add(reader.GetString(0));
			}

			Assert.That(tables, Does.Contain("Colectivo"));
			Assert.That(tables, Does.Contain("Tarjeta"));
			Assert.That(tables, Does.Contain("Boleto"));
		}

		[Test]
		public void Colectivo_ModelMapping_MatchesSchema() {
			var colectivoEntityType = _context.Model.FindEntityType(typeof(Colectivo));
			Assert.That(colectivoEntityType, Is.Not.Null);
			Assert.That(colectivoEntityType!.GetTableName(), Is.EqualTo("Colectivo"));

			var pk = colectivoEntityType.FindPrimaryKey();
			Assert.That(pk, Is.Not.Null);
			Assert.That(pk!.Properties.Select(p => p.Name), Contains.Item("NroInterno"));

			var lineaProp = colectivoEntityType.FindProperty(nameof(Colectivo.Linea));
			Assert.That(lineaProp, Is.Not.Null);
			Assert.That(lineaProp!.GetColumnName(), Is.EqualTo("Linea"));
		}

		[Test]
		public void Tarjeta_ModelMapping_MatchesSchema() {
			var tarjetaEntityType = _context.Model.FindEntityType(typeof(Tarjeta));
			Assert.That(tarjetaEntityType, Is.Not.Null);
			Assert.That(tarjetaEntityType!.GetTableName(), Is.EqualTo("Tarjeta"));

			var pk = tarjetaEntityType.FindPrimaryKey();
			Assert.That(pk, Is.Not.Null);
			Assert.That(pk!.Properties.Select(p => p.Name), Contains.Item("Id"));

			var dniProp = tarjetaEntityType.FindProperty(nameof(Tarjeta.dniUsuario));
			Assert.That(dniProp, Is.Not.Null);
			Assert.That(dniProp!.GetColumnName(), Is.EqualTo("dniUsuario"));

			var saldoProp = tarjetaEntityType.FindProperty(nameof(Tarjeta.Saldo));
			Assert.That(saldoProp, Is.Not.Null);
			Assert.That(saldoProp!.GetColumnName(), Is.EqualTo("Saldo"));
		}

		[Test]
		public void Boleto_ModelMapping_MatchesSchemaAndForeignKeys() {
			var boletoEntityType = _context.Model.FindEntityType(typeof(Boleto));
			Assert.That(boletoEntityType, Is.Not.Null);
			Assert.That(boletoEntityType!.GetTableName(), Is.EqualTo("Boleto"));

			var pk = boletoEntityType.FindPrimaryKey();
			Assert.That(pk, Is.Not.Null);
			Assert.That(pk!.Properties.Select(p => p.Name), Contains.Item("Id"));

			var fechaProp = boletoEntityType.FindProperty(nameof(Boleto.Fecha));
			Assert.That(fechaProp, Is.Not.Null);
			Assert.That(fechaProp!.GetColumnName(), Is.EqualTo("Fecha"));

			var montoProp = boletoEntityType.FindProperty(nameof(Boleto.Monto));
			Assert.That(montoProp, Is.Not.Null);
			Assert.That(montoProp!.GetColumnName(), Is.EqualTo("Monto"));

			var colectivoEntityType = _context.Model.FindEntityType(typeof(Colectivo))!;
			var tarjetaEntityType = _context.Model.FindEntityType(typeof(Tarjeta))!;

			var foreignKeys = boletoEntityType.GetForeignKeys().ToList();
			Assert.That(foreignKeys, Has.Count.EqualTo(2));

			var colectivoFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType == colectivoEntityType);
			Assert.That(colectivoFk, Is.Not.Null);
			Assert.That(colectivoFk!.Properties.Select(p => p.GetColumnName()), Contains.Item("ColectivoId"));

			var tarjetaFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType == tarjetaEntityType);
			Assert.That(tarjetaFk, Is.Not.Null);
			Assert.That(tarjetaFk!.Properties.Select(p => p.GetColumnName()), Contains.Item("TarjetaId"));
		}

		[Test]
		public void Colectivo_Table_CRUD_Operations() {
			var colectivo = new Colectivo {
				Linea = "142 Rojo"
			};

			_context.Colectivos.Add(colectivo);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(colectivo.NroInterno, Is.GreaterThan(0));
			int savedId = colectivo.NroInterno;

			var readColectivo = _context.Colectivos.Find(savedId);
			Assert.That(readColectivo, Is.Not.Null);
			Assert.That(readColectivo!.NroInterno, Is.EqualTo(savedId));
			Assert.That(readColectivo.Linea, Is.EqualTo("142 Rojo"));

			readColectivo.Linea = "142 Negra";
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var updatedColectivo = _context.Colectivos.Find(savedId);
			Assert.That(updatedColectivo, Is.Not.Null);
			Assert.That(updatedColectivo!.Linea, Is.EqualTo("142 Negra"));

			_context.Colectivos.Remove(updatedColectivo);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var deletedColectivo = _context.Colectivos.Find(savedId);
			Assert.That(deletedColectivo, Is.Null);
		}

		[Test]
		public void Tarjeta_Table_CRUD_Operations() {
			var tarjeta = new Tarjeta {
				dniUsuario = 40123456,
				Saldo = 5000
			};

			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(tarjeta.Id, Is.GreaterThan(0));
			int savedId = tarjeta.Id;

			var readTarjeta = _context.Tarjetas.Find(savedId);
			Assert.That(readTarjeta, Is.Not.Null);
			Assert.That(readTarjeta!.Id, Is.EqualTo(savedId));
			Assert.That(readTarjeta.dniUsuario, Is.EqualTo(40123456));
			Assert.That(readTarjeta.Saldo, Is.EqualTo(5000));

			readTarjeta.dniUsuario = 38999111;
			readTarjeta.Saldo = 8000;
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var updatedTarjeta = _context.Tarjetas.Find(savedId);
			Assert.That(updatedTarjeta, Is.Not.Null);
			Assert.That(updatedTarjeta!.dniUsuario, Is.EqualTo(38999111));
			Assert.That(updatedTarjeta.Saldo, Is.EqualTo(8000));

			_context.Tarjetas.Remove(updatedTarjeta);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var deletedTarjeta = _context.Tarjetas.Find(savedId);
			Assert.That(deletedTarjeta, Is.Null);
		}

		[Test]
		public void Boleto_Table_CRUD_Operations() {
			var colectivo = new Colectivo {Linea = "110"};
			var tarjeta = new Tarjeta {dniUsuario = 29999000, Saldo = 15000};

			_context.Colectivos.Add(colectivo);
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			DateTime explicitFecha = new(2026, 9, 21, 14, 0, 0);

			var boleto = new Boleto {
				Fecha = explicitFecha,
				Colectivo = colectivo,
				Monto = 1580,
				Tarjeta = tarjeta
			};

			_context.Boletos.Add(boleto);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(boleto.Id, Is.GreaterThan(0));
			int savedBoletoId = boleto.Id;

			var readBoleto = _context.Boletos.Find(savedBoletoId);
			Assert.That(readBoleto, Is.Not.Null);
			Assert.That(readBoleto!.Id, Is.EqualTo(savedBoletoId));
			Assert.That(readBoleto.Fecha, Is.EqualTo(explicitFecha));
			Assert.That(readBoleto.Monto, Is.EqualTo(1580));

			readBoleto.Monto = 2000;
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var updatedBoleto = _context.Boletos.Find(savedBoletoId);
			Assert.That(updatedBoleto, Is.Not.Null);
			Assert.That(updatedBoleto!.Monto, Is.EqualTo(2000));

			_context.Boletos.Remove(updatedBoleto);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var deletedBoleto = _context.Boletos.Find(savedBoletoId);
			Assert.That(deletedBoleto, Is.Null);
		}

		[Test]
		public void CrossTable_Relational_CRUD_Operations() {
			var colectivo = new Colectivo {Linea = "110"};
			var tarjeta = new Tarjeta {dniUsuario = 35123123, Saldo = 10000};

			_context.Colectivos.Add(colectivo);
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			DateTime explicitFecha = new(2026, 9, 20, 10, 30, 0);
			var boleto = new Boleto {
				Fecha = explicitFecha,
				Colectivo = colectivo,
				Monto = 1580,
				Tarjeta = tarjeta
			};

			_context.Boletos.Add(boleto);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(boleto.Id, Is.GreaterThan(0));
			int savedBoletoId = boleto.Id;
			int savedColectivoId = colectivo.NroInterno;
			int savedTarjetaId = tarjeta.Id;

			var readBoleto = _context.Boletos
				.Include(b => b.Colectivo)
				.Include(b => b.Tarjeta)
				.FirstOrDefault(b => b.Id == savedBoletoId);

			Assert.That(readBoleto, Is.Not.Null);
			Assert.That(readBoleto!.Id, Is.EqualTo(savedBoletoId));
			Assert.That(readBoleto.Fecha, Is.EqualTo(explicitFecha));
			Assert.That(readBoleto.Monto, Is.EqualTo(1580));
			Assert.That(readBoleto.Colectivo, Is.Not.Null);
			Assert.That(readBoleto.Colectivo.NroInterno, Is.EqualTo(savedColectivoId));
			Assert.That(readBoleto.Colectivo.Linea, Is.EqualTo("110"));
			Assert.That(readBoleto.Tarjeta, Is.Not.Null);
			Assert.That(readBoleto.Tarjeta.Id, Is.EqualTo(savedTarjetaId));
			Assert.That(readBoleto.Tarjeta.dniUsuario, Is.EqualTo(35123123));

			readBoleto.Colectivo.Linea = "110 2";
			readBoleto.Tarjeta.Saldo = 8420;
			readBoleto.Monto = 1800;
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			var reloadedBoleto = _context.Boletos
				.Include(b => b.Colectivo)
				.Include(b => b.Tarjeta)
				.FirstOrDefault(b => b.Id == savedBoletoId);

			Assert.That(reloadedBoleto, Is.Not.Null);
			Assert.That(reloadedBoleto!.Monto, Is.EqualTo(1800));
			Assert.That(reloadedBoleto.Colectivo.Linea, Is.EqualTo("110 2"));
			Assert.That(reloadedBoleto.Tarjeta.Saldo, Is.EqualTo(8420));

			_context.Boletos.Remove(reloadedBoleto);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(_context.Boletos.Find(savedBoletoId), Is.Null);

			var colectivoToDelete = _context.Colectivos.Find(savedColectivoId);
			var tarjetaToDelete = _context.Tarjetas.Find(savedTarjetaId);
			Assert.That(colectivoToDelete, Is.Not.Null);
			Assert.That(tarjetaToDelete, Is.Not.Null);

			_context.Colectivos.Remove(colectivoToDelete!);
			_context.Tarjetas.Remove(tarjetaToDelete!);
			_context.SaveChanges();
			_context.ChangeTracker.Clear();

			Assert.That(_context.Colectivos.Find(savedColectivoId), Is.Null);
			Assert.That(_context.Tarjetas.Find(savedTarjetaId), Is.Null);
		}
	}
}
