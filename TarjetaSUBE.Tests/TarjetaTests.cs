using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests {
	public class TarjetaTests {
		private SubeContext _context = null!;
		private decimal tarifaNormal;

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
			tarifaNormal = 2150;

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
		}

		[TearDown]
		public void TearDown() {
			_context.Dispose();
		}

		[TestCase(2000)]
		[TestCase(3000)]
		[TestCase(4000)]
		[TestCase(5000)]
		[TestCase(8000)]
		[TestCase(10000)]
		[TestCase(15000)]
		[TestCase(20000)]
		[TestCase(25000)]
		[TestCase(30000)]
		public void ValidarCarga_ConMontosPermitidos_RetornaTrue(decimal monto) {
			bool esValido = _tarjetaNormal.ValidarCarga(monto);

			Assert.That(esValido, Is.True);
		}

		[TestCase(500)]
		[TestCase(1000)]
		[TestCase(1580)]
		[TestCase(6000)]
		[TestCase(50000)]
		public void ValidarCarga_ConMontosNoPermitidos_RetornaFalse(decimal monto) {
			bool esValido = _tarjetaNormal.ValidarCarga(monto);

			Assert.That(esValido, Is.False);
		}


		[TestCase(2000)]
		[TestCase(3000)]
		[TestCase(4000)]
		[TestCase(5000)]
		[TestCase(8000)]
		[TestCase(10000)]
		[TestCase(15000)]
		[TestCase(20000)]
		[TestCase(25000)]
		[TestCase(30000)]
		public void CargarTarjeta_ConMontosPermitidos_AumentaSaldoYPersisteEnDb(decimal monto) {
			bool resultado = _tarjetaNormal.CargarTarjeta(monto, _context);

			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(monto));

			var tarjetaEnDb = _context.Tarjetas.Find(_tarjetaNormal.Id);
			Assert.That(tarjetaEnDb, Is.Not.Null);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(monto));
		}

		[Test]
		public void CargarTarjeta_ConMontoInvalido_RetornaFalseYNoModificaSaldo() {
			bool resultado = _tarjetaNormal.CargarTarjeta(500, _context);

			Assert.That(resultado, Is.False);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(0));
		}

		[TestCase(1000)]
		[TestCase(2000)]
		public void Pagar_ConSaldoSuficiente_DescuentaSaldoYRetornaTrue(decimal monto) {
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = monto };
			bool resultado = tarjeta.Pagar(tarifaNormal);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo((monto-tarifaNormal)));
		}

		[Test]
		public void Pagar_ConSaldoInsuficiente_RetornaFalseYNoModificaSaldo() {
			bool resultado = _tarjetaNormal.Pagar(tarifaNormal);

			Assert.That(resultado, Is.False);
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(0));
		}

		[Test]
		public void Pagar_ConSaldoExacto_DejaSaldoEnLimiteYRetornaTrue() {
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 150 };
			bool resultado = tarjeta.Pagar(tarifaNormal);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-2000));
		}

		[Test]
		public void Pagar_NoPuedeQuedarConMenosSaldoQueElPermitido() {
			// Tarjeta con saldo 0: realiza un primer viaje plus (-1950, permitido ya que no supera -2000)
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 200 };
			bool primerViaje = tarjeta.Pagar(tarifaNormal);

			Assert.That(primerViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1950));

			// Segundo viaje: -1950 - 2150 = -4100 (menor a -2000, no permitido)
			bool segundoViaje = tarjeta.Pagar(tarifaNormal);

			Assert.That(segundoViaje, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1950));
		}

		[Test]
		public void CargarTarjeta_ConSaldoNegativoPorViajePlus_DescuentaDeudaYActualizaSaldoCorrectamente() {
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 200 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Realiza un viaje con saldo cero, quedando en saldo negativo (-1950)
			bool viajeExitoso = tarjeta.Pagar(tarifaNormal);
			Assert.That(viajeExitoso, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1950));

			// Se realiza una carga permitida de 2000 pesos
			bool resultadoCarga = tarjeta.CargarTarjeta(2000, _context);

			Assert.That(resultadoCarga, Is.True);
			// El saldo resultante debe descontar la deuda: 2000 - 1950 = 50
			Assert.That(tarjeta.Saldo, Is.EqualTo(50));

			var tarjetaEnDb = _context.Tarjetas.Find(tarjeta.Id);
			Assert.That(tarjetaEnDb, Is.Not.Null);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(50));
		}

		[Test]
		public void FranquiciaCompleta_SiemprePuedePagarBoleto() {
			decimal monto = _tarjetaFranquiciaCompleta!.TipoTarjeta!.CalcularMonto(tarifaNormal);

			bool resultado = _tarjetaFranquiciaCompleta.Pagar(monto);

			Assert.That(monto, Is.EqualTo(0));
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaFranquiciaCompleta.Saldo, Is.EqualTo(0));
		}

		[Test]
		public void MedioBoleto_MontoDelBoletoEsSiempreLaMitadDelNormal() {
			decimal monto = _tarjetaMedioBoleto!.TipoTarjeta!.CalcularMonto(tarifaNormal);
			bool resultado = _tarjetaMedioBoleto.Pagar(monto);

			Assert.That(monto, Is.EqualTo(tarifaNormal / 2));
			Assert.That(resultado, Is.True);
			Assert.That(_tarjetaMedioBoleto.Saldo, Is.EqualTo(0 - monto));
		}

		[Test]
		public void CargarTarjeta_SuperandoMaximoPermitido_AcreditaHasta56000YExcedenteQuedaPendiente() {
			_tarjetaNormal.CargarTarjeta(30000, _context);
			_tarjetaNormal.CargarTarjeta(20000, _context);

			// Se carga un monto permitido de 10.000 (50.000 + 10.000 = 60.000 > 56.000)
			bool resultado = _tarjetaNormal.CargarTarjeta(10000, _context);

			Assert.That(resultado, Is.True);

			// Debe acreditarse hasta el tope máximo permitido (56000)
			Assert.That(_tarjetaNormal.Saldo, Is.EqualTo(56000), "El saldo debe alcanzar el tope máximo de 56000.");

			// El excedente restante (4000) debe quedar almacenado y pendiente de acreditación
			Assert.That(_tarjetaNormal.SaldoPendiente, Is.EqualTo(4000), "El excedente debe quedar almacenado en SaldoPendiente.");

			// Verificamos que también persista en la base de datos
			var tarjetaEnDb = _context.Tarjetas.Find(_tarjetaNormal.Id);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(56000));
			Assert.That(tarjetaEnDb.SaldoPendiente, Is.EqualTo(4000));
		}

		[Test]
		public void Pagar_ConSaldoPendiente_AcreditaAutomaticamenteHastaElMaximoLuegoDelViaje() {
			// Tarjeta al tope máximo (56.000) y con 4.000 pendientes
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 56000, SaldoPendiente = 4000 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Realiza un viaje con tarifa normal de 2150
			bool viajeExitoso = tarjeta.Pagar(tarifaNormal);

			Assert.That(viajeExitoso, Is.True);

			// Gastó 2150, pero acreditó 2150 de lo pendiente: el saldo vuelve a llenarse hasta 56.000
			Assert.That(tarjeta.Saldo, Is.EqualTo(56000), "Luego del viaje debe recargar hasta el máximo nuevamente.");

			// El saldo pendiente bajó de 4000 a 1850 (4000 - 2150)
			Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(1850), "El saldo pendiente debe descontar lo que se acreditó.");
		}

		[Test]
		public void AcreditarCarga_ConSaldoNegativoQuePasaAPositivo_RetornaMontoRecuperadoHastaCero() {
			// Tarjeta con saldo negativo de -500 y carga pendiente de 2000
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -500, SaldoPendiente = 2000 };
			decimal recuperadoNegativo = tarjeta.AcreditarCarga();

			// Debe recuperar los 500 negativos para llegar a 0
			Assert.That(recuperadoNegativo, Is.EqualTo(500));
			Assert.That(tarjeta.Saldo, Is.EqualTo(1500));
			Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0));
		}

		[Test]
		public void AcreditarCarga_ConSaldoNegativoQueSigueEnNegativo_RetornaTotalAcreditado() {
			// Tarjeta con saldo negativo de -1500 y carga pendiente de 500
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -1500, SaldoPendiente = 500 };
			decimal recuperadoNegativo = tarjeta.AcreditarCarga();

			// Sube de -1500 a -1000, cubrió 500 de negativo
			Assert.That(recuperadoNegativo, Is.EqualTo(500));
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1000));
			Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0));
		}

		[Test]
		public void AcreditarCarga_ConSaldoPositivoPrevio_RetornaCero() {
			// Tarjeta con saldo positivo previo
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 1000, SaldoPendiente = 2000 };
			decimal recuperadoNegativo = tarjeta.AcreditarCarga();

			// No tenía saldo negativo por cubrir
			Assert.That(recuperadoNegativo, Is.EqualTo(0));
			Assert.That(tarjeta.Saldo, Is.EqualTo(3000));
			Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(0));
		}
	}
}
