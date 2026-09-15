using System;

namespace Sube
{
	public class Boleto
	{
		public int Id { get; set; }
		public DateTime Fecha { get;set }
		public int ColectivoId { get; set; }
		public decimal Monto { get; set; }
		public int TarjetaId { get; set; }
		
		
		
	}
}
