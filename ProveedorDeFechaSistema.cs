using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE
{
	public class ProveedorDeFechaSistema : IProveedorFecha
	{
		public DateTime Ahora => DateTime.Now;
	}
}
