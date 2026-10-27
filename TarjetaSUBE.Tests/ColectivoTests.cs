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
                FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
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

            tarifaNormal = 2150m;
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
			// 5000 - 2150 = 2850
			Assert.That(tarjeta.Saldo, Is.EqualTo(2850));

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
		public void PagarCon_ConSaldoInsuficiente_PermiteViajePlusCreaBoletoYQuedaEnSaldoNegativo()
		{
			var tarjeta = new Tarjeta { dniUsuario = 12345678, Saldo = 200 };

			_context.Tarjetas.Add(tarjeta);

			bool resultado = colectivo.pagarCon(_context, tarjeta, new ProveedorFechaSimulado());

			Assert.That(resultado, Is.True);
			Assert.That(tarjeta.Saldo, Is.EqualTo(-1950)); // 200 - 2150
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

            // 1er viaje: 08:00 -> paga $1075
            bool viaje1 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje1, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(10000 - 1075)); // 8925
            
            // 2do viaje: 09:00 (mismo día) -> paga $1075
            reloj.AdelantarMinutos(60);
            bool viaje2 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje2, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(8925 - 1075)); //7850
            
            // 3er viaje: 12:00 (mismo día) -> paga tarifa normal $2150
            reloj.AdelantarMinutos(180);
            bool viaje3 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje3, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(7850 - 2150)); //5700
            
            // 4to viaje: al día siguiente (08:00) -> se resetea y vuelve a pagar $1075
            reloj.AdelantarDias(1);
            bool viaje4 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje4, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5700 - 1075)); //4625
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
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000 - 1075)); // 3925
          
            // 2do viaje: 08:03 (pasaron solo 3 min) -> Rebota
            reloj.AdelantarMinutos(3);
            bool viaje2 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje2, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(3925)); // El saldo no se descuenta
           
            
            // 3er intento: 08:06 (pasaron 6 min desde el 1ero) -> Pasa
            reloj.AdelantarMinutos(3);
            bool viaje3 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje3, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(3925 - 1075)); //2850
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

            // 3er viaje (10:00, posterior al 2do): cobra precio completo ($2150)
            reloj.AdelantarMinutos(60);
            Assert.That(colectivo.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000 - 2150)); //2850
            
            // Verificamos que el boleto emitido para este 3er viaje registró el monto completo ($2150)
            var boletoTercerViaje = _context.Boletos.OrderByDescending(b => b.Fecha).First();
            Assert.That(boletoTercerViaje.Monto, Is.EqualTo(2150));
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
          
            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);
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

            // 2. Descuenta exactamente la mitad (2150 / 2 = 1075) -> 5000 - 1075 = 3925
            Assert.That(tarjeta.Saldo, Is.EqualTo(3925));
            // 3. El boleto emitido tiene registrado el monto de 1075
            var boletoEmitido = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boletoEmitido.Monto, Is.EqualTo(1075));
        }
        [Test]
        public void TarjetaNormal_EnColectivo_AplicaUsoFrecuenteYSeReseteaAlCambiarDeMes()
        {
            var tipoNormal = new TarjetaNormal { id = 1, nombre = "Normal", porcentaje_descuento = 0 };
            _context.TiposTarjetas.Add(tipoNormal);
            var tarjeta = new Tarjeta
            {
                dniUsuario = 11223344,
                Saldo = 100000,
                TipoTarjeta = tipoNormal,
                id_beneficio = tipoNormal.id
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();
            // Simulamos 29 viajes previos en el mes actual agregando boletos
            for (int i = 0; i < 29; i++)
            {
                _context.Boletos.Add(new Boleto
                {
                    Fecha = reloj.Ahora,
                    Linea = "102 Negra",
                    Monto = 2150,
                    TotalAbonado = 2150,
                    Saldo = 50000,
                    SaldoRestante = 50000,
                    Tarjeta = tarjeta,
                    TarjetaId = tarjeta.Id,
                    TipoTarjeta = "TarjetaNormal",
                    Colectivo = colectivo
                });
            }
            _context.SaveChanges();
            // Viaje numero 30 en el mismo mes: debe cobrar con 20% de descuento ($1720)
            decimal saldoAntesViaje30 = tarjeta.Saldo;
            bool viaje30 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje30, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntesViaje30 - 1720m));

            for (int i = 0; i < 49; i++)
            {
                _context.Boletos.Add(new Boleto
                {
                    Fecha = reloj.Ahora,
                    Linea = "102 Negra",
                    Monto = 1720,
                    TotalAbonado = 1720,
                    Saldo = 50000,
                    SaldoRestante = 50000,
                    Tarjeta = tarjeta,
                    TarjetaId = tarjeta.Id,
                    TipoTarjeta = "TarjetaNormal",
                    Colectivo = colectivo
                });
            }
            _context.SaveChanges();
            // Viaje numero 80: debe cobrar con 25% de descuento ($1612.50)
            decimal saldoAntesViaje80 = tarjeta.Saldo;
            bool viaje80 = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viaje80, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntesViaje80 - 1612.5m)); // 2150 * 0.75 = 1612.5

            // Adelantamos 35 dias (pasamos al mes siguiente): debe volver a cobrar tarifa completa ($2150)
            reloj.AdelantarDias(35);
            decimal saldoAntesNuevoMes = tarjeta.Saldo;
            bool viajeNuevoMes = colectivo.pagarCon(_context, tarjeta, reloj);
            Assert.That(viajeNuevoMes, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(saldoAntesNuevoMes - 2150m));


        }


        [Test]
        public void Colectivo_ConTarifaBasePersonalizada_CobraMontoCorrespondiente()
        {
            var colectivoInterurbano = new Colectivo { NroInterno = 202, Linea = "35/9" };
            colectivoInterurbano.AsignarTarifaBase(3000m);
            _context.Colectivos.Add(colectivoInterurbano);

            var tarjeta = new Tarjeta { dniUsuario = 99887766, Saldo = 10000 };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            bool viaje = colectivoInterurbano.pagarCon(_context, tarjeta, reloj);

            Assert.That(viaje, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(10000 - 3000));
            var boleto = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boleto.Monto, Is.EqualTo(3000));
        }

        [Test]
        public void Colectivo_ConTarifaBasePersonalizadaYMedioBoleto_AplicaDescuentoSobreTarifaEspecifica()
        {
            var colectivoInterurbano = new Colectivo { NroInterno = 203, Linea = "Expreso" };
            colectivoInterurbano.AsignarTarifaBase(3000m);
            _context.Colectivos.Add(colectivoInterurbano);

            var tarjeta = new Tarjeta
            {
                dniUsuario = 99887755,
                Saldo = 10000,

        [Test]
        public void Franquicias_FueraDeFranjaHoraria_TransaccionFallaCompleta()
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
                dniUsuario = 99887766,
                Saldo = 5000,
                TipoTarjeta = franquicia
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Sábado a las 10:00 (fuera de la franja)
            reloj.FechaActual = new DateTime(2026, 10, 24, 10, 0, 0);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.False, "No debe permitir pagar con franquicia en día sábado.");
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000), "El saldo no debe verse afectado.");
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(0), "No debe emitirse ningún boleto.");
        }

        [Test]
        public void MedioBoleto_FueraDeFranjaHoraria_NoPuedePagarEnSabado()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 5000,
                TipoTarjeta = medioBoleto
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Sábado 10:00
            reloj.FechaActual = new DateTime(2026, 10, 24, 10, 0, 0);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(0));
        }

        [Test]
        public void MedioBoleto_AntesDeLasSeis_NoPuedePagar()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 5000,
                TipoTarjeta = medioBoleto
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            bool viaje = colectivoInterurbano.pagarCon(_context, tarjeta, reloj);

            Assert.That(viaje, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(10000 - 1500));
            var boleto = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boleto.Monto, Is.EqualTo(1500));
        }

        [Test]
        public void ColectivoInterurbano_TieneTarifaDe3000PorDefecto()
        {
            var interurbano = new ColectivoInterurbano { NroInterno = 301, Linea = "Gálvez" };
            Assert.That(interurbano.Tarifa, Is.EqualTo(3000m));
            Assert.That(interurbano.ObtenerTarifaBase(), Is.EqualTo(3000m));

            _context.Colectivos.Add(interurbano);
            var tarjeta = new Tarjeta { dniUsuario = 44556677, Saldo = 5000 };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            bool pago = interurbano.pagarCon(_context, tarjeta, reloj);
            Assert.That(pago, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(2000m)); // 5000 - 3000
            var boleto = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boleto.Monto, Is.EqualTo(3000m));
        }

        [Test]
        public void ColectivoInterurbano_ConFranquiciaCompleta_CobraCero()
        {
            var interurbano = new ColectivoInterurbano { NroInterno = 302, Linea = "Baigorria" };
            _context.Colectivos.Add(interurbano);

            var franquicia = new FranquiciaCompleta { id = 5, nombre = "Franquicia Completa", porcentaje_descuento = 100 };
            _context.TiposTarjetas.Add(franquicia);

            var tarjeta = new Tarjeta { dniUsuario = 55667799, Saldo = 1000, TipoTarjeta = franquicia };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            bool pago = interurbano.pagarCon(_context, tarjeta, reloj);
            Assert.That(pago, Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(1000m));
            var boleto = _context.Boletos.First(b => b.Tarjeta.Id == tarjeta.Id);
            Assert.That(boleto.Monto, Is.EqualTo(0m));
        }

        [Test]
        public void ColectivoInterurbano_ConBoletoGratuitoEstudiantil_PrimerosDosViajesGratisYTerceroPagaCompleto()
        {
            var interurbano = new ColectivoInterurbano { NroInterno = 303, Linea = "Gálvez" };
            _context.Colectivos.Add(interurbano);

            var tarjeta = new Tarjeta { dniUsuario = 66778899, Saldo = 5000, TipoTarjeta = boletoGratuito };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // 1er viaje: gratis
            Assert.That(interurbano.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));

            // 2do viaje: gratis
            reloj.AdelantarMinutos(60);
            Assert.That(interurbano.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));

            // 3er viaje: paga tarifa interurbana completa ($3000)
            reloj.AdelantarMinutos(60);
            Assert.That(interurbano.pagarCon(_context, tarjeta, reloj), Is.True);
            Assert.That(tarjeta.Saldo, Is.EqualTo(2000)); // 5000 - 3000
            // Lunes 05:59:59
            reloj.FechaActual = new DateTime(2026, 10, 19, 5, 59, 59);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(0));
        }

        [Test]
        public void MedioBoleto_DespuesDeLasVeintidos_NoPuedePagar()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 12345678,
                Saldo = 5000,
                TipoTarjeta = medioBoleto
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Martes 22:00:01
            reloj.FechaActual = new DateTime(2026, 10, 20, 22, 0, 1);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(0));
        }

        [Test]
        public void BoletoGratuito_FueraDeFranjaHoraria_NoPuedePagar()
        {
            var tarjeta = new Tarjeta
            {
                dniUsuario = 22334455,
                Saldo = 5000,
                TipoTarjeta = boletoGratuito
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Domingo 14:00
            reloj.FechaActual = new DateTime(2026, 10, 25, 14, 0, 0);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.False);
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000));
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(0));
        }

        [Test]
        public void TarjetaNormal_FueraDeFranjaHorariaDeFranquicias_PuedePagarSiempre()
        {
            var tarjetaNormal = new TarjetaNormal
            {
                id = 1,
                nombre = "TarjetaNormal",
                porcentaje_descuento = 0
            };
            _context.TiposTarjetas.Add(tarjetaNormal);

            var tarjeta = new Tarjeta
            {
                dniUsuario = 11112222,
                Saldo = 5000,
                TipoTarjeta = tarjetaNormal
            };
            _context.Tarjetas.Add(tarjeta);
            _context.SaveChanges();

            // Domingo a las 23:00 (fuera de la franja de franquicias)
            reloj.FechaActual = new DateTime(2026, 10, 25, 23, 0, 0);

            bool resultado = colectivo.pagarCon(_context, tarjeta, reloj);

            Assert.That(resultado, Is.True, "TarjetaNormal funciona 24/7.");
            Assert.That(tarjeta.Saldo, Is.EqualTo(5000 - 2150));
            Assert.That(_context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id), Is.EqualTo(1));
        }
    }
}
