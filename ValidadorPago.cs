namespace TarjetaSUBE {
	public class ValidadorPago {
		private readonly IServicioTrasbordo m_servicioTrasbordo;
		private readonly ISelectorTarjeta m_selectorTarjeta;

		// DESINVENTEN C# YA!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
		public static ValidadorPago Instancia { get; } = new(new ServicioTrasbordo(), new SelectorTarjeta(new TarjetaNormal()));

		private ValidadorPago(IServicioTrasbordo servicioTrasbordo, ISelectorTarjeta selectorTarjeta) {
			m_servicioTrasbordo = servicioTrasbordo;
			m_selectorTarjeta = selectorTarjeta;
		}

		public bool PagarCon(Colectivo colectivo, SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			tarjeta.TipoTarjeta ??= context.TiposTarjetas.Find(tarjeta.id_beneficio);
			proveedor ??= new ProveedorDeFechaSistema();

			DateTime fechaActual = proveedor.Ahora;
			var tipoEfectivo = m_selectorTarjeta.ResolverTarjeta(context, tarjeta, fechaActual);

			bool esTrasbordo = m_servicioTrasbordo.EsTrasbordoValido(context, tarjeta, colectivo.Linea, fechaActual);

			decimal tarifaBase = colectivo.ObtenerTarifaBase();
			decimal monto = esTrasbordo ? 0m : tipoEfectivo.CalcularTarifa(context, tarjeta, fechaActual, tarifaBase);
			decimal saldoPrevio = tarjeta.Saldo;

			if (tarjeta.Pagar(monto)) {
				decimal totalAbonado = monto;
				if (saldoPrevio < 0 && monto > 0) {
					totalAbonado = monto + Math.Abs(saldoPrevio);
				}

				Boleto boleto = new() {
					TipoTarjeta = tipoEfectivo.nombre ?? "TarjetaNormal",
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
