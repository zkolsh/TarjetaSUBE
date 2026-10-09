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

		[Test]
		public void ValidarCarga_SuperandoSaldoMaximoDe40000_RetornaFalse()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 35000 };

			// 35000 + 10000 = 45000 > 40000
			bool esValido = tarjeta.ValidarCarga(10000);

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
	}
}
