namespace TarjetaSUBE {
	public abstract class TipoTarjeta {
		public required int id {get; set;}
		public required string nombre {get; set;}
		public required float porcentaje_descuento {get; set;}



        public virtual bool PuedeViajar(SubeContext context, int tarjetaId, DateTime fechaActual)
        {
            return true;
        }

        public virtual decimal CalcularMonto(decimal tarifaBasica, int viajesMes = 0)
        {
            return tarifaBasica * (1 - ((decimal)porcentaje_descuento / 100m));
        }

        public virtual decimal CalcularTarifa(SubeContext context, Tarjeta tarjeta, DateTime fechaActual, decimal tarifaBasica)
        {
            return CalcularMonto(tarifaBasica);
        }
    }
}
