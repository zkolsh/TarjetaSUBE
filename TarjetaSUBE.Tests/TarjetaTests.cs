using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests
{
	public class TarjetaTests
	{
        private SubeContext _context = null!;
        
		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
		}

		[TearDown]
		public void TearDown()
		{
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
		public void ValidarCarga_ConMontosPermitidos_RetornaTrue(decimal monto)
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };

			bool esValido = tarjeta.ValidarCarga(monto);

			Assert.That(esValido, Is.True);
		}

		[TestCase(500)]
		[TestCase(1000)]
		[TestCase(1580)]
		[TestCase(6000)]
		[TestCase(50000)]
		public void ValidarCarga_ConMontosNoPermitidos_RetornaFalse(decimal monto)
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };

			bool esValido = tarjeta.ValidarCarga(monto);

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
		public void CargarTarjeta_ConMontosPermitidos_AumentaSaldoYPersisteEnDb(decimal monto)
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = tarjeta.CargarTarjeta(monto, _context);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(monto));

			var tarjetaEnDb = _context.Tarjetas.Find(tarjeta.Id);
			Assert.That(tarjetaEnDb, Is.Not.Null);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(monto));
		}

		[Test]
		public void CargarTarjeta_ConMontoInvalido_RetornaFalseYNoModificaSaldo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 1000 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = tarjeta.CargarTarjeta(500, _context);

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(1000));
		}

		[TestCase(1000)]
		[TestCase(2000)]
		public void Pagar_ConSaldoSuficiente_DescuentaSaldoYRetornaTrue(decimal monto)
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = monto };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo((monto-1580)));
		}

		[Test]
		public void Pagar_ConSaldoInsuficiente_RetornaFalseYNoModificaSaldo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -1000 };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1000));
		}

		[Test]
		public void Pagar_ConSaldoExacto_DejaSaldoEnLimiteYRetornaTrue()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -420 };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-2000));
		}

		[Test]
		public void Pagar_NoPuedeQuedarConMenosSaldoQueElPermitido()
		{
			// Tarjeta con saldo 0: realiza un primer viaje plus (-1580, permitido ya que no supera -2000)
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };
			bool primerViaje = tarjeta.Pagar(1580);

			Assert.That(primerViaje, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1580));

			// Segundo viaje: -1580 - 1580 = -3160 (menor a -2000, no permitido)
			bool segundoViaje = tarjeta.Pagar(1580);

			Assert.That(segundoViaje, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1580));
		}

		[Test]
		public void CargarTarjeta_ConSaldoNegativoPorViajePlus_DescuentaDeudaYActualizaSaldoCorrectamente()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Realiza un viaje con saldo cero, quedando en saldo negativo (-1580)
			bool viajeExitoso = tarjeta.Pagar(1580);
			Assert.That(viajeExitoso, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1580));

			// Se realiza una carga permitida de 2000 pesos
			bool resultadoCarga = tarjeta.CargarTarjeta(2000, _context);

			Assert.That(resultadoCarga, Is.True);
			// El saldo resultante debe descontar la deuda: 2000 - 1580 = 420
			Assert.That(tarjeta.Saldo, Is.EqualTo(420));

			var tarjetaEnDb = _context.Tarjetas.Find(tarjeta.Id);
			Assert.That(tarjetaEnDb, Is.Not.Null);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(420));
		}

		[Test]
		public void FranquiciaCompleta_SiemprePuedePagarBoleto()
		{
			var franquiciaCompleta = new FranquiciaCompleta
			{
				id = 1,
				nombre = "Franquicia Completa",
				porcentaje_descuento = 100
			};
			_context.TiposTarjetas.Add(franquiciaCompleta);
			_context.SaveChanges();

			decimal tarifaNormal = 1580;
			decimal montoAPagar = tarifaNormal * (1 - ((decimal)franquiciaCompleta.porcentaje_descuento / 100m));

			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -2000 };

			bool resultado = tarjeta.Pagar(montoAPagar);

			Assert.That(montoAPagar, Is.EqualTo(0));
			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-2000));
		}

		[Test]
		public void MedioBoleto_MontoDelBoletoEsSiempreLaMitadDelNormal()
		{
			var medioBoleto = new MedioBoletoEstudiantil
			{
				id = 2,
				nombre = "Medio Boleto Estudiantil",
				porcentaje_descuento = 50
			};
			_context.TiposTarjetas.Add(medioBoleto);
			_context.SaveChanges();

			decimal tarifaNormal = 1580;
			decimal montoAPagar = tarifaNormal * (1 - ((decimal)medioBoleto.porcentaje_descuento / 100m));

			Assert.That(montoAPagar, Is.EqualTo(tarifaNormal / 2));
		}

		[Test]
		public void CargarTarjeta_SuperandoMaximoPermitido_AcreditaHasta56000YExcedenteQuedaPendiente()
		{
			// Tarjeta con 50.000 de saldo inicial
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 50000 };
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			// Se carga un monto permitido de 10.000 (50.000 + 10.000 = 60.000 > 56.000)
			bool resultado = tarjeta.CargarTarjeta(10000, _context);

			Assert.That(resultado, Is.True);

			// Debe acreditarse hasta el tope máximo permitido (56000)
			Assert.That(tarjeta.Saldo, Is.EqualTo(56000), "El saldo debe alcanzar el tope máximo de 56000.");
			
			// El excedente restante (4000) debe quedar almacenado y pendiente de acreditación
			Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(4000), "El excedente debe quedar almacenado en SaldoPendiente.");
			
			// Verificamos que también persista en la base de datos
			var tarjetaEnDb = _context.Tarjetas.Find(tarjeta.Id);
			Assert.That(tarjetaEnDb!.Saldo, Is.EqualTo(56000));
			Assert.That(tarjetaEnDb.SaldoPendiente, Is.EqualTo(4000));
		}
		
		[Test]
		public void Pagar_ConSaldoPendiente_AcreditaAutomaticamenteHastaElMaximoLuegoDelViaje()
		{
    		// Tarjeta al tope máximo (56.000) y con 4.000 pendientes
    		var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 56000, SaldoPendiente = 4000 };
    		_context.Tarjetas.Add(tarjeta);
    		_context.SaveChanges();

    		// Realiza un viaje con tarifa normal de 1580
    		bool viajeExitoso = tarjeta.Pagar(1580);
			
    		Assert.That(viajeExitoso, Is.True);

    		// Gastó 1580, pero acreditó 1580 de lo pendiente: el saldo vuelve a llenarse hasta 56.000
    		Assert.That(tarjeta.Saldo, Is.EqualTo(56000), "Luego del viaje debe recargar hasta el máximo nuevamente.");
    		
			// El saldo pendiente bajó de 4000 a 2420 (4000 - 1580)
    		Assert.That(tarjeta.SaldoPendiente, Is.EqualTo(2420), "El saldo pendiente debe descontar lo que se acreditó.");
		}

		

	}
}
