using System;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests
{
    public class ProveedorFechaSimulado : IProveedorFecha
    {
        public DateTime FechaActual { get; set; }
        public DateTime Ahora => FechaActual;
        public void AdelantarMinutos(double minutos)
        {
            FechaActual = FechaActual.AddMinutes(minutos);
        }
        public void AdelantarDias(double dias)
        {
            FechaActual = FechaActual.AddDays(dias);
        }
    }
}
