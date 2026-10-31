using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE
{
    public class Bicicletero
    {
        public int id { get; set; }
        public string ubicacion { get; set; } = "";
        public int cantidad_bicicletas { get; set; }

        private static readonly decimal tarifaDiaria = 2418.75m;
        private static readonly decimal multa = 1250m;
        public bool PagarConTarjeta(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor)

        {
            decimal pago = tarifaDiaria;
            if(cantidad_bicicletas == 0)
            {
                Console.WriteLine("No se puede realizar el pago, no hay bicicletas disponibles.");
                return false;
            }
            DateTime fechaActual = proveedor.Ahora;
            RetiroBicicleta? retiro = context.RetiroBicicleta
                  .Where(r => r.id_tarjeta == tarjeta.Id)
                  .OrderByDescending(r => r.fecha_hora_retiro)
                  .FirstOrDefault();

            if (retiro != null)
            {
                if (retiro.fecha_hora_devolucion - retiro.fecha_hora_retiro > TimeSpan.FromHours(1))
                {
                    Console.WriteLine("Vas a tener que pagar multa usaste la bici mas de una hora la ultima vez");
                    pago += multa;
                }

                if (retiro.fecha_hora_devolucion == null)
                {
                    Console.WriteLine("No se puede realizar el pago, la bicicleta no ha sido devuelta.");
                    return false;
                }
                if (retiro.fecha_hora_retiro.Date == proveedor.Ahora.Date)
                {

                    pago -= tarifaDiaria;
                }
            }


            if (pago > 0 && !tarjeta.Pagar(pago))
            {
                Console.WriteLine("No se puede realizar el pago, saldo insuficiente.");
                return false;
            }
            RegistrarRetiro(context, tarjeta, proveedor);
            return true;

        }

        public bool DevolverBicicleta(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor)
        {
            DateTime fechaActual = proveedor.Ahora;
            RetiroBicicleta? retiro = context.RetiroBicicleta
                  .Where(r => r.id_tarjeta == tarjeta.Id)
                  .OrderByDescending(r => r.fecha_hora_retiro)
                  .FirstOrDefault(); 
            
            if (retiro == null)
            {
                Console.WriteLine("No se puede devolver la bicicleta, no hay un retiro registrado.");
                return false;
            }
            if (retiro.fecha_hora_devolucion != null)
            {
                Console.WriteLine("No se puede devolver la bicicleta, ya fue devuelta.");
                return false;
            }

          
            retiro.fecha_hora_devolucion = fechaActual;
            cantidad_bicicletas++;
            context.SaveChanges();
            return true;
        }

        private void RegistrarRetiro(SubeContext context, Tarjeta tarjeta, IProveedorFecha proveedor)
        {
            DateTime fechaActual = proveedor.Ahora;
            RetiroBicicleta bicleta = new RetiroBicicleta
            {
                Id_Bicicletero = this.id,
                id_tarjeta = tarjeta.Id,
                fecha_hora_retiro = fechaActual
            };
            cantidad_bicicletas--;
            context.RetiroBicicleta.Add(bicleta);
            context.SaveChanges();
        }
    }
}
