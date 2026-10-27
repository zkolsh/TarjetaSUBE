namespace TarjetaSUBE {
	public class FranjaHorariaFranquicias : IFranjaHoraria {
		private readonly TimeSpan _horaInicio;
		private readonly TimeSpan _horaFin;

		public FranjaHorariaFranquicias() : this(new TimeSpan(6, 0, 0), new TimeSpan(22, 0, 0)) {
		}

		public FranjaHorariaFranquicias(TimeSpan horaInicio, TimeSpan horaFin) {
			_horaInicio = horaInicio;
			_horaFin = horaFin;
		}

		public bool Contiene(DateTime fecha) {
			if (fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday) {
				return false;
			}

			TimeSpan hora = fecha.TimeOfDay;
			return hora >= _horaInicio && hora <= _horaFin;
		}
	}
}
