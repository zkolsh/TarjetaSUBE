using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests { 

	public class ColectivoTests
	{
        private SubeContext _context = null!;
        private Colectivo colectivo = null!;


        [SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
			colectivo = new Colectivo { NroInterno = 101, Linea = "102 Negra" };
			_context.Colectivos.Add(colectivo);
			_context.SaveChanges();
		}

		[TearDown]
		public void TearDown()
		{
			_context.Dispose();
		}

		[Test]
		public void PagarCon_ConSaldoSuficiente_DescuentaTarifaCreaBoletoYPersiste()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 5000 };

			_context.Tarjetas.Add(tarjeta);
	

			bool resultado = colectivo.pagarCon(_context, tarjeta);

			Assert.That(resultado, Is.True);
			// 5000 - 1580 = 3420
			Assert.That(tarjeta.Saldo, Is.EqualTo(3420));

			var boletos = _context.Boletos.Include(b => b.Colectivo).Include(b => b.Tarjeta).ToList();
			Assert.That(boletos.Count, Is.EqualTo(1));

			var boleto = boletos.First();
			Assert.That(boleto.Monto, Is.EqualTo(1580));
			Assert.That(boleto.Colectivo.NroInterno, Is.EqualTo(101));
			Assert.That(boleto.Tarjeta.Id, Is.EqualTo(tarjeta.Id));
		}

		[Test]
		public void PagarCon_ConSaldoInsuficiente_RetornaFalseYNoCreaBoleto()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 1000 };


			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta);

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(1000));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(0));
		}
	}
}
