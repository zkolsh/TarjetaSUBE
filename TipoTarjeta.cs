namespace TarjetaSUBE {
	public abstract class TipoTarjeta {
		public required int id {get; set;}
		public required string nombre {get; set;}
		public required float porcentaje_descuento {get; set;}

        public virtual bool TieneRestriccionDeIntervalo => false;
        public virtual int LimiteViajesConDescuentoPorDia => int.MaxValue;

        public virtual bool PermiteDescuento(int viajesHoyConDescuento)
        {
            return viajesHoyConDescuento < LimiteViajesConDescuentoPorDia;
        }

        public virtual decimal CalcularMonto(decimal tarifa)
        {
            return tarifa * (1 - ((decimal)porcentaje_descuento / 100m));
        }
    }
}
