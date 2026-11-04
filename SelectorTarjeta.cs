namespace TarjetaSUBE {
	public class SelectorTarjeta(TipoTarjeta tarjetaNormal) : ISelectorTarjeta {
		private readonly TipoTarjeta m_tarjetaNormal = tarjetaNormal;

		public TipoTarjeta ResolverTarjeta(SubeContext context, Tarjeta tarjeta, DateTime fechaActual) {
			var tt = tarjeta.TipoTarjeta ?? m_tarjetaNormal;
			if (tt.PuedeViajar(context, tarjeta.Id, fechaActual)) {
				return tt;
			} else {
				// Degenerar a tarjeta normal usando una tarjeta que NO se guarda en el context (recicla ID)
				return m_tarjetaNormal;
			}
		}
	}
}
