namespace TarjetaSUBE
{
	public class Boleto
	{
		public int Id { get; set; }
		public DateTime Fecha { get; set; }
		public required Colectivo Colectivo { get; set; }
		public decimal Monto { get; set; }
		public required Tarjeta Tarjeta { get; set; }
	}
}
