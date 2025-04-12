using System;

namespace Proyecto_relampago.Models
{
    public class PDF
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string RutaPDF { get; set; }
        public string RutaImagen { get; set; }
        public DateTime FechaSubida { get; set; }
        public int UsuarioId { get; set; }
    }
}