using NUnit.Framework;
using TarjetaSUBE;

namespace TarjetaSUBE.Tests {
	[TestFixture]
	public class FranjaHorariaFranquiciasTests {
		private FranjaHorariaFranquicias _franja = null!;

		[SetUp]
		public void Setup() {
			_franja = new FranjaHorariaFranquicias();
		}

		[TestCase(2026, 10, 19, 6,  0,  0,  ExpectedResult = true)]   // Lunes 06:00:00 (límite inferior)
		[TestCase(2026, 10, 19, 12, 30, 0,  ExpectedResult = true)]  // Lunes mediodía
		[TestCase(2026, 10, 20, 15, 0,  0,  ExpectedResult = true)]   // Martes
		[TestCase(2026, 10, 21, 10, 0,  0,  ExpectedResult = true)]   // Miércoles
		[TestCase(2026, 10, 22, 18, 0,  0,  ExpectedResult = true)]   // Jueves
		[TestCase(2026, 10, 23, 22, 0,  0,  ExpectedResult = true)]   // Viernes 22:00:00 (límite superior)
		[TestCase(2026, 10, 19, 5,  59, 59, ExpectedResult = false)] // Lunes antes de las 6
		[TestCase(2026, 10, 23, 22, 0,  1,  ExpectedResult = false)]  // Viernes después de las 22
		[TestCase(2026, 10, 24, 12, 0,  0,  ExpectedResult = false)]  // Sábado al mediodía
		[TestCase(2026, 10, 25, 12, 0,  0,  ExpectedResult = false)]  // Domingo al mediodía
		public bool Contiene_ValidacionesDeDiasYHorarios(int anio, int mes, int dia, int hora, int minuto, int segundo) {
			DateTime fecha = new(anio, mes, dia, hora, minuto, segundo);
			return _franja.Contiene(fecha);
		}
	}
}
