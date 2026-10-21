namespace TarjetaSUBE
{
	public class Tarjeta
	{
		private readonly static HashSet<decimal> CargasPermitidas
			= [2000,3000,4000,5000,8000,10000,15000,20000,25000,30000];
		public int Id { get; set; }
		public int dniUsuario { get; set; }
		public decimal Saldo { get; set; }
		public int id_beneficio { get; set; } = 1;
		public virtual TipoTarjeta? TipoTarjeta { get; set; }
		private readonly static decimal SaldoMaximo = 56000;
		private readonly static decimal LimiteNegativo = -2000;
		public decimal SaldoPendiente {get;set;} = 0;

		public bool Pagar(decimal monto)
		{
			if((Saldo - monto) < LimiteNegativo)
			{
				Console.WriteLine("Te quedarias con monto menor al permitido.No podes Pagar.");
				return false;
			}
			
			Saldo -= monto;

			AcreditarCarga(); 
			
			return true; 
		}

		public bool CargarTarjeta(decimal monto,SubeContext context)
		{
			if (ValidarCarga(monto))
			{
			 	SaldoPendiente += monto;
        		AcreditarCarga();
        		context.SaveChanges();
        		return true;
			}
			return false;
		}

		public bool ValidarCarga(decimal monto)
		{
			return CargasPermitidas.Contains(monto);
		}

		public decimal AcreditarCarga()
		{
    		if (SaldoPendiente <= 0) return 0;
    		decimal espacioDisponible = SaldoMaximo - Saldo;
    		if (espacioDisponible <= 0) return 0;
			decimal saldoPrevio = Saldo;
			if (SaldoPendiente <= espacioDisponible)
			{
				Saldo += SaldoPendiente;
				SaldoPendiente = 0;
			}
			else
			{
				Saldo = SaldoMaximo;
				SaldoPendiente -= espacioDisponible;
			}
			if(saldoPrevio >= 0){
				return 0;
			}
			if(Saldo >= 0){
			    return  Math.Abs(saldoPrevio);
			}
			return Saldo - saldoPrevio;
		}


	}
}
