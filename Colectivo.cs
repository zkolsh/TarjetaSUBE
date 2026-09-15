using System;

namespace Sube
{
	public class Colectivo
	{

        public int NroInterno { get; set; }
        public string Linea { get; set; }
		private readonly static int tarifaBasica = 1580
        
		public Colectivo()
		{
			
		}

		public bool pagarCon(Tarjeta tarjeta)
		{
			if (tarjeta.Pagar(tarifaBasica))
			{
				Boleto boleto = new Boleto {Fecha = DateTime.Now, ColectivoId = this.NroInterno, Monto = tarifaBasica, TarjetaId = tarjeta.Id };
				return true;
			}
			return false
        }

	}
}
