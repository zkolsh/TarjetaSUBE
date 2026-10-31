using System;
using System.Collections.Generic;
using System.Text;

namespace TarjetaSUBE
{
    public class RetiroBicicleta
    {
        public int Id { get; set; }
        public int Id_Bicicletero { get; set; }
        public Bicicletero Bicicletero { get; set; } = null!;
        public int id_tarjeta { get; set; }
        public Tarjeta Tarjeta { get; set; } = null!;
        public DateTime fecha_hora_retiro { get; set; }
        public DateTime? fecha_hora_devolucion { get; set; }

    }
}
