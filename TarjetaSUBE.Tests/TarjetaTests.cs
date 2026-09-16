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

		[Test]
		public void Pagar_ConSaldoSuficiente_DescuentaSaldoYRetornaTrue()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 3000 };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(1420));
		}

		[Test]
		public void Pagar_ConSaldoInsuficiente_RetornaFalseYNoModificaSaldo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 1000 };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(1000));
		}

		[Test]
		public void Pagar_ConSaldoExacto_DejaSaldoEnCeroYRetornaTrue()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 1580 };

			bool resultado = tarjeta.Pagar(1580);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(0));
		}
	}
}
