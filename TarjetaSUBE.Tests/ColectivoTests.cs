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
        private decimal tarifaNormal; 

        private BoletoGratuitoEstudiantil boletoGratuito = null!;
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

            boletoGratuito = new BoletoGratuitoEstudiantil
            {
                id = 3,
                nombre = "Boleto Gratuito Estudiantil",
                porcentaje_descuento = 100
            };

            _context.TiposTarjetas.Add(boletoGratuito);
            _context.SaveChanges();

            tarifaNormal = 1580m;
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
	

			bool resultado = colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			// 5000 - 1580 = 3420
			Assert.That(tarjeta.Saldo, Is.EqualTo(3420));

			var boletos = _context.Boletos.Include(b => b.Colectivo).Include(b => b.Tarjeta).ToList();
			Assert.That(boletos.Count, Is.EqualTo(1));

			var boleto = boletos.First();
			Assert.That(boleto.Monto, Is.EqualTo(tarifaNormal));
			Assert.That(boleto.Colectivo.NroInterno, Is.EqualTo(101));
			Assert.That(boleto.Tarjeta.Id, Is.EqualTo(tarjeta.Id));
		}

		[Test]
		public void PagarCon_ConSaldoInsuficiente_RetornaFalseYNoCreaBoleto()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = -1000 };

			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.False);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1000));
			Assert.That(_context.Boletos.Count(), Is.EqualTo(0));
		}

      


        [Test]
		public void PagarCon_ConSaldoCero_PermiteViajePlusCreaBoletoYQuedaEnSaldoNegativo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 0 };

			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

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

        [Test]
        public void BoletoGratuito_NoPermiteRealizarMasDeDosViajesGratuitosPorDia()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 11223344,
                Saldo = 10000,
                TipoTarjeta = boletoGratuito
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // 1er viaje: 08:00 -> Gratis ($0)
            colectivo.pagarCon(_context, tarjeta, reloj);

            // 2do viaje: 09:00 -> Gratis ($0)
            reloj.AdelantarMinutos(60);
            colectivo.pagarCon(_context, tarjeta, reloj);

            // 3er viaje: 12:00 -> Ya no es gratis
            reloj.AdelantarMinutos(180);
            colectivo.pagarCon(_context, tarjeta, reloj);

            // 4to viaje: 13:00 -> Ya no es gratis
            reloj.AdelantarMinutos(60);
            colectivo.pagarCon(_context, tarjeta, reloj);
            // Verificamos en la base de datos que la cantidad de boletos con Monto == 0 en el día es exactamente 2
            int boletosGratisDelDia = _context.Boletos
                .Count(b => b.Tarjeta.Id == tarjeta.Id
                         && b.Fecha.Date == reloj.Ahora.Date
                         && b.Monto == 0);
            Assert.That(boletosGratisDelDia, Is.EqualTo(2), "No debe haber más de dos viajes gratuitos en el mismo día.");
        }

        [Test]
        public void BoletoGratuito_ViajesPosterioresAlSegundoSeCobranConPrecioCompleto()
        {
           
            var tarjeta = new Tarjeta
            {
                dniUsuario = 55667788,
                Saldo = 5000,
                TipoTarjeta = boletoGratuito
            };

            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // 1er viaje (08:00): gratis ($0) -> Saldo sigue en 5000
            Assert.That(colectivo.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));

            // 2do viaje (09:00): gratis ($0) -> Saldo sigue en 5000
            reloj.AdelantarMinutos(60);
            Assert.That(colectivo.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));

            // 3er viaje (10:00, posterior al 2do): cobra precio completo ($1580)
            reloj.AdelantarMinutos(60);
            Assert.That(colectivo.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000 - 1580)); // 3420
            
            // Verificamos que el boleto emitido para este 3er viaje registró el monto completo ($1580)
            var boletoTercerViaje = _context.Boletos.OrderByDescending(b => b.Fecha).First();
            Assert.That(boletoTercerViaje.Monto, Is.EqualTo(1580));
        }

        [Test]
        public void FranquiciaCompleta_SiemprePuedePagarBoletoEnColectivo_InclusoSinSaldo()
        {
            var franquicia = new FranquiciaCompleta
            {
                id = 4,
                nombre = "Franquicia Completa",
                porcentaje_descuento = 100
            };
            _context.TiposTarjetas.Add(franquicia);
            var tarjeta = new Tarjeta
            {
                dniUsuario = 12344321,
                Saldo = -2000, // En el límite negativo máximo
                TipoTarjeta = franquicia
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();
          
            bool resultado = colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());
            Assert.That(resultado, Is.True, "Franquicia completa siempre debe poder pagar en el colectivo.");
            Assert.That(tarjeta.Saldo, Is.EqualTo(-2000), "No debe descontar saldo.");
            var boletoEmitido = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boletoEmitido.Monto, Is.EqualTo(0));
        }

        [Test]
        public void MedioBoleto_PrimerViaje_CobraLaMitadDeLaTarifaNormal()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 5000,
                TipoTarjeta = medioBoleto
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Paga el primer viaje
            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);
            // 1. Debe permitir el viaje
            Assert.That(resultado, Is.True);

            // 2. Descuenta exactamente la mitad (1580 / 2 = 790) -> 5000 - 790 = 4210
            Assert.That(tarjeta.Saldo, Is.EqualTo(4210));
            // 3. El boleto emitido tiene registrado el monto de 790
            var boletoEmitido = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boletoEmitido.Monto, Is.EqualTo(790));
        }
    }
}
