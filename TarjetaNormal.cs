namespace TarjetaSUBE {
	public class TarjetaNormal: TipoTarjeta {
        public override decimal CalcularMonto(decimal tarifaBasica, int viajesMes = 0)
        {
            if (viajesMes < 29) return tarifaBasica;
            if (viajesMes < 79) return tarifaBasica * 0.8m;
            return tarifaBasica * 0.75m;
        }
	}
}
