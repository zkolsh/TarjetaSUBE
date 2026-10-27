using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE.Tests
{
    public class TarjetaNormalTests
    {
        private decimal tarifaNormal = 2150m;
        [TestCase(0, 2150)]    // Viaje 1: tarifa normal
        [TestCase(28, 2150)]   // Viaje 29: tarifa normal
        [TestCase(29, 1720)]   // Viaje 30: 20% descuento (2150 * 0.8 = 1720)
        [TestCase(78, 1720)]   // Viaje 79: 20% descuento
        [TestCase(79, 1612.5)] // Viaje 80: 25% descuento (2150 * 0.75 = 1612.5)
        [TestCase(100, 1612.5)]// Viaje 101 en adelante: 25% descuento
        public void CalcularMonto_AplicaDescuentoSegunCantidadDeViajesDelMes(int viajesMes, decimal montoEsperado)
        {
            var tipoNormal = new TarjetaNormal { id = 1, nombre = "Normal", porcentaje_descuento = 0 };
            decimal resultado = tipoNormal.CalcularMonto(tarifaNormal, viajesMes);
            Assert.That(resultado, Is.EqualTo(montoEsperado));
        }

        [Test]
        public void CalcularMonto_SinEspecificarViajes_DevuelveTarifaNormalCompleta()
        {
            var tipoNormal = new TarjetaNormal { id = 1, nombre = "Normal", porcentaje_descuento = 0 };
            decimal resultado = tipoNormal.CalcularMonto(tarifaNormal);
            Assert.That(resultado, Is.EqualTo(tarifaNormal));
        }
    }
}

