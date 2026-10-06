namespace TarjetaSUBE {
	public abstract class TipoTarjeta {
		public required int id {get; set;}
		public required string nombre {get; set;}
		public required float porcentaje_descuento {get; set;}
	}
}
