using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE.Tests
    {
        [TestFixture]
        public class BicicleteroTest
        {
            private SubeContext _context = null!;
            private ProveedorFechaSimulado _fechaSimulada = null!;
            private Bicicletero _bicicletero = null!;
            private Tarjeta _tarjeta = null!;
            private decimal  _costoRetiro = 2418.75m;

            private decimal _multa = 1250m;

        

        [SetUp]
            public void Setup()
            {
                var options = new DbContextOptionsBuilder<SubeContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

                _context = new SubeContext(options);
                
                _bicicletero = new Bicicletero
                {
                    id = 1,
                    ubicacion = "Calle Falsa 123",
                    cantidad_bicicletas = 5
                };


                TipoTarjeta Normal = new TarjetaNormal
                {
                    id = 1,
                    nombre = "TarjetaNormal",
                    porcentaje_descuento = 0
                };

                TipoTarjeta FranquiciaCompleta = new FranquiciaCompleta
                {
                    id = 2,
                    nombre = "TarjetaFranquiciaCompleta",
                    porcentaje_descuento = 100
                };

                TipoTarjeta MedioBoletoEstudiantil = new MedioBoletoEstudiantil
                {
                    id = 3,
                    nombre = "TarjetaMedioBoletoEstudiantil",
                    porcentaje_descuento = 50
                };

                TipoTarjeta BoletoEducativo = new BoletoGratuitoEstudiantil
                {
                    id = 4,
                    nombre = "TarjetaBoletoEducativo",
                    porcentaje_descuento = 100
                };

                _tarjeta = new Tarjeta
                {
                    dniUsuario = 12345678,
                    Saldo = 5000,
                    TipoTarjeta = Normal,
                    id_beneficio = Normal.id
                };
                _context.Bicicleteros.Add(_bicicletero);
                _context.TiposTarjetas.Add(Normal);
                _context.TiposTarjetas.Add(FranquiciaCompleta);
                _context.TiposTarjetas.Add(MedioBoletoEstudiantil);
                _context.TiposTarjetas.Add(BoletoEducativo);
                _context.Tarjetas.Add(_tarjeta);
        
                _context.SaveChanges();

                _fechaSimulada = new ProveedorFechaSimulado
                {
                    FechaActual = new DateTime(2026, 10, 19, 8, 0, 0)
                };
            }

            [TearDown]
            public void TearDown()
            {
                _context.Dispose();
            }

        [Test]
        //Sin Retiros Previos
        public void PagoYRetiroExitosamente()
        {
            decimal saldoOriginal = _tarjeta.Saldo;
     

            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultado, Is.True);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
                .Where(r => r.id_tarjeta == _tarjeta.Id)
                .Include(r => r.Tarjeta)
                .OrderByDescending(r => r.fecha_hora_retiro)
                .FirstOrDefault();

         
            Assert.That(retiro.Tarjeta.Saldo, Is.EqualTo(saldoOriginal - _costoRetiro));
           
        }

        [Test]

        public void DevolucionExitosamente()
        {
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);

            bool resultadoDevolucion = _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultadoDevolucion, Is.True);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
                .Where(r => r.id_tarjeta == _tarjeta.Id)
                .Include(r => r.Tarjeta)
                .OrderByDescending(r => r.fecha_hora_retiro)
                .FirstOrDefault();

            Assert.That(retiro.fecha_hora_devolucion, Is.EqualTo(_fechaSimulada.FechaActual));
        }

        [Test]
        public void SiNuncaDejasteNoPodesDevolver()
        {
            bool resultadoDevolucion = _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultadoDevolucion, Is.False);
        }

        [Test]
        public void SiYaDevolvisteNoPodesDevolver()
        {
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            bool resultadoDevolucion = _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultadoDevolucion, Is.False);
        }

        [Test]
        public void RetirasteYDevolvisteEnTiempoYRetirasDeNuevoEnDiaEsGratis()
        {
            decimal saldoOriginal = _tarjeta.Saldo;
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(20);
            
            RetiroBicicleta? retiroOriginal = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();

            decimal saldoMedio = saldoOriginal - _costoRetiro;
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(20); 

            bool resultadoRetiro = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();
            Assert.That(resultadoRetiro, Is.True);
            Assert.That(retiro.Tarjeta.Saldo, Is.EqualTo(saldoMedio));
            Assert.That(retiroOriginal.fecha_hora_devolucion - retiroOriginal.fecha_hora_retiro, Is.LessThan(TimeSpan.FromHours(1)));


        }

        [Test]

        public void RetirasteYDevolvisteEnTiempoYEsOtroDiaPagas()
        {
            decimal saldoOriginal = _tarjeta.Saldo;
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(20);

            RetiroBicicleta? retiroOriginal = _context.RetiroBicicleta
             .Where(r => r.id_tarjeta == _tarjeta.Id)
             .Include(r => r.Tarjeta)
             .OrderByDescending(r => r.fecha_hora_retiro)
             .FirstOrDefault();

            decimal saldoMedio = saldoOriginal - _costoRetiro;
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarDias(1);

            bool resultadoRetiro = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();

            Assert.That(resultadoRetiro, Is.True);
            Assert.That(retiro.Tarjeta.Saldo, Is.EqualTo(saldoMedio-_costoRetiro));
            Assert.That(retiroOriginal.fecha_hora_devolucion - retiroOriginal.fecha_hora_retiro, Is.LessThan(TimeSpan.FromHours(1)));
        }

        [Test]

        public void RetirasteYDevolvisteTardeMulta()
        {
            decimal saldoOriginal = _tarjeta.Saldo;
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(80);

            RetiroBicicleta? retiroOriginal = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();

            decimal saldoMedio = saldoOriginal - _costoRetiro;
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(20);

            bool resultadoRetiro = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();
            Assert.That(resultadoRetiro, Is.True);
            Assert.That(retiro.Tarjeta.Saldo, Is.EqualTo(saldoMedio - _multa));
            Assert.That(retiroOriginal.fecha_hora_devolucion - retiroOriginal.fecha_hora_retiro, Is.GreaterThan(TimeSpan.FromHours(1)));

        }

        [Test]

        public void RetirasteYDevolvisteTardeMultaYEsOtroDiaPagas()
        {
            decimal saldoOriginal = _tarjeta.Saldo;
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(80);

            RetiroBicicleta? retiroOriginal = _context.RetiroBicicleta
             .Where(r => r.id_tarjeta == _tarjeta.Id)
             .Include(r => r.Tarjeta)
             .OrderByDescending(r => r.fecha_hora_retiro)
             .FirstOrDefault();

            decimal saldoMedio = saldoOriginal - _costoRetiro;
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarDias(1);

            bool resultadoRetiro = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);

            RetiroBicicleta? retiro = _context.RetiroBicicleta
               .Where(r => r.id_tarjeta == _tarjeta.Id)
               .Include(r => r.Tarjeta)
               .OrderByDescending(r => r.fecha_hora_retiro)
               .FirstOrDefault();

            Assert.That(resultadoRetiro, Is.True);
            Assert.That(retiro.Tarjeta.Saldo, Is.EqualTo(saldoMedio - _costoRetiro - _multa));
            Assert.That(retiroOriginal.fecha_hora_devolucion - retiroOriginal.fecha_hora_retiro, Is.GreaterThan(TimeSpan.FromHours(1)));

        }

        [Test]
        public void RetirasteYDevolvisteTardeVariasVecesElMismoDiaAcumulaMultas()
        {
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);  
            _fechaSimulada.AdelantarMinutos(80);
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(20);

            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);   //paga multa 1
            _fechaSimulada.AdelantarMinutos(80);
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada); 
            _fechaSimulada.AdelantarMinutos(20);

            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada); //paga multa 2

            Assert.That(resultado, Is.True);
            Assert.That(_tarjeta.Saldo, Is.EqualTo(5000 - _costoRetiro - 2 * _multa));
        }

        [Test]
        public void NoHayBicicletasDisponibles()
        {
            _bicicletero.cantidad_bicicletas = 0;
            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void NoHaySaldoSuficiente()
        {
            _tarjeta.Saldo = 0;
            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultado, Is.False);
        }

        [Test]
        public void HaySaldoSuficientePeroMulta()
        {
       
            _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            _fechaSimulada.AdelantarMinutos(80);
            _bicicletero.DevolverBicicleta(_context, _tarjeta, _fechaSimulada);
            
            _fechaSimulada.AdelantarDias(1);

            _tarjeta.Saldo = 1000; // Saldo insuficiente para cubrir la multa
            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultado, Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void NoImportaTuFranquiciaPagas(int id_tipo_tarjeta)
        {
            _tarjeta.TipoTarjeta = _context.TiposTarjetas.FirstOrDefault(t => t.id == id_tipo_tarjeta)!;

            _tarjeta.id_beneficio = _tarjeta.TipoTarjeta.id;

            decimal saldoInicial = _tarjeta.Saldo;
            bool resultado = _bicicletero.PagarConTarjeta(_context, _tarjeta, _fechaSimulada);
            Assert.That(resultado, Is.True);
            Assert.That(_tarjeta.Saldo, Is.EqualTo(saldoInicial - _costoRetiro));
        }
    }
}
