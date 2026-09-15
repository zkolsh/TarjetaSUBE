using System;

namespace Sube
{
	public class Boleto
	{
		public int Id { get; set; }
		public DateTime Fecha { get;set }
		public Colectivo colectivo { get; set; }
		public decimal Monto { get; set; }
		public Tarjeta tarjeta { get; set; }
	}
}
