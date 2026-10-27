namespace TarjetaSUBE {
	public class ColectivoInterurbano : Colectivo {
		public static decimal TarifaInterurbana = 3000m;

		public ColectivoInterurbano() : base(TarifaInterurbana) {
		}

		public ColectivoInterurbano(IValidadorPago validadorPago) : base(validadorPago, TarifaInterurbana) {
		}
	}
}
