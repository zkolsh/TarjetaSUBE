namespace TarjetaSUBE {
	public class Colectivo {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		public static decimal tarifaBasica = 2150m;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			tarjeta.TipoTarjeta ??= context.TiposTarjetas.Find(tarjeta.id_beneficio);

			proveedor ??= new ProveedorDeFechaSistema();
			DateTime fechaActual = proveedor.Ahora;

			if (tarjeta.TipoTarjeta != null && !tarjeta.TipoTarjeta.PuedeViajar(context, tarjeta.Id, fechaActual)) {
				return false;
			}

			decimal monto = tarjeta.TipoTarjeta?.CalcularTarifa(context, tarjeta, fechaActual, tarifaBasica) ?? tarifaBasica;
			decimal saldoPrevio = tarjeta.Saldo;

			if (tarjeta.Pagar(monto)) {
				decimal totalAbonado = monto;
				if (saldoPrevio < 0 && monto > 0) {
					totalAbonado = monto + Math.Abs(saldoPrevio);
				}

				Boleto boleto = new() {
					TipoTarjeta = tarjeta.TipoTarjeta?.nombre ?? "TarjetaNormal",
					Fecha = fechaActual,
					Colectivo = this,
					Linea = Linea,
					Monto = monto,
					TotalAbonado = totalAbonado,
					Saldo = tarjeta.Saldo,
					SaldoRestante = tarjeta.Saldo,
					Tarjeta = tarjeta,
				};

				context.Boletos.Add(boleto);
				context.SaveChanges();
				return true;
			}
			return false;
		}
	}
}
