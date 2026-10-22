using NUnit.Framework;
using TarjetaSUBE;
namespace TarjetaSUBE.Tests
{
    public class TipoTarjetaTests
    {
        private decimal tarifaNormal;
       
        [SetUp]
		public void Setup()
		{
			tarifaNormal = 2150;
		}


        [Test]
        public void CalcularMonto_TarjetaNormal_DevuelveTarifaCompleta()
        {
            var tipo = new TarjetaNormal
            {
                id = 1,
                nombre = "Normal",
                porcentaje_descuento = 0
            };
            decimal resultado = tipo.CalcularMonto(tarifaNormal);
            Assert.That(resultado, Is.EqualTo(tarifaNormal));
        }

        [Test]
        public void CalcularMonto_MedioBoletoEstudiantil_DevuelveLaMitad()
        {
            var tipo = new MedioBoletoEstudiantil
            {
                id = 2,
                nombre = "Medio Boleto",
                porcentaje_descuento = 50
            };
            decimal resultado = tipo.CalcularMonto(tarifaNormal);
            Assert.That(resultado, Is.EqualTo(tarifaNormal / 2));
        }

        [Test]
        public void CalcularMonto_FranquiciaCompleta_DevuelveCero()
        {
            var tipo = new FranquiciaCompleta
            {
                id = 3,
                nombre = "Franquicia Completa",
                porcentaje_descuento = 100
            };
            decimal resultado = tipo.CalcularMonto(tarifaNormal);
            Assert.That(resultado, Is.EqualTo(0m));
        }

        [Test]
        public void CalcularMonto_BoletoGratuitoEstudiantil_DevuelveCero()
        {
            var tipo = new BoletoGratuitoEstudiantil
            {
                id = 4,
                nombre = "Boleto Gratuito",
                porcentaje_descuento = 100
            };
            decimal resultado = tipo.CalcularMonto(tarifaNormal);
            Assert.That(resultado, Is.EqualTo(0m));
        }
    }
}
