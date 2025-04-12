using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Proyecto_relampago.Models
{
    /// <summary>
    /// modelo para guardar los resultados de búsqueda
    /// </summary>
    public class PdfSearchResult
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string RutaPDF { get; set; }
        public string RutaImagen { get; set; }
        public string Snippet { get; set; }
    }


}