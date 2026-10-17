using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests { 

	public class ColectivoTests
	{
        private SubeContext _context = null!;
        private Colectivo colectivo = null!;
        private ProveedorFechaSimulado reloj = null!;
        private MedioBoletoEstudiantil medioBoleto = null!;

        [SetUp]
		public void Setup()
		{
			var options = new DbContextOptionsBuilder<SubeContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;

			_context = new SubeContext(options);
			colectivo = new Colectivo { NroInterno = 101, Linea = "102 Negra" };
			_context.Colectivos.Add(colectivo);

            reloj = new ProveedorFechaSimulado
            {
                FechaActual = new DateTime(2026, 10, 17, 8, 0, 0)
            };
           
            medioBoleto = new MedioBoletoEstudiantil
            {
                id = 2,
                nombre = "Medio Boleto Estudiantil",
                porcentaje_descuento = 50
            };

            _context.TiposTarjetas.Add(medioBoleto);
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
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -1000 };

			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta);

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1000));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(0));
		}

      


        [Test]
		public void PagarCon_ConSaldoCero_PermiteViajePlusCreaBoletoYQuedaEnSaldoNegativo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };

			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta);

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1580));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(1));
		}
        [Test]
        public void MedioBoleto_SoloPermiteDosViajesPorDiaConDescuento_ElTerceroPagaNormal()
        {

            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 10000,
                TipoTarjeta = medioBoleto
            };

            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // 1er viaje: 08:00 -> paga $790
            bool viaje1 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje1, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(10000 - 790)); // 9210
            
            // 2do viaje: 09:00 (mismo día) -> paga $790
            reloj.AdelantarMinutos(60);
            bool viaje2 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje2, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(9210 - 790)); // 8420
            
            // 3er viaje: 12:00 (mismo día) -> paga tarifa normal $1580
            reloj.AdelantarMinutos(180);
            bool viaje3 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje3, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(8420 - 1580)); // 6840
            
            // 4to viaje: al día siguiente (08:00) -> se resetea y vuelve a pagar $790
            reloj.AdelantarDias(1);
            bool viaje4 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje4, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(6840 - 790)); // 6050
        }

        [Test]
        public void MedioBoleto_NoPermiteViajarNuevamenteEnMenosDeCincoMinutos()
        {

            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 5000,
                TipoTarjeta = medioBoleto
            };

            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();
            // 1er viaje: 08:00 -> Pasa
            bool viaje1 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje1, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000 - 790)); // 4210
          
            // 2do viaje: 08:03 (pasaron solo 3 min) -> Rebota
            reloj.AdelantarMinutos(3);
            bool viaje2 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje2, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(4210)); // El saldo no se descuenta
           
            
            // 3er intento: 08:06 (pasaron 6 min desde el 1ero) -> Pasa
            reloj.AdelantarMinutos(3);
            bool viaje3 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje3, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(4210 - 790)); // 3420
        }
    }
}
