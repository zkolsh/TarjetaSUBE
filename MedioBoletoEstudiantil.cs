namespace TarjetaSUBE {
	public class MedioBoletoEstudiantil: TipoTarjeta {
		public override bool TieneRestriccionDeIntervalo => true;
		public override int LimiteViajesConDescuentoPorDia => 2;
	}
}
