namespace TarjetaSUBE {
	public class MedioBoletoEstudiantil: TipoTarjeta {
		private static readonly IFranjaHoraria FranjaHoraria = new FranjaHorariaFranquicias();

		public override bool PuedeViajar(SubeContext context, int tarjetaId, DateTime fechaActual) {
			if (!FranjaHoraria.Contiene(fechaActual)) {
				return false;
			}

			var ultimoBoleto = context.Boletos
				.Where(b => b.Tarjeta.Id == tarjetaId)
				.OrderByDescending(b => b.Fecha)
				.FirstOrDefault();
			if (ultimoBoleto != null && (fechaActual - ultimoBoleto.Fecha).TotalMinutes < 5) {
				return false;
			}
			return true;
		}

		public override decimal CalcularTarifa(SubeContext context, Tarjeta tarjeta, DateTime fechaActual, decimal tarifaBasica) {
			int viajesHoyConDescuento = context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id && b.Fecha.Date == fechaActual.Date && b.Monto < tarifaBasica);
			if (viajesHoyConDescuento >= 2) {
				return tarifaBasica;
			}
			return CalcularMonto(tarifaBasica);
		}
	}
}
