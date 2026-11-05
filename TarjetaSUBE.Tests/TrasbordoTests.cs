using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests {
	[TestFixture]
	public class TrasbordoTests {
		private SubeContext _context = null!;
		private Colectivo _colectivoLinea1 = null!;
		private Colectivo _colectivoLinea2 = null!;
		private Colectivo _colectivoLinea3 = null!;
		private MedioBoletoEstudiantil _medioBoleto = null!;
		private BoletoGratuitoEstudiantil _boletoGratuito = null!;

		private IFranjaHoraria _franjaTrasbordo = new FranjaHorariaTrasbordo();

		private TipoTarjeta _tipoNormal;
		private TipoTarjeta _medioBoletoEstudiantil;
		private TipoTarjeta _boletoEducativo;

		private Tarjeta _tarjetaNormal;
		private Tarjeta _tarjetaMedioBoleto;
		private Tarjeta _tarjetaBoletoEducativo;

		private ProveedorFechaSimulado reloj;

		private Colectivo CrearColectivo(int interno, string linea) {
			return new Colectivo {
				NroInterno = interno,
				Linea = linea,
			};
		}

		[SetUp]
		public void Setup() {
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);

			_colectivoLinea1 = CrearColectivo(101, "101 Roja");
			_colectivoLinea2 = CrearColectivo(102, "102 Negra");
			_colectivoLinea3 = CrearColectivo(103, "103 Verde");
			_context.Colectivos.AddRange(_colectivoLinea1, _colectivoLinea2, _colectivoLinea3);

			_tipoNormal = new TarjetaNormal {
				id = 1,
				nombre = "TarjetaNormal",
				porcentaje_descuento = 0
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
			_context.TiposTarjetas.Add(_medioBoletoEstudiantil);
			_context.TiposTarjetas.Add(_boletoEducativo);
			_context.Tarjetas.Add(_tarjetaNormal);
			_context.Tarjetas.Add(_tarjetaMedioBoleto);
			_context.Tarjetas.Add(_tarjetaBoletoEducativo);

			_context.SaveChanges();

			reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};
		}

		[TearDown]
		public void TearDown() {
			_context.Dispose();
		}

		[Test]
		public void FranjaHorariaTrasbordo_PermiteLunesASabadoEntre7Y22() {
			// Lunes a las 07:00 (límite inferior)
			var lunes7am = new DateTime(2026, 10, 19, 7, 0, 0);
			Assert.That(_franjaTrasbordo.Contiene(lunes7am), Is.True);

			// Miércoles a las 14:30
			var miercolesTarde = new DateTime(2026, 10, 21, 14, 30, 0);
			Assert.That(_franjaTrasbordo.Contiene(miercolesTarde), Is.True);

			// Sábado a las 22:00 (límite superior)
			var sabado22pm = new DateTime(2026, 10, 24, 22, 0, 0);
			Assert.That(_franjaTrasbordo.Contiene(sabado22pm), Is.True);

			// Domingo (no permitido)
			var domingo = new DateTime(2026, 10, 25, 10, 0, 0);
			Assert.That(_franjaTrasbordo.Contiene(domingo), Is.False);

			// Lunes antes de las 7:00 (ej. 6:59)
			var lunesTemprano = new DateTime(2026, 10, 19, 6, 59, 59);
			Assert.That(_franjaTrasbordo.Contiene(lunesTemprano), Is.False);

			// Sábado después de las 22:00 (ej. 22:01)
			var sabadoNoche = new DateTime(2026, 10, 24, 22, 1, 0);
			Assert.That(_franjaTrasbordo.Contiene(sabadoNoche), Is.False);
		}

		[Test]
		public void Trasbordo_DentroDeUnaHoraYEntreLineasDistintas_EsGratisYSeIndicaEnBoleto() {
			_tarjetaNormal.CargarTarjeta(5000, _context);

			// Primer viaje en Línea 101
			bool primerViaje = _colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(primerViaje, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(2850m)); // 5000 - 2150

			var primerBoleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(primerBoleto.EsTrasbordo, Is.False);
			Assert.That(primerBoleto.Monto, Is.EqualTo(2150m));

			// 30 minutos después en Línea 102
			reloj.AdelantarMinutos(30);
			bool segundoViaje = _colectivoLinea2.pagarCon(_context, _tarjetaNormal, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(2850m), "No debió cobrar saldo en trasbordo");

			var boletoTrasbordo = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boletoTrasbordo.EsTrasbordo, Is.True, "Debe indicarse en el boleto que es trasbordo");
			Assert.That(boletoTrasbordo.Monto, Is.EqualTo(0m));
			Assert.That(boletoTrasbordo.TotalAbonado, Is.EqualTo(0m));
			Assert.That(boletoTrasbordo.Linea, Is.EqualTo("102 Negra"));
		}

		[Test]
		public void Trasbordo_EnMismaLinea_NoAplicaTrasbordoYCobraTarifa() {
			_tarjetaNormal.CargarTarjeta(10000, _context);

			// Primer viaje en Línea 101
			_colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			// 20 minutos después, misma línea 101
			reloj.AdelantarMinutos(20);
			bool segundoViaje = _colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(5700m), "En la misma línea debe cobrar tarifa regular");

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.False);
			Assert.That(boleto.Monto, Is.EqualTo(2150m));
		}

		[Test]
		public void Trasbordo_PasadaLaHora_NoAplicaTrasbordoYCobraTarifa() {
			_tarjetaNormal.CargarTarjeta(10000, _context);

			// Primer viaje en Línea 101
			_colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(61);
			bool segundoViaje = _colectivoLinea2.pagarCon(_context, _tarjetaNormal, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(5700m));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.False);
			Assert.That(boleto.Monto, Is.EqualTo(2150m));
		}

		[Test]
		public void TrasbordosSinLimite_DentroDelPlazoDeUnaHoraDesdeElPrimerBoleto() {
			_tarjetaNormal.CargarTarjeta(10000, _context);

			_colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(15);
			bool viaje2 = _colectivoLinea2.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(viaje2, Is.True);
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(20);
			bool viaje3 = _colectivoLinea3.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(viaje3, Is.True);
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(20);
			bool viaje4 = _colectivoLinea1.pagarCon(_context,_tarjetaNormal, reloj);
			Assert.That(viaje4, Is.True);
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			// A las 09:05 (ya pasó 1 hora y 5 minutos desde el primer boleto a las 08:00) - Cobra
			reloj.AdelantarMinutos(10);
			bool viaje5 = _colectivoLinea2.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(viaje5, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(5700m), "Debe cobrar nuevo boleto porque superó 1 hora del primer boleto");
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.False);
		}

		[Test]
		public void Trasbordo_FueraDeHorarioPermitido_NoAplicaTrasbordo() {
			_tarjetaNormal.CargarTarjeta(10000, _context);
			reloj.FechaActual = new DateTime(2026, 10, 25, 10, 0, 0);

			_colectivoLinea1.pagarCon(_context, _tarjetaNormal, reloj);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(7850m));

			// 20 minutos después en Línea 102
			reloj.AdelantarMinutos(20);
			_colectivoLinea2.pagarCon(_context, _tarjetaNormal, reloj);

			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(5700m), "En domingo no aplica trasbordo sin costo");
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.False);
		}

		[Test]
		public void TodasLasTarjetas_PuedenRealizarTrasbordo_MedioBoleto() {
			_tarjetaMedioBoleto.CargarTarjeta(5000, _context);

			// Primer viaje con medio boleto (paga 1075)
			_colectivoLinea1.pagarCon(_context, _tarjetaMedioBoleto, reloj);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(3925m)); // 5000 - 1075

			// 20 minutos después toma otra línea -> trasbordo gratis
			reloj.AdelantarMinutos(20);
			bool trasbordo = _colectivoLinea2.pagarCon(_context, _tarjetaMedioBoleto, reloj);

			Assert.That(trasbordo, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(3925m));
			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.True);
			Assert.That(boleto.Monto, Is.EqualTo(0m));
		}

		[Test]
		public void TodasLasTarjetas_PuedenRealizarTrasbordo_BoletoGratuito() {
			_tarjetaBoletoEducativo.CargarTarjeta(5000, _context);

			_colectivoLinea1.pagarCon(_context, _tarjetaBoletoEducativo, reloj);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000m));

			// 25 minutos después en línea 2
			reloj.AdelantarMinutos(25);
			bool trasbordo = _colectivoLinea2.pagarCon(_context, _tarjetaBoletoEducativo, reloj);

			Assert.That(trasbordo, Is.True);
			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.True);
			Assert.That(boleto.Monto, Is.EqualTo(0m));
		}
	}
}
