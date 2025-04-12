namespace Proyecto_relampago.Models
{
    public class Usuario
    {
        public string Correo { get; set; }
        public string UsuarioNombre { get; set; }  // Evitamos conflicto con la clase Usuario
        public string Contrasena { get; set; }
        public string Rol { get; set; }
    }
}


