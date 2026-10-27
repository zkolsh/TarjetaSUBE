namespace TarjetaSUBE {
	public class Colectivo {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		public static decimal tarifaBasica = 2150m;

		private readonly IValidadorPago _validadorPago;

		public Colectivo() : this(new ValidadorPago()) {
		}

		public Colectivo(IValidadorPago validadorPago) {
			_validadorPago = validadorPago ?? throw new ArgumentNullException(nameof(validadorPago));
		}

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			return _validadorPago.PagarCon(this, context, tarjeta, proveedor);
		}
	}
}
