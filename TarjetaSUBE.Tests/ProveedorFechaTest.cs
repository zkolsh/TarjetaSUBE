using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE.Tests
{
    internal class ProveedorFechaTest
    {
        [Test]
        public void Ahora_DevuelveFechaYHoraActualDelSistema()
        {
            var proveedor = new ProveedorDeFechaSistema();
            Assert.That(proveedor.Ahora, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromSeconds(1)));
        }
    }
}
