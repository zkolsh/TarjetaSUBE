namespace TarjetaSUBE
{
	public class Boleto
	{
		public int Id { get; set; }
		public DateTime Fecha { get; set; }
		public required Colectivo Colectivo { get; set; }
		public required string Linea { get; set; }
		public decimal Monto { get; set; }
		public decimal TotalAbonado { get; set; }
		public decimal Saldo { get; set; }
		public decimal SaldoRestante { get; set; }
		public int TarjetaId { get; set; }
		public required Tarjeta Tarjeta { get; set; }
		public required string TipoTarjeta { get; set; }
		public bool EsTrasbordo { get; set; } = false;
	}
}
