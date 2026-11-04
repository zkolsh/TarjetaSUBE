namespace TarjetaSUBE {
	public interface ISelectorTarjeta {
		public TipoTarjeta ResolverTarjeta(SubeContext context, Tarjeta tarjeta, DateTime fechaActual);
	}
}
