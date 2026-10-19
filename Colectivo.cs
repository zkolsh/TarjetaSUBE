namespace TarjetaSUBE
{
	public class Colectivo
	{
		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		private readonly static int tarifaBasica = 1580;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta)
		{
			if (tarjeta.TipoTarjeta == null)
			{
				tarjeta.TipoTarjeta = context.TiposTarjetas.Find(tarjeta.id_beneficio);
			}

			decimal tarifa = tarifaBasica;
			if (tarjeta.TipoTarjeta != null)
			{
				tarifa = tarifaBasica * (1m - ((decimal)tarjeta.TipoTarjeta.porcentaje_descuento / 100m));
			}

			decimal saldoPrevio = tarjeta.Saldo;

			if (tarjeta.Pagar(tarifa))
			{
				decimal totalAbonado = tarifa;
				if (saldoPrevio < 0 && tarifa > 0)
				{
					totalAbonado = tarifa + Math.Abs(saldoPrevio);
				}

				Boleto boleto = new() {
					Fecha = DateTime.Now,
					Colectivo = this,
					Linea = this.Linea,
					Monto = tarifa,
					TotalAbonado = totalAbonado,
					Saldo = tarjeta.Saldo,
					SaldoRestante = tarjeta.Saldo,
					Tarjeta = tarjeta,
					TarjetaId = tarjeta.Id,
					TipoTarjeta = tarjeta.TipoTarjeta?.nombre?.Trim() ?? "TarjetaNormal"
				};

				context.Boletos.Add(boleto);
				context.SaveChanges();
				return true;
			}
			return false;
		}
	}
}
