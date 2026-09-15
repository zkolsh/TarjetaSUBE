using System;

namespace Sube
{
	public class Tarjeta
	{
		private readonly static HashSet<decimal> CargasPermitidas
			= new HashSet<decimal> {2000,3000,4000,5000,8000,10000,15000,20000,25000,30000};
		public int Id { get; set; }
		public int dniUsuario { get; set; }
		public decimal Saldo { get; set; }
        public Tarjeta()
		{

		}
	}
}
