namespace TarjetaSUBE {
	public class ValidadorPago(IServicioTrasbordo servicioTrasbordo, ISelectorTarjeta selectorTarjeta) : IValidadorPago {
		private readonly IServicioTrasbordo m_servicioTrasbordo = servicioTrasbordo ?? throw new ArgumentNullException(nameof(servicioTrasbordo));
		private readonly ISelectorTarjeta m_selectorTarjeta = selectorTarjeta ?? throw new ArgumentNullException(nameof(selectorTarjeta));

		// DESINVENTEN C# YA!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

		public bool PagarCon(Colectivo colectivo, SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			tarjeta.TipoTarjeta ??= context.TiposTarjetas.Find(tarjeta.id_beneficio);
			proveedor ??= new ProveedorDeFechaSistema();

			DateTime fechaActual = proveedor.Ahora;
			var tipoEfectivo = m_selectorTarjeta.ResolverTarjeta(context, tarjeta, fechaActual);

			bool esTrasbordo = m_servicioTrasbordo.EsTrasbordoValido(context, tarjeta, colectivo.Linea, fechaActual);

			decimal tarifaBase = colectivo.ObtenerTarifaBase();
			decimal monto = esTrasbordo ? 0m : (tarjeta.TipoTarjeta?.CalcularTarifa(context, tarjeta, fechaActual, tarifaBase) ?? tarifaBase);
			decimal saldoPrevio = tarjeta.Saldo;

			if (tarjeta.Pagar(monto)) {
				decimal totalAbonado = monto;
				if (saldoPrevio < 0 && monto > 0) {
					totalAbonado = monto + Math.Abs(saldoPrevio);
				}

				Boleto boleto = new() {
					TipoTarjeta = tipoEfectivo.nombre,
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
