namespace TarjetaSUBE {
	public abstract class TipoTarjeta {
		public required int id {get; set;}
		public required string nombre {get; set;}
		public required float porcentaje_descuento {get; set;}

        public decimal CalcularMonto(decimal tarifaBasica)
        {
            return tarifaBasica * (1 - ((decimal)porcentaje_descuento / 100m));
        }

    }
}
