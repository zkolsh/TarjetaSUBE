namespace TarjetaSUBE {
	public class BoletoGratuitoEstudiantil: TipoTarjeta {
		private static readonly int m_maxViajesLibresDiarios = 2;
		private static readonly IFranjaHoraria FranjaHoraria = new FranjaHorariaFranquicias();

		public override bool PuedeViajar(SubeContext context, int tarjetaId, DateTime fechaActual) {
			return FranjaHoraria.Contiene(fechaActual);
		}

		public override decimal CalcularTarifa(SubeContext context, Tarjeta tarjeta, DateTime fechaActual, decimal tarifaBasica) {
			int viajesHoyConDescuento = context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id && b.Fecha.Date == fechaActual.Date && b.Monto < tarifaBasica);
			if (viajesHoyConDescuento >= m_maxViajesLibresDiarios) {
				return tarifaBasica;
			}
			return CalcularMonto(tarifaBasica);
		}
	}
}
