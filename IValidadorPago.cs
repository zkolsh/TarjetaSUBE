namespace TarjetaSUBE {
	public interface IValidadorPago {
		bool PagarCon(Colectivo colectivo, SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor);
	}
}
