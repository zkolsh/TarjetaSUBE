namespace TarjetaSUBE {
	public class Colectivo {
		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		public static decimal tarifaBasica = 2150m;
		private decimal? _tarifa;
		public decimal Tarifa {
			get => _tarifa ?? tarifaBasica;
			set => _tarifa = value;
		}

		public virtual decimal ObtenerTarifaBase() => Tarifa;
		public void AsignarTarifaBase(decimal tarifa) => Tarifa = tarifa;



		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor) {
			tarjeta.TipoTarjeta ??= context.TiposTarjetas.Find(tarjeta.id_beneficio);

			proveedor ??= new ProveedorDeFechaSistema();
			DateTime fechaActual = proveedor.Ahora;
			if (tarjeta.TipoTarjeta?.TieneRestriccionDeIntervalo == true) {
				if (EsIntervaloMenorACincoMinutos(context, tarjeta.Id, fechaActual)) {
					return false;
				}
			}

			decimal monto = CalcularTarifa(context, tarjeta, fechaActual);
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

		private decimal CalcularTarifa(SubeContext context, Tarjeta tarjeta, DateTime fechaActual) {
			decimal baseTarifa = ObtenerTarifaBase();
			if (tarjeta.TipoTarjeta == null) {
				return baseTarifa;
			}

			int viajesHoyConDescuento = context.Boletos.Count(b => b.Tarjeta.Id == tarjeta.Id && b.Fecha.Date == fechaActual.Date && b.Monto < baseTarifa);
			if (!tarjeta.TipoTarjeta.PermiteDescuento(viajesHoyConDescuento)) { 
				return baseTarifa;
			}

			    int viajesMes = context.Boletos.Count(b => 
								b.Tarjeta.Id == tarjeta.Id && 
								b.Fecha.Month == fechaActual.Month && 
								b.Fecha.Year == fechaActual.Year);


			return tarjeta.TipoTarjeta?.CalcularMonto(tarifaBasica, viajesMes) ?? tarifaBasica;
		}

		private bool EsIntervaloMenorACincoMinutos(SubeContext context, int tarjetaId, DateTime fecha) {
			var ultimoBoleto = context.Boletos
				.Where(b => b.Tarjeta.Id == tarjetaId)
				.OrderByDescending(b => b.Fecha)
				.FirstOrDefault();
			return ultimoBoleto != null && (fecha - ultimoBoleto.Fecha).TotalMinutes < 5;
		}
	}
}
