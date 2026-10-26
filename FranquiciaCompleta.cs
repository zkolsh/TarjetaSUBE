namespace TarjetaSUBE {
	public class FranquiciaCompleta: TipoTarjeta {
		private static readonly IFranjaHoraria FranjaHoraria = new FranjaHorariaFranquicias();

		public override bool PuedeViajar(SubeContext context, int tarjetaId, DateTime fechaActual) {
			return FranjaHoraria.Contiene(fechaActual);
		}
	}
}
