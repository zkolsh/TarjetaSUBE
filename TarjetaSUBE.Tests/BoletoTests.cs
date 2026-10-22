using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests
{
	[TestFixture]
	public class BoletoTests
	{
		private SubeContext _context = null!;
		private Colectivo _colectivo = null!;

		[SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
			_colectivo = new Colectivo { NroInterno = 101, Linea = "102 Negra" };
			_context.Colectivos.Add(_colectivo);
			_context.SaveChanges();
		}

		[TearDown]
		public void TearDown()
		{
			_context.Dispose();
		}

		[Test]
		public void Boleto_ConTarjetaNormal_ObtieneDatosCorrectosYTarifaCompleta()
		{
			var tipoNormal = new TarjetaNormal
			{
				id = 1,
				nombre = "TarjetaNormal",
				porcentaje_descuento = 0
			};
			_context.TiposTarjetas.Add(tipoNormal);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 12345678,
				Saldo = 5000,
				TipoTarjeta = tipoNormal,
				id_beneficio = tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			ProveedorFechaSimulado fecha = new();
			DateTime antesDelViaje = fecha.FechaActual;
			bool resultado = _colectivo.pagarCon(_context, tarjeta, fecha);
			fecha.AdelantarMinutos(10);
			DateTime despuesDelViaje = fecha.FechaActual;

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(2850)); //5000 - 2150

			var boleto = _context.Boletos.Include(b => b.Colectivo).Include(b => b.Tarjeta).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);

			Assert.That(boleto!.Fecha, Is.InRange(antesDelViaje, despuesDelViaje));
			Assert.That(boleto.TipoTarjeta, Is.EqualTo("TarjetaNormal"));
			Assert.That(boleto.Linea, Is.EqualTo("102 Negra"));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(2150));
			Assert.That(boleto.Monto, Is.EqualTo(2150));
			Assert.That(boleto.Saldo, Is.EqualTo(2850));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(2850));
			Assert.That(boleto.Tarjeta.Id, Is.EqualTo(tarjeta.Id));
			Assert.That(boleto.TarjetaId, Is.EqualTo(tarjeta.Id));
		}

		[Test]
		public void Boleto_ConMedioBoletoEstudiantil_ObtieneDescuento50Porciento()
		{
			var medioBoleto = new MedioBoletoEstudiantil
			{
				id = 2,
				nombre = "MedioBoletoEstudiantil",
				porcentaje_descuento = 50
			};
			_context.TiposTarjetas.Add(medioBoleto);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 23456789,
				Saldo = 2000,
				TipoTarjeta = medioBoleto,
				id_beneficio = medioBoleto.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = _colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(925)); // 2000 - 1075

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
		public void Boleto_ConBoletoGratuitoEstudiantil_ObtieneTarifaCero()
		{
			var boletoGratuito = new BoletoGratuitoEstudiantil
			{
				id = 3,
				nombre = "BoletoGratuitoEstudiantil",
				porcentaje_descuento = 100
			};
			_context.TiposTarjetas.Add(boletoGratuito);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 34567890,
				Saldo = 1000,
				TipoTarjeta = boletoGratuito,
				id_beneficio = boletoGratuito.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = _colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(1000));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.TipoTarjeta, Is.EqualTo("BoletoGratuitoEstudiantil"));
			Assert.That(boleto.Monto, Is.EqualTo(0));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(0));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(1000));
			Assert.That(boleto.Saldo, Is.EqualTo(1000));
		}

		[Test]
		public void Boleto_ConFranquiciaCompleta_ObtieneTarifaCero()
		{
			var franquiciaCompleta = new FranquiciaCompleta
			{
				id = 4,
				nombre = "FranquiciaCompleta",
				porcentaje_descuento = 100
			};
			_context.TiposTarjetas.Add(franquiciaCompleta);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 45678901,
				Saldo = 0,
				TipoTarjeta = franquiciaCompleta,
				id_beneficio = franquiciaCompleta.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = _colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(0));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.TipoTarjeta, Is.EqualTo("FranquiciaCompleta"));
			Assert.That(boleto.Monto, Is.EqualTo(0));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(0));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(0));
			Assert.That(boleto.Saldo, Is.EqualTo(0));
		}

		/*[Test]
		public void Boleto_ConTarjetaNormalYSaldoNegativo_InformaTotalAbonadoSuperiorATarifaNormal()
		{
			var tipoNormal = new TarjetaNormal
			{
				id = 1,
				nombre = "TarjetaNormal",
				porcentaje_descuento = 0
			};
			_context.TiposTarjetas.Add(tipoNormal);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 56789012,
				Saldo = -200,
				TipoTarjeta = tipoNormal,
				id_beneficio = tipoNormal.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = _colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			// Saldo restante: -200 - 1580 = -1780
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1780));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.Monto, Is.EqualTo(1580));
			Assert.That(boleto.TotalAbonado, Is.GreaterThan(1580));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(1780));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(-1780));
			Assert.That(boleto.Saldo, Is.EqualTo(-1780));
		}*/
		//Test a revisar porque con este saldo Negativo Imposible de hacer.

		[Test]
		public void Boleto_ConMedioBoletoYSaldoNegativo_InformaTotalAbonadoSuperiorATarifaNormal()
		{
			var medioBoleto = new MedioBoletoEstudiantil
			{
				id = 2,
				nombre = "MedioBoletoEstudiantil",
				porcentaje_descuento = 50
			};
			_context.TiposTarjetas.Add(medioBoleto);

			var tarjeta = new Tarjeta
			{
				dniUsuario = 67890123,
				Saldo = -500,
				TipoTarjeta = medioBoleto,
				id_beneficio = medioBoleto.id
			};
			_context.Tarjetas.Add(tarjeta);
			_context.SaveChanges();

			bool resultado = _colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			// Saldo restante: -500 - 1075 = -1575
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1575));

			var boleto = _context.Boletos.OrderByDescending(b => b.Id).FirstOrDefault();
			Assert.That(boleto, Is.Not.Null);
			Assert.That(boleto!.Monto, Is.EqualTo(1075));
			Assert.That(boleto.TotalAbonado, Is.GreaterThan(1075));
			Assert.That(boleto.TotalAbonado, Is.EqualTo(1575));
			Assert.That(boleto.SaldoRestante, Is.EqualTo(-1575));
			Assert.That(boleto.Saldo, Is.EqualTo(-1575));
		}

		[Test]
		public void Boleto_Propiedades_RetornanValoresCorrectos()
		{
			DateTime fecha = new(2026, 10, 19, 10, 0, 0);
			var colectivo = new Colectivo { NroInterno = 101, Linea = "102 Negra" };
			var tarjeta = new Tarjeta { Id = 7, dniUsuario = 12345678, Saldo = 3000 };

			var boleto = new Boleto
			{
				Id = 42,
				Fecha = fecha,
				Colectivo = colectivo,
				Linea = "102 Negra",
				Monto = 1580,
				TotalAbonado = 1780,
				Saldo = -1780,
				SaldoRestante = -1780,
				Tarjeta = tarjeta,
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
			Assert.That(boleto.Colectivo, Is.EqualTo(colectivo));
			Assert.That(boleto.Tarjeta, Is.EqualTo(tarjeta));
		}
	}
}
