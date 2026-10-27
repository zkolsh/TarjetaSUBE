namespace TarjetaSUBE {
	public class Colectivo {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }

		private static readonly decimal tarifaBasica = 2150m;

		private readonly decimal Tarifa;

		public virtual decimal ObtenerTarifaBase() => Tarifa;

		private readonly IValidadorPago _validadorPago;

		public Colectivo() : this(new ValidadorPago(), tarifaBasica) {
		}

		public Colectivo(decimal tarifa) : this(new ValidadorPago(), tarifa) {
		}

		public Colectivo(IValidadorPago validadorPago) : this(validadorPago, tarifaBasica) {
		}

		public Colectivo(IValidadorPago validadorPago, decimal tarifa) {
			_validadorPago = validadorPago ?? throw new ArgumentNullException(nameof(validadorPago));
			Tarifa = tarifa;
		}

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			return _validadorPago.PagarCon(this, context, tarjeta, proveedor);
		}
	}
}
