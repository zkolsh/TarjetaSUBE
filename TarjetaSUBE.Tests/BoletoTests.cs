using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests {
	[TestFixture]
	public class BoletoTests {
		private SubeContext _context = null!;
		private Colectivo _colectivo = null!;
		private ProveedorFechaSimulado _fechaSimulada = null!;

		private TipoTarjeta _tipoNormal;
		private TipoTarjeta _franquiciaCompleta;
		private TipoTarjeta _medioBoletoEstudiantil;
		private TipoTarjeta _boletoEducativo;

		private Tarjeta _tarjetaNormal;
		private Tarjeta _tarjetaFranquiciaCompleta;
		private Tarjeta _tarjetaMedioBoleto;
		private Tarjeta _tarjetaBoletoEducativo;

		private Colectivo CrearColectivo(decimal? tarifa = null) {
			return tarifa.HasValue? new Colectivo(tarifa.Value) {
					NroInterno = 101,
					Linea = "102 Negra"
				} : new Colectivo {
					NroInterno = 101,
					Linea = "102 Negra"
				};
		}

		[SetUp]
		public void Setup() {
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
			_colectivo = CrearColectivo();

			_tipoNormal = new TarjetaNormal {
				id = 1,
				nombre = "TarjetaNormal",
				porcentaje_descuento = 0
			};

			_franquiciaCompleta = new FranquiciaCompleta {
				id = 2,
				nombre = "FranquiciaCompleta",
				porcentaje_descuento = 100
			};

			_medioBoletoEstudiantil = new MedioBoletoEstudiantil {
				id = 3,
				nombre = "MedioBoletoEstudiantil",
				porcentaje_descuento = 50
			};

			_boletoEducativo = new BoletoGratuitoEstudiantil {
				id = 4,
				nombre = "BoletoGratuitoEstudiantil",
				porcentaje_descuento = 100
			};

			_tarjetaNormal = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 0,
				TipoTarjeta = _tipoNormal,
			};

			_tarjetaFranquiciaCompleta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 0,
				TipoTarjeta = _franquiciaCompleta,
			};

			_tarjetaMedioBoleto = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 0,
				TipoTarjeta = _medioBoletoEstudiantil,
			};

			_tarjetaBoletoEducativo = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 0,
				TipoTarjeta = _boletoEducativo,
			};

			_context.TiposTarjetas.Add(_tipoNormal);
			_context.TiposTarjetas.Add(_franquiciaCompleta);
			_context.TiposTarjetas.Add(_medioBoletoEstudiantil);
			_context.TiposTarjetas.Add(_boletoEducativo);
			_context.Tarjetas.Add(_tarjetaNormal);
			_context.Tarjetas.Add(_tarjetaFranquiciaCompleta);
			_context.Tarjetas.Add(_tarjetaMedioBoleto);
			_context.Tarjetas.Add(_tarjetaBoletoEducativo);

			_context.Colectivos.Add(_colectivo);
			_context.SaveChanges();

			_fechaSimulada = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};
		}

		[TearDown]
		public void TearDown() {
			_context.Dispose();
		}

		[Test]
		public void Boleto_ConTarjetaNormal_ObtieneDatosCorrectosYTarifaCompleta() {
			_tarjetaNormal.CargarTarjeta(5000, _context);
			_tarjetaNormal.AcreditarCarga();

			ProveedorFechaSimulado fecha = new();
			DateTime antesDelViaje = fecha.FechaActual;
			bool resultado = _colectivo.pagarCon(_context, _tarjetaNormal, fecha);
			fecha.AdelantarMinutos(10);
			DateTime despuesDelViaje = fecha.FechaActual;

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(2850)); //5000 - 2150

			var boleto = _context.Boletos.Include(b => b.Colectivo).Include(b => b.Tarjeta).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);

			Assert.That(boleto!.Fecha, Is.InRange(antesDelViaje, despuesDelViaje));
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
			Assert.That(boleto.Linea, Is.EqualTo("102 Negra"));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(2150));
			Assert.That(boleto.Monto, Is.EqualTo(2150));
			Assert.That(boleto.Saldo, Is.EqualTo(2850));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(2850));
			Assert.That(boleto.Tarjeta.Id, Is.EqualTo(_tarjetaNormal.Id));
			Assert.That(boleto.TarjetaId, Is.EqualTo(_tarjetaNormal.Id));
		}

		[Test]
		public void Boleto_ConMedioBoletoEstudiantil_ObtieneDescuento50Porciento() {
			Assert.That(_tarjetaMedioBoleto.CargarTarjeta(2000, _context), Is.True);
			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _fechaSimulada);

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(925)); // 2000 - 1075

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.TipoTarjeta, Is.EqualTo("MedioBoletoEstudiantil"));
			Assert.That(boleto.Monto, Is.EqualTo(1075));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(1075));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(925));
			Assert.That(boleto.Saldo, Is.EqualTo(925));
			Assert.That(boleto.Linea, Is.EqualTo("102 Negra"));
		}

		[Test]
		public void Boleto_ConBoletoGratuitoEstudiantil_ObtieneTarifaCero() {
			Assert.That(_tarjetaBoletoEducativo.CargarTarjeta(2000, _context), Is.True);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _fechaSimulada);

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(2000));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.TipoTarjeta, Is.EqualTo("BoletoGratuitoEstudiantil"));
			Assert.That(boleto.Monto, Is.EqualTo(0));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(0));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(2000));
			Assert.That(boleto.Saldo, Is.EqualTo(2000));
		}

		[Test]
		public void Boleto_ConFranquiciaCompleta_ObtieneTarifaCero() {
			bool resultado = _colectivo.pagarCon(_context, _tarjetaFranquiciaCompleta, _fechaSimulada);

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaFranquiciaCompleta.Saldo, Is.EqualTo(0));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.TipoTarjeta, Is.EqualTo("FranquiciaCompleta"));
			Assert.That(boleto.Monto, Is.EqualTo(0));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(0));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(0));
			Assert.That(boleto.Saldo, Is.EqualTo(0));
		}

		[Test]
		public void Boleto_ConMedioBoletoYSaldoNegativo_InformaTotalAbonadoSuperiorATarifaNormal() {
			Assert.That(_tarjetaMedioBoleto.CargarTarjeta(2000, _context), Is.True);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _fechaSimulada);

			Assert.That(resultado, Is.True);
			// Saldo restante: 1500 - 1075 = 425
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(925));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.Monto, Is.EqualTo(1075));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(1075));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(925));
			Assert.That(boleto.Saldo, Is.EqualTo(925));
		}

		[Test]
		public void Boleto_Propiedades_RetornanValoresCorrectos() {
			DateTime fecha = new(2026, 10, 19, 10, 0, 0);
			Assert.That(_tarjetaNormal.CargarTarjeta(3000, _context), Is.True);

			var boleto = new Boleto
			{
				Id = 42,
				Fecha = fecha,
				Colectivo = _colectivo,
				Linea = "102 Negra",
				Monto = 1580,
				TotalAbonado = 1780,
				Saldo = -1780,
				SaldoRestante = -1780,
				Tarjeta = _tarjetaNormal,
				TarjetaId = 7,
				TipoTarjeta = "TarjetaNormal"
			};

			Assert.That(boleto.Fecha, Is.EqualTo(fecha));
			Assert.That(boleto.Linea, Is.EqualTo("102 Negra"));
			Assert.That(boleto.Monto, Is.EqualTo(1580));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(1780));
			Assert.That(boleto.Saldo, Is.EqualTo(-1780));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(-1780));
			Assert.That(boleto.TarjetaId, Is.EqualTo(7));
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
			Assert.That(boleto.Colectivo, Is.EqualTo(_colectivo));
			Assert.That(boleto.Tarjeta, Is.EqualTo(_tarjetaNormal));
		}
	}
}
