namespace TarjetaSUBE {
	public class ValidadorPago : IValidadorPago {
		private readonly IServicioTrasbordo _servicioTrasbordo;

		public ValidadorPago() : this(new ServicioTrasbordo()) {
		}

		public ValidadorPago(IServicioTrasbordo servicioTrasbordo) {
			_servicioTrasbordo = servicioTrasbordo ?? throw new ArgumentNullException(nameof(servicioTrasbordo));
		}

		public bool PagarCon(Colectivo colectivo, SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			tarjeta.TipoTarjeta ??= context.TiposTarjetas.Find(tarjeta.id_beneficio);

			proveedor ??= new ProveedorDeFechaSistema();
			DateTime fechaActual = proveedor.Ahora;

			if (tarjeta.TipoTarjeta != null && !tarjeta.TipoTarjeta.PuedeViajar(context, tarjeta.Id, fechaActual)) {
				return false;
			}

			bool esTrasbordo = _servicioTrasbordo.EsTrasbordoValido(context, tarjeta, colectivo.Linea, fechaActual);

			decimal tarifaBase = colectivo.ObtenerTarifaBase();
			decimal monto = esTrasbordo ? 0m : (tarjeta.TipoTarjeta?.CalcularTarifa(context, tarjeta, fechaActual, tarifaBase) ?? tarifaBase);
			decimal saldoPrevio = tarjeta.Saldo;

			if (tarjeta.Pagar(monto)) {
				decimal totalAbonado = monto;
				if (saldoPrevio < 0 && monto > 0) {
					totalAbonado = monto + Math.Abs(saldoPrevio);
				}

				Boleto boleto = new() {
					TipoTarjeta = tarjeta.TipoTarjeta?.nombre ?? "TarjetaNormal",
					Fecha = fechaActual,
					Colectivo = colectivo,
					Linea = colectivo.Linea,
					Monto = monto,
					TotalAbonado = totalAbonado,
					Saldo = tarjeta.Saldo,
					SaldoRestante = tarjeta.Saldo,
					Tarjeta = tarjeta,
					EsTrasbordo = esTrasbordo,
				};

				context.Boletos.Add(boleto);
				context.SaveChanges();
				return true;
			}
			return false;
		}
	}
}
