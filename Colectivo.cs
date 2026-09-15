namespace TarjetaSUBE
{
	public class Colectivo
	{

		public int NroInterno { get; set; }
		public required string Linea { get; set; }
		private readonly static int tarifaBasica = 1580;

		public bool pagarCon(SubeContext context, Tarjeta tarjeta)
		{
			if (tarjeta.Pagar(tarifaBasica))
			{
				Boleto boleto = new() {
					Fecha = DateTime.Now,
					Colectivo = this,
					Monto = tarifaBasica,
					Tarjeta = tarjeta,
				};

				context.Boletos.Add(boleto);
				context.SaveChanges();
				return true;
			}
			return false;
		}

	}
}
