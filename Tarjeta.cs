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
		public bool Pagar(decimal monto)
		{
			if(monto < Saldo)
			{
				Console.WriteLine("Te quedarias con monto negativo no podes pagar.")

				return false;
			}
			Saldo -= monto;
			return true; 
        }

		public bool CargarTarjeta(decimal monto)
        {
            if (CargasPermitidas.Contains(monto))
            {
                Saldo += monto;
                return true;
            }
            else
            {
                if(Saldo+monto > 40000)
				{
					Console.WriteLine("El saldo de la tarjeta no puede superar los 40000 pesos.");
                }
				else
				{
					Console.WriteLine("Monto de carga no permitido. Los montos permitidos son: 2000, 3000, 4000, 5000, 8000, 10000, 15000, 20000, 25000, 30000.");
				}
                return false;
            }
        }
    }
}
