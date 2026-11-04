namespace TarjetaSUBE {
	public class Colectivo {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }

		public Colectivo() {
			_tarifa = TarifaBasica;
		}

		public Colectivo(decimal tarifa) {
			_tarifa = tarifa;
		}

		private const decimal TarifaBasica = 2150m;

		protected readonly decimal _tarifa;

		public virtual decimal ObtenerTarifaBase() => _tarifa;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			return ValidadorPago.Instancia.PagarCon(this, context, tarjeta, proveedor);
		}
	}
}
