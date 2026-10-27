namespace TarjetaSUBE {
	public class ColectivoInterurbano : Colectivo {
		public const decimal TarifaInterurbana = 3000m;

		public ColectivoInterurbano() {
			Tarifa = TarifaInterurbana;
		}
	}
}
