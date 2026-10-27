namespace TarjetaSUBE {
	public interface IServicioTrasbordo {
		bool EsTrasbordoValido(SubeContext context, Tarjeta tarjeta, string lineaActual, DateTime fechaActual);
	}
}
