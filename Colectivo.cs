using System;

namespace Sube
{
	public class Colectivo
	{

        public int NroInterno { get; set; }
        public string Linea { get; set; }
		private readonly static int tarifaBasica = 1580;
        
		public Colectivo()
		{
			
		}

		public bool pagarCon(Tarjeta tarjeta,SubeContext context)
		{
			if (tarjeta.Pagar(tarifaBasica))
			{
				Boleto boleto = new Boleto {Fecha = DateTime.Now, Colectivo = this, Monto = tarifaBasica, tarjeta = this.tarjeta };
				context.Boletos.Add(boleto);
                context.SaveChanges();
                return true;
			}
			return false;
        }

	}
}
