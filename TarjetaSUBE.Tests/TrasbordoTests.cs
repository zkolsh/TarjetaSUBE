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
		private TarjetaNormal _tipoNormal = null!;
		private MedioBoletoEstudiantil _medioBoleto = null!;
		private BoletoGratuitoEstudiantil _boletoGratuito = null!;

		[SetUp]
		public void Setup() {
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);

			_colectivoLinea1 = new Colectivo { NroInterno = 101, Linea = "101 Roja" };
			_colectivoLinea2 = new Colectivo { NroInterno = 102, Linea = "102 Negra" };
			_colectivoLinea3 = new Colectivo { NroInterno = 103, Linea = "103 Verde" };
			_context.Colectivos.AddRange(_colectivoLinea1, _colectivoLinea2, _colectivoLinea3);

			_tipoNormal = new TarjetaNormal {
				id = 1,
				nombre = "TarjetaNormal",
				porcentaje_descuento = 0
			};
			_medioBoleto = new MedioBoletoEstudiantil {
				id = 2,
				nombre = "Medio Boleto Estudiantil",
				porcentaje_descuento = 50
			};
			_boletoGratuito = new BoletoGratuitoEstudiantil {
				id = 3,
				nombre = "Boleto Gratuito Estudiantil",
				porcentaje_descuento = 100
			};

			_context.TiposTarjetas.AddRange(_tipoNormal, _medioBoleto, _boletoGratuito);
			_context.SaveChanges();
		}

		[TearDown]
		public void TearDown() {
			_context.Dispose();
		}

		[Test]
		public void FranjaHorariaTrasbordo_PermiteLunesASabadoEntre7Y22() {
			var franja = new FranjaHorariaTrasbordo();

			// Lunes a las 07:00 (límite inferior)
			var lunes7am = new DateTime(2026, 10, 19, 7, 0, 0);
			Assert.That(franja.Contiene(lunes7am), Is.True);

			// Miércoles a las 14:30
			var miercolesTarde = new DateTime(2026, 10, 21, 14, 30, 0);
			Assert.That(franja.Contiene(miercolesTarde), Is.True);

			// Sábado a las 22:00 (límite superior)
			var sabado22pm = new DateTime(2026, 10, 24, 22, 0, 0);
			Assert.That(franja.Contiene(sabado22pm), Is.True);

			// Domingo (no permitido)
			var domingo = new DateTime(2026, 10, 25, 10, 0, 0);
			Assert.That(franja.Contiene(domingo), Is.False);

			// Lunes antes de las 7:00 (ej. 6:59)
			var lunesTemprano = new DateTime(2026, 10, 19, 6, 59, 59);
			Assert.That(franja.Contiene(lunesTemprano), Is.False);

			// Sábado después de las 22:00 (ej. 22:01)
			var sabadoNoche = new DateTime(2026, 10, 24, 22, 1, 0);
			Assert.That(franja.Contiene(sabadoNoche), Is.False);
		}

		[Test]
		public void Trasbordo_DentroDeUnaHoraYEntreLineasDistintas_EsGratisYSeIndicaEnBoleto() {
			var tarjeta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 5000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Lunes a las 08:00
			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			// Primer viaje en Línea 101
			bool primerViaje = _colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(primerViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(2850m)); // 5000 - 2150

			var primerBoleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(primerBoleto.EsTrasbordo, Is.False);
			Assert.That(primerBoleto.Monto, Is.EqualTo(2150m));

			// 30 minutos después en Línea 102
			reloj.AdelantarMinutos(30);
			bool segundoViaje = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(2850m), "No debió cobrar saldo en trasbordo");

			var boletoTrasbordo = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boletoTrasbordo.EsTrasbordo, Is.True, "Debe indicarse en el boleto que es trasbordo");
			Assert.That(boletoTrasbordo.Monto, Is.EqualTo(0m));
			Assert.That(boletoTrasbordo.TotalAbonado, Is.EqualTo(0m));
			Assert.That(boletoTrasbordo.Linea, Is.EqualTo("102 Negra"));
		}

		[Test]
		public void Trasbordo_EnMismaLinea_NoAplicaTrasbordoYCobraTarifa() {
			var tarjeta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 10000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			// Primer viaje en Línea 101
			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));

			// 20 minutos después, misma línea 101
			reloj.AdelantarMinutos(20);
			bool segundoViaje = _colectivoLinea1.pagarCon(_context, tarjeta, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(5700m), "En la misma línea debe cobrar tarifa regular");

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.False);
			Assert.That(boleto.Monto, Is.EqualTo(2150m));
		}

		[Test]
		public void Trasbordo_PasadaLaHora_NoAplicaTrasbordoYCobraTarifa() {
			var tarjeta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 10000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			// Primer viaje en Línea 101
			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(61);
			bool segundoViaje = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);

			Assert.That(segundoViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(5700m));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.False);
			Assert.That(boleto.Monto, Is.EqualTo(2150m));
		}

		[Test]
		public void TrasbordosSinLimite_DentroDelPlazoDeUnaHoraDesdeElPrimerBoleto() {
			var tarjeta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 10000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));

			reloj.AdelantarMinutos(15);
			bool viaje2 = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);
			Assert.That(viaje2, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);

			reloj.AdelantarMinutos(20);
			bool viaje3 = _colectivoLinea3.pagarCon(_context, tarjeta, reloj);
			Assert.That(viaje3, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);

			reloj.AdelantarMinutos(20);
			bool viaje4 = _colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(viaje4, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.True);

			// A las 09:05 (ya pasó 1 hora y 5 minutos desde el primer boleto a las 08:00) - Cobra
			reloj.AdelantarMinutos(10);
			bool viaje5 = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);
			Assert.That(viaje5, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(5700m), "Debe cobrar nuevo boleto porque superó 1 hora del primer boleto");
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.False);
		}

		[Test]
		public void Trasbordo_FueraDeHorarioPermitido_NoAplicaTrasbordo() {
			var tarjeta = new Tarjeta {
				dniUsuario = 12345678,
				Saldo = 10000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Domingo a las 10:00 (Domingos no hay trasbordo)
			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 25, 10, 0, 0)
			};

			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(7850m));

			// 20 minutos después en Línea 102
			reloj.AdelantarMinutos(20);
			_colectivoLinea2.pagarCon(_context, tarjeta, reloj);

			Assert.That(tarjeta.Saldo, Is.EqualTo(5700m), "En domingo no aplica trasbordo sin costo");
			Assert.That(_context.Boletos.OrderByDescending(b => b.Id).First().EsTrasbordo, Is.False);
		}

		[Test]
		public void TodasLasTarjetas_PuedenRealizarTrasbordo_MedioBoleto() {
			var tarjeta = new Tarjeta {
				dniUsuario = 23456789,
				Saldo = 5000,
				TipoTarjeta = _medioBoleto,
				id_beneficio = _medioBoleto.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Lunes 08:00
			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			// Primer viaje con medio boleto (paga 1075)
			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(3925m)); // 5000 - 1075

			// 20 minutos después toma otra línea -> trasbordo gratis
			reloj.AdelantarMinutos(20);
			bool trasbordo = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);

			Assert.That(trasbordo, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(3925m));
			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.True);
			Assert.That(boleto.Monto, Is.EqualTo(0m));
		}

		[Test]
		public void TodasLasTarjetas_PuedenRealizarTrasbordo_BoletoGratuito() {
			var tarjeta = new Tarjeta {
				dniUsuario = 34567890,
				Saldo = 5000,
				TipoTarjeta = _boletoGratuito,
				id_beneficio = _boletoGratuito.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			_colectivoLinea1.pagarCon(_context, tarjeta, reloj);
			Assert.That(tarjeta.Saldo, Is.EqualTo(5000m));

			// 25 minutos después en línea 2
			reloj.AdelantarMinutos(25);
			bool trasbordo = _colectivoLinea2.pagarCon(_context, tarjeta, reloj);

			Assert.That(trasbordo, Is.True);
			var boleto = _context.Boletos.OrderByDescending(b => b.Id).First();
			Assert.That(boleto.EsTrasbordo, Is.True);
			Assert.That(boleto.Monto, Is.EqualTo(0m));
		}

		[Test]
		public void Colectivo_PermiteInyeccionDeValidadorPagoPersonalizado() {
			var validadorCustom = new ValidadorPago(new ServicioTrasbordo());
			var colectivo = new Colectivo(validadorCustom) {
				NroInterno = 999,
				Linea = "999 Custom"
			};
			_context.Colectivos.Add(colectivo);
			_context.SaveChanges();

			var tarjeta = new Tarjeta {
				dniUsuario = 99999999,
				Saldo = 5000,
				TipoTarjeta = _tipoNormal,
				id_beneficio = _tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			var reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

			bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);
			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(2850m));
		}
	}
}
