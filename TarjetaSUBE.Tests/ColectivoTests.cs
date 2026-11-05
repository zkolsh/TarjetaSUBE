using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests { 
	public class ColectivoTests {
		private SubeContext _context = null!;
		private Colectivo _colectivo = null!;
		private ProveedorFechaSimulado _reloj = null!;
		private decimal _tarifaNormal; 

		private TipoTarjeta _tipoNormal;
		private TipoTarjeta _franquiciaCompleta;
		private TipoTarjeta _medioBoletoEstudiantil;
		private TipoTarjeta _boletoEducativo;

		private Tarjeta _tarjetaNormal;
		private Tarjeta _tarjetaFranquiciaCompleta;
		private Tarjeta _tarjetaMedioBoleto;
		private Tarjeta _tarjetaBoletoEducativo;

		[SetUp]
		public void Setup() {
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
			_colectivo = new Colectivo { NroInterno = 101, Linea = "102 Negra" };
			_context.Colectivos.Add(_colectivo);

			_reloj = new ProveedorFechaSimulado {
				FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
			};

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

			_context.SaveChanges();

			_tarifaNormal = 2150m;
		}

		[TearDown]
		public void TearDown() {
			_context.Dispose();
		}

		[Test]
		public void PagarCon_ConSaldoSuficiente_DescuentaTarifaCreaBoletoYPersiste() {
			Assert.That(_tarjetaNormal.CargarTarjeta(5000, _context), Is.True);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaNormal, new ProveedorFechaSimulado());
			Assert.That(resultado, Is.True);
			// 5000 - 2150 = 2850
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(2850));

			var boletos = _context.Boletos.Include(b => b.Colectivo).Include(b => b.Tarjeta).ToList();
			Assert.That(boletos.Count, Is.EqualTo(1));

			var boleto = boletos.First();
			Assert.That(boleto.Monto, Is.EqualTo(_tarifaNormal));
			Assert.That(boleto.Colectivo.NroInterno, Is.EqualTo(101));
			Assert.That(boleto.Tarjeta.Id, Is.EqualTo(_tarjetaNormal.Id));
		}

		[Test]
		public void PagarCon_ConSaldoInsuficiente_RetornaFalseYNoCreaBoleto() {
			bool resultado = _colectivo.pagarCon(_context, _tarjetaNormal, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.False);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(0));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(0));
		}

		[Test]
		public void PagarCon_ConSaldoInsuficiente_PermiteViajePlusCreaBoletoYQuedaEnSaldoNegativo() {
			Assert.That(_tarjetaNormal.CargarTarjeta(2000, _context), Is.True);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaNormal, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(-150));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(1));
		}

		[Test]
		public void MedioBoleto_SoloPermiteDosViajesPorDiaConDescuento_ElTerceroPagaNormal() {
			Assert.That(_tarjetaMedioBoleto.CargarTarjeta(10000, _context), Is.True);

			// 1er viaje: 08:00 -> paga $1075
			bool viaje1 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje1, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(10000 - 1075)); // 8925

			// 2do viaje: 09:00 (mismo día) -> paga $1075
			_reloj.AdelantarMinutos(60);
			bool viaje2 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje2, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(8925 - 1075)); //7850

			// 3er viaje: 12:00 (mismo día) -> paga tarifa normal $2150
			_reloj.AdelantarMinutos(180);
			bool viaje3 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje3, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(7850 - 2150)); //5700

			// 4to viaje: al día siguiente (08:00) -> se resetea y vuelve a pagar $1075
			_reloj.AdelantarDias(1);
			bool viaje4 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje4, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5700 - 1075)); //4625
		}

		[Test]
		public void MedioBoleto_NoPermiteViajarNuevamenteEnMenosDeCincoMinutos() {
			Assert.That(_tarjetaMedioBoleto.CargarTarjeta(5000, _context), Is.True);

			// 1er viaje: 08:00 -> Pasa
			bool viaje1 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje1, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000 - 1075));

			// 2do viaje: 08:03 (pasaron solo 3 min) -> Degenera a TarjetaNormal
			_reloj.AdelantarMinutos(3);
			bool viaje2 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id), Is.EqualTo(2), "Debe haberse emitido un sólo boleto");
			// Verificar que hay un boleto con TarjetaNormal
			// .Single() lanza excepcion si no encuentra nada -> No la atrapamos, entonces NUnit marca el test como fallido.
			_context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id && b.TipoTarjeta == "MedioBoletoEstudiantil").Single();
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000 - 1075 - 2150)); // El saldo SI se descuenta, pero boleto normal

			// 3er intento: 08:06 (pasaron 6 min desde el 1ero) -> Pasa
			_reloj.AdelantarMinutos(3);
			bool viaje3 = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(viaje3, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000 - 1075 - 2150 - 1075));
		}

		[Test]
		public void BoletoGratuito_NoPermiteRealizarMasDeDosViajesGratuitosPorDia() {
			Assert.That(_tarjetaBoletoEducativo.CargarTarjeta(10000, _context), Is.True);

			// 1er viaje: 08:00 -> Gratis ($0)
			_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj);

			// 2do viaje: 09:00 -> Gratis ($0)
			_reloj.AdelantarMinutos(60);
			_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj);

			// 3er viaje: 12:00 -> Ya no es gratis
			_reloj.AdelantarMinutos(180);
			_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj);

			// 4to viaje: 13:00 -> Ya no es gratis
			_reloj.AdelantarMinutos(60);
			_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj);

			// Verificamos en la base de datos que la cantidad de boletos con Monto == 0 en el día es exactamente 2
			int boletosGratisDelDia = _context.Boletos
				.Count(b => b.Tarjeta.Id == _tarjetaBoletoEducativo.Id && b.Fecha.Date == _reloj.Ahora.Date && b.Monto == 0);
			Assert.That(boletosGratisDelDia, Is.EqualTo(2), "No debe haber más de dos viajes gratuitos en el mismo día.");
		}

		[Test]
		public void BoletoGratuito_ViajesPosterioresAlSegundoSeCobranConPrecioCompleto() {
			Assert.That(_tarjetaBoletoEducativo.CargarTarjeta(5000, _context), Is.True);

			// 1er viaje (08:00): gratis ($0) -> Saldo sigue en 5000
			Assert.That(_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000));

			// 2do viaje (09:00): gratis ($0) -> Saldo sigue en 5000
			_reloj.AdelantarMinutos(60);
			Assert.That(_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000));

			// 3er viaje (10:00, posterior al 2do): cobra precio completo ($2150)
			_reloj.AdelantarMinutos(60);
			Assert.That(_colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000 - 2150)); //2850

			// Verificamos que el boleto emitido para este 3er viaje registró el monto completo ($2150)
			var boletoTercerViaje = _context.Boletos.OrderByDescending(b => b.Fecha).First();
			Assert.That(boletoTercerViaje.Monto, Is.EqualTo(2150));
		}

		[Test]
		public void FranquiciaCompleta_SiemprePuedePagarBoletoEnColectivo_InclusoSinSaldo() {
			bool resultado = _colectivo.pagarCon(_context, _tarjetaFranquiciaCompleta, _reloj);
			Assert.That(resultado, Is.True, "Franquicia completa siempre debe poder pagar en el colectivo.");
			Assert.That(_tarjetaFranquiciaCompleta.Saldo, Is.EqualTo(0), "No debe descontar saldo.");
			var boletoEmitido = _context.Boletos.First(b => b.Tarjeta.Id == _tarjetaFranquiciaCompleta.Id);
			Assert.That(boletoEmitido.Monto, Is.EqualTo(0));
		}

		[Test]
		public void MedioBoleto_PrimerViaje_CobraLaMitadDeLaTarifaNormal() {
			_tarjetaMedioBoleto.CargarTarjeta(5000, _context);

			// Paga el primer viaje
			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			// 1. Debe permitir el viaje
			Assert.That(resultado, Is.True);

			// 2. Descuenta exactamente la mitad (2150 / 2 = 1075) -> 5000 - 1075 = 3925
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(3925));
			// 3. El boleto emitido tiene registrado el monto de 1075
			var boletoEmitido = _context.Boletos.First(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id);
			Assert.That(boletoEmitido.Monto, Is.EqualTo(1075));
		}
		[Test]
		public void TarjetaNormal_EnColectivo_AplicaUsoFrecuenteYSeReseteaAlCambiarDeMes() {
			_tarjetaNormal.CargarTarjeta(25000, _context);
			_tarjetaNormal.CargarTarjeta(25000, _context);

			// Simulamos 29 viajes previos en el mes actual agregando boletos
			for (int i = 0; i < 29; i++) {
				_context.Boletos.Add(new Boleto {
					Fecha = _reloj.Ahora,
					Linea = "102 Negra",
					Monto = 2150,
					TotalAbonado = 2150,
					Saldo = 50000,
					SaldoRestante = 50000,
					Tarjeta = _tarjetaNormal,
					TarjetaId = _tarjetaNormal.Id,
					TipoTarjeta = "TarjetaNormal",
					Colectivo = _colectivo
				});
			}

			_context.SaveChanges();
			// Viaje numero 30 en el mismo mes: debe cobrar con 20% de descuento ($1720)
			decimal saldoAntesViaje30 = _tarjetaNormal.Saldo;
			bool viaje30 = _colectivo.pagarCon(_context, _tarjetaNormal, _reloj);
			Assert.That(viaje30, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(saldoAntesViaje30 - 1720m));

			for (int i = 0; i < 49; i++) {
				_context.Boletos.Add(new Boleto {
					Fecha = _reloj.Ahora,
					Linea = "102 Negra",
					Monto = 1720,
					TotalAbonado = 1720,
					Saldo = 50000,
					SaldoRestante = 50000,
					Tarjeta = _tarjetaNormal,
					TarjetaId = _tarjetaNormal.Id,
					TipoTarjeta = "TarjetaNormal",
					Colectivo = _colectivo
				});
			}
			_context.SaveChanges();
			// Viaje numero 80: debe cobrar con 25% de descuento ($1612.50)
			decimal saldoAntesViaje80 = _tarjetaNormal.Saldo;
			bool viaje80 = _colectivo.pagarCon(_context, _tarjetaNormal, _reloj);
			Assert.That(viaje80, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(saldoAntesViaje80 - 1612.5m)); // 2150 * 0.75 = 1612.5

			// Adelantamos 35 dias (pasamos al mes siguiente): debe volver a cobrar tarifa completa ($2150)
			_reloj.AdelantarDias(35);
			decimal saldoAntesNuevoMes = _tarjetaNormal.Saldo;
			bool viajeNuevoMes = _colectivo.pagarCon(_context, _tarjetaNormal, _reloj);
			Assert.That(viajeNuevoMes, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(saldoAntesNuevoMes - 2150m));
		}

		[Test]
		public void Colectivo_ConTarifaBasePersonalizada_CobraMontoCorrespondiente() {
			var colectivoInterurbano = new Colectivo(3000m) {NroInterno = 202, Linea = "35/9"};
			_context.Colectivos.Add(colectivoInterurbano);

			var tarjeta = new Tarjeta { dniUsuario = 99887766, Saldo = 10000 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool viaje = colectivoInterurbano.pagarCon(_context, tarjeta, _reloj);

			Assert.That(viaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(10000 - 3000));
			var boleto = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
			Assert.That(boleto.Monto, Is.EqualTo(3000));
		}

		[Test]
		public void Colectivo_ConTarifaBasePersonalizadaYMedioBoleto_AplicaDescuentoSobreTarifaEspecifica() {
			var colectivoInterurbano = new Colectivo(3000m) { NroInterno = 203, Linea = "Expreso" };
			_context.Colectivos.Add(colectivoInterurbano);

			_tarjetaMedioBoleto.CargarTarjeta(10000, _context);

			bool viaje = colectivoInterurbano.pagarCon(_context, _tarjetaMedioBoleto, _reloj);

			Assert.That(viaje, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(10000 - 1500));
			var boleto = _context.Boletos.First(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id);
			Assert.That(boleto.Monto, Is.EqualTo(1500));
		}

		[Test]
		public void Franquicias_FueraDeFranjaHoraria_NoAplicaBeneficio() {
			_tarjetaFranquiciaCompleta.CargarTarjeta(5000, _context);

			// Sábado a las 10:00 (fuera de la franja)
			_reloj.FechaActual = new DateTime(2026, 10, 24, 10, 0, 0);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaFranquiciaCompleta, _reloj);

			Assert.That(resultado, Is.True); // Con tarjeta normal, se debería poder pagar
			Assert.That(_tarjetaFranquiciaCompleta.Saldo, Is.EqualTo(5000 - 2150), "El saldo no debe verse afectado.");
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaFranquiciaCompleta.Id), Is.EqualTo(1), "Debe haberse emitido un sólo boleto");
			var boleto = _context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaFranquiciaCompleta.Id).Single();
			Assert.That(boleto.TipoTarjeta, Is.Not.Null);
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
		}

		[Test]
		public void MedioBoleto_FueraDeFranjaHoraria_NoAplicaEnSabado() {
			_tarjetaMedioBoleto.CargarTarjeta(5000, _context);

			// Sábado 10:00
			_reloj.FechaActual = new DateTime(2026, 10, 24, 10, 0, 0);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000m - 2150m));

			var boleto = _context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id).Single();
			Assert.That(boleto.TipoTarjeta, Is.Not.Null);
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
		}

		[Test]
		public void MedioBoleto_AntesDeLasSeis_NoAplicaBeneficio() {
			_tarjetaMedioBoleto.CargarTarjeta(5000, _context);

			// Lunes 05:59:59
			_reloj.FechaActual = new DateTime(2026, 10, 19, 5, 59, 59);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000 - 2150));
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id), Is.EqualTo(1), "Debe haberse emitido un sólo boleto");
			var boleto = _context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id).Single();
			Assert.That(boleto.TipoTarjeta, Is.Not.Null);
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
		}

		[Test]
		public void ColectivoInterurbano_TieneTarifaDe3000PorDefecto()
		{
			var interurbano = new ColectivoInterurbano { NroInterno = 301, Linea = "Gálvez" };
			Assert.That(interurbano.ObtenerTarifaBase(), Is.EqualTo(3000m));

			_context.Colectivos.Add(interurbano);
			_tarjetaNormal.CargarTarjeta(5000, _context);

			bool pago = interurbano.pagarCon(_context, _tarjetaNormal, _reloj);
			Assert.That(pago, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(2000m)); // 5000 - 3000
			var boleto = _context.Boletos.First(b => b.Tarjeta.Id == _tarjetaNormal.Id);
			Assert.That(boleto.Monto, Is.EqualTo(3000m));
		}

		[Test]
		public void ColectivoInterurbano_ConFranquiciaCompleta_CobraCero() {
			var interurbano = new ColectivoInterurbano { NroInterno = 302, Linea = "Baigorria" };
			_context.Colectivos.Add(interurbano);

			var franquicia = new FranquiciaCompleta { id = 5, nombre = "Franquicia Completa", porcentaje_descuento = 100 };
			_context.TiposTarjetas.Add(franquicia);

			_tarjetaFranquiciaCompleta.CargarTarjeta(2000, _context);

			bool pago = interurbano.pagarCon(_context, _tarjetaFranquiciaCompleta, _reloj);
			Assert.That(pago, Is.True);
			Assert.That(_tarjetaFranquiciaCompleta.Saldo, Is.EqualTo(2000m));
			var boleto = _context.Boletos.First(b => b.Tarjeta.Id == _tarjetaFranquiciaCompleta.Id);
			Assert.That(boleto.Monto, Is.EqualTo(0m));
		}

		[Test]
		public void ColectivoInterurbano_ConBoletoGratuitoEstudiantil_PrimerosDosViajesGratisYTerceroPagaCompleto() {
			var interurbano = new ColectivoInterurbano { NroInterno = 303, Linea = "Gálvez" };
			_tarjetaBoletoEducativo.CargarTarjeta(5000,  _context);

			// 1er viaje: gratis
			Assert.That(interurbano.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000));

			// 2do viaje: gratis
			_reloj.AdelantarMinutos(60);
			Assert.That(interurbano.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000));

			// 3er viaje: paga tarifa interurbana completa ($3000)
			_reloj.AdelantarMinutos(60);
			Assert.That(interurbano.pagarCon(_context, _tarjetaBoletoEducativo, _reloj), Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(2000)); // 5000 - 3000
		}

		[Test]
		public void MedioBoleto_DespuesDeLasVeintidos_NoAplicaBeneficio() {
			_tarjetaMedioBoleto.CargarTarjeta(5000, _context);

			// Martes 22:00:01
			_reloj.FechaActual = new DateTime(2026, 10, 20, 22, 0, 1);

			Assert.That(_tarjetaMedioBoleto.TipoTarjeta.nombre, Is.EqualTo("MedioBoletoEstudiantil"));

			bool resultado = _colectivo.pagarCon(_context, _tarjetaMedioBoleto, _reloj);
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(5000 - 2150));
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id), Is.EqualTo(1), "Debe haberse emitido un sólo boleto");
			var boleto = _context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaMedioBoleto.Id).Single();
		}

		[Test]
		public void BoletoGratuito_FueraDeFranjaHoraria_NoAplicaBeneficio() {
			_tarjetaBoletoEducativo.CargarTarjeta(5000, _context);

			// Domingo 14:00
			_reloj.FechaActual = new DateTime(2026, 10, 25, 14, 0, 0);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaBoletoEducativo, _reloj);
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaBoletoEducativo.Saldo, Is.EqualTo(5000 - 2150));
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaBoletoEducativo.Id), Is.EqualTo(1), "Debe haberse emitido un sólo boleto");
			var boleto = _context.Boletos.Where(b => b.Tarjeta.Id == _tarjetaBoletoEducativo.Id).Single();
			Assert.That(boleto.TipoTarjeta, Is.Not.Null);
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
		}

		[Test]
		public void TarjetaNormal_FueraDeFranjaHorariaDeFranquicias_PuedePagarSiempre() {
			_tarjetaNormal.CargarTarjeta(5000, _context);

			// Domingo a las 23:00 (fuera de la franja de franquicias)
			_reloj.FechaActual = new DateTime(2026, 10, 25, 23, 0, 0);

			bool resultado = _colectivo.pagarCon(_context, _tarjetaNormal, _reloj);

			Assert.That(resultado, Is.True, "TarjetaNormal funciona 24/7.");
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(5000 - 2150));
			Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == _tarjetaNormal.Id), Is.EqualTo(1));
		}
	}
}
