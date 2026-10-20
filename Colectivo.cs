namespace TarjetaSUBE
{
	public class Colectivo
	{

		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		private readonly static decimal tarifaBasica = 1580;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta, IProveedorFecha? proveedor = null)
		{
            proveedor ??= new ProveedorDeFechaSistema();
            DateTime fechaActual = proveedor.Ahora;
            if (tarjeta.TipoTarjeta is MedioBoletoEstudiantil)
            {
                if (EsIntervaloMenorACincoMinutos(context, tarjeta.Id, fechaActual))
                {
                    return false;
                }
            }
            decimal monto = CalcularTarifa(context, tarjeta, fechaActual);

            if (tarjeta.Pagar(monto))
			{
				Boleto boleto = new() {
					Fecha = fechaActual,
					Colectivo = this,
					Monto = monto,
					Tarjeta = tarjeta,
				};

				context.Boletos.Add(boleto);
				context.SaveChanges();
				return true;
			}
			return false;
		}

        private decimal CalcularTarifa(SubeContext context, Tarjeta tarjeta, DateTime fechaActual)
        {

            int viajesHoyConDescuento = context.Boletos
                .Count(b => b.Tarjeta.Id == tarjeta.Id
                         && b.Fecha.Date == fechaActual.Date
                         && b.Monto < tarifaBasica);

            if ((tarjeta.TipoTarjeta is MedioBoletoEstudiantil || tarjeta.TipoTarjeta is BoletoGratuitoEstudiantil) && viajesHoyConDescuento >= 2) { 
               
                    return tarifaBasica;
            }
            return tarjeta.TipoTarjeta?.CalcularMonto(tarifaBasica) ?? tarifaBasica;
        }
    


        private bool EsIntervaloMenorACincoMinutos(SubeContext context, int tarjetaId, DateTime fecha)
            {
                var ultimoBoleto = context.Boletos
                    .Where(b => b.Tarjeta.Id == tarjetaId)
                    .OrderByDescending(b => b.Fecha)
                    .FirstOrDefault();
                return ultimoBoleto != null && (fecha - ultimoBoleto.Fecha).TotalMinutes < 5;
            }



    }
}
