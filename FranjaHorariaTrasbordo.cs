namespace TarjetaSUBE {
	public class FranjaHorariaTrasbordo : IFranjaHoraria {
		private readonly TimeSpan _horaInicio;
		private readonly TimeSpan _horaFin;

		public FranjaHorariaTrasbordo() : this(new TimeSpan(7, 0, 0), new TimeSpan(22, 0, 0)) {
		}

		public FranjaHorariaTrasbordo(TimeSpan horaInicio, TimeSpan horaFin) {
			_horaInicio = horaInicio;
			_horaFin = horaFin;
		}

		public bool Contiene(DateTime fecha) {
			if (fecha.DayOfWeek == DayOfWeek.Sunday) {
				return false;
			}

			TimeSpan hora = fecha.TimeOfDay;
			return hora >= _horaInicio && hora <= _horaFin;
		}
	}
}
