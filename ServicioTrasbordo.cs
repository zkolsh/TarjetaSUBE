using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE {
	public class ServicioTrasbordo : IServicioTrasbordo {
		private readonly IFranjaHoraria _franjaHoraria;
		private readonly TimeSpan _tiempoMaximoTrasbordo;

		public ServicioTrasbordo(IFranjaHoraria? franjaHoraria = null, TimeSpan? tiempoMaximoTrasbordo = null) {
			_franjaHoraria = franjaHoraria ?? new FranjaHorariaTrasbordo();
			_tiempoMaximoTrasbordo = tiempoMaximoTrasbordo ?? TimeSpan.FromHours(1);
		}

		public bool EsTrasbordoValido(SubeContext context, Tarjeta tarjeta, string lineaActual, DateTime fechaActual) {
			if (!_franjaHoraria.Contiene(fechaActual)) {
				return false;
			}

			var boletosTarjeta = context.Boletos
				.Where(b => b.Tarjeta.Id == tarjeta.Id && b.Fecha <= fechaActual)
				.OrderByDescending(b => b.Fecha)
				.Take(20)
				.ToList();

			var ultimoBoleto = boletosTarjeta.FirstOrDefault();
			if (ultimoBoleto == null) {
				return false;
			}

			// Debe ser entre líneas distintas con respecto al último colectivo tomado
			if (ultimoBoleto.Linea == lineaActual) {
				return false;
			}

			// Buscamos el boleto que inició la cadena actual (el más reciente que NO fue trasbordo)
			var primerBoleto = boletosTarjeta.FirstOrDefault(b => !b.EsTrasbordo);
			if (primerBoleto == null) {
				return false;
			}

			if ((fechaActual - primerBoleto.Fecha) > _tiempoMaximoTrasbordo) {
				return false;
			}

			return true;
		}
	}
}
