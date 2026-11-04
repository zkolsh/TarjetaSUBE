namespace TarjetaSUBE {
	public class Colectivo(IValidadorPago validadorPago, decimal tarifa = Colectivo.TarifaBasica) {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }

		private const decimal TarifaBasica = 2150m;

		protected readonly decimal _tarifa = tarifa;
		private readonly IValidadorPago _validadorPago = validadorPago ?? throw new ArgumentNullException(nameof(validadorPago));

		public virtual decimal ObtenerTarifaBase() => _tarifa;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			return _validadorPago.PagarCon(this, context, tarjeta, proveedor);
		}
	}
}
