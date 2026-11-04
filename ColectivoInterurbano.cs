namespace TarjetaSUBE {
	public class ColectivoInterurbano(IValidadorPago validadorPago) : Colectivo(validadorPago, TarifaInterurbana) {
		public static decimal TarifaInterurbana = 3000m;
	}
}
