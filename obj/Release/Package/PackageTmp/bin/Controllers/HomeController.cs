using Proyecto_relampago.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace Proyecto_relampago.Controllers
{
   
    public class HomeController : Controller
	{
        [HttpPost]
        public ActionResult Agregar(HttpPostedFileBase ArchivoPDF, HttpPostedFileBase Imagen, string Nombre)
        {
            if (ArchivoPDF == null || Imagen == null || string.IsNullOrEmpty(Nombre))
                return Content("Datos incompletos.");

            // Generar rutas para archivos
            string pdfPath = System.IO.Path.Combine("/Uploads/PDFs/", System.IO.Path.GetFileName(ArchivoPDF.FileName));
            string imgPath = System.IO.Path.Combine("/Uploads/Imagenes/", System.IO.Path.GetFileName(Imagen.FileName));

            string serverPdfPath = Server.MapPath(pdfPath);
            string serverImgPath = Server.MapPath(imgPath);

            // Guardar archivos en disco
            try
            {
                ArchivoPDF.SaveAs(serverPdfPath);
                Imagen.SaveAs(serverImgPath);
            }
            catch (Exception ex)
            {
                return Content("Error al guardar los archivos: " + ex.Message);
            }

            // Recuperar usuario de la sesión
            int usuarioId;
            if (Session["UsuarioId"] != null)
                usuarioId = (int)Session["UsuarioId"];
            else
                usuarioId = 1; // Valor por defecto o gestionarlo adecuadamente

            int pdfId = 0;
            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // Usamos OUTPUT INSERTED.Id para obtener el ID insertado
                string query = @"
            INSERT INTO PDFs (Nombre, RutaPDF, RutaImagen, UsuarioId) 
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @RutaPDF, @RutaImagen, @UsuarioId);";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Nombre", Nombre);
                cmd.Parameters.AddWithValue("@RutaPDF", pdfPath);
                cmd.Parameters.AddWithValue("@RutaImagen", imgPath);
                cmd.Parameters.AddWithValue("@UsuarioId", usuarioId);

                // Obtener el Id insertado
                try
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        pdfId = Convert.ToInt32(result);
                        System.Diagnostics.Debug.WriteLine("pdfId obtenido: " + pdfId);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("pdfId es null");
                        return Content("No se pudo obtener el ID del PDF insertado.");
                    }
                }
                catch (Exception ex)
                {
                    return Content("Error al insertar PDF: " + ex.Message);
                }
            }

            // Extraer contenido del PDF usando iTextSharp
            string contenidoPDF = "";
            try
            {
                using (PdfReader reader = new PdfReader(serverPdfPath))
                {
                    for (int i = 1; i <= reader.NumberOfPages; i++)
                    {
                        contenidoPDF += PdfTextExtractor.GetTextFromPage(reader, i);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error al extraer el contenido del PDF: " + ex.Message);
                return Content("Error al extraer el contenido del PDF: " + ex.Message);
            }

            // Verificar que se haya extraído contenido
            if (string.IsNullOrEmpty(contenidoPDF))
            {
                System.Diagnostics.Debug.WriteLine("El contenido extraído del PDF está vacío.");
                // Puedes decidir si deseas proceder o no
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Contenido extraído: " + contenidoPDF.Substring(0, Math.Min(100, contenidoPDF.Length)));
            }

            // Insertar el contenido en la tabla PDFContenido
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                try
                {
                    conn.Open();
                    string query = "INSERT INTO PDFContenido (PDF_Id, Contenido) VALUES (@PDF_Id, @Contenido)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@PDF_Id", pdfId);
                    cmd.Parameters.AddWithValue("@Contenido", contenidoPDF);
                    int rowsAffected = cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("Filas afectadas en PDFContenido: " + rowsAffected);

                    if (rowsAffected == 0)
                    {
                        return Content("No se insertó contenido en PDFContenido.");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error al insertar en PDFContenido: " + ex.Message);
                    return Content("Error al insertar en PDFContenido: " + ex.Message);
                }
            }

            return RedirectToAction("busquedaadmin", "Home");
        }

        [HttpPost]
        public ActionResult Registrar(Usuario model)
        {
            if (ModelState.IsValid)
            {
                string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO Usuarios (Correo, Usuario, Contrasena, Rol) VALUES (@Correo, @Usuario, @Contrasena, @Rol)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Correo", model.Correo);
                    cmd.Parameters.AddWithValue("@Usuario", model.UsuarioNombre);
                    cmd.Parameters.AddWithValue("@Contrasena", model.Contrasena); // Puedes aplicar hashing aquí
                    cmd.Parameters.AddWithValue("@Rol", "cliente"); // Siempre como cliente al registrarse

                    conn.Open();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }

                TempData["Mensaje"] = "¡Registro exitoso!";
                return RedirectToAction("Index");
            }

            return View("Index");
        }

        [HttpPost]
        public ActionResult IniciarSesion(string loginUsername, string loginPassword)
        {
            string cadena = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                string sql = "SELECT Usuario, Rol, Id FROM Usuarios WHERE Usuario = @usuario AND Contrasena = @contrasena";
                SqlCommand cmd = new SqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@usuario", loginUsername);
                cmd.Parameters.AddWithValue("@contrasena", loginPassword);

                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    string rol = reader["Rol"].ToString();
                    Session["UsuarioId"] = reader["Id"];

                    if (rol == "administrador")

                        return RedirectToAction("busquedaadmin", "Home");
                    else
                        return RedirectToAction("busquedaclientes", "Home");
                }
                else
                {
                    TempData["ErrorLogin"] = "Usuario o contraseña incorrectos.";
                    return RedirectToAction("Index");
                }
            }
        }

        public ActionResult Index()
		{
			return View();
		}

		public ActionResult busquedaclientes()
		{

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}

        public ActionResult busquedaadmin()
        {
            return RedirectToAction("BusquedaAdmin", "PDF");
        }

        //public ActionResult Buscar()
        //{
        //    return View();
        //}


        public ActionResult Buscar(string termino)
        {
            // Elimina espacios en blanco del término
            //termino = termino?.Trim();

            if (string.IsNullOrEmpty(termino))
            {
                return View(new List<PdfSearchResult>());
            }

            List<PdfSearchResult> resultados = new List<PdfSearchResult>();
            string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    // Se usa LOWER para hacer búsqueda insensible a mayúsculas
                    string query = @"
                SELECT p.Id, p.Nombre, p.RutaPDF, p.RutaImagen, pc.Contenido 
                FROM PDFs p
                INNER JOIN PDFContenido pc ON p.Id = pc.PDF_Id
                WHERE LOWER(pc.Contenido) LIKE '%' + LOWER(@Termino) + '%'";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Termino", termino);
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        string contenido = reader["Contenido"].ToString();
                        string snippet = "";

                        int index = contenido.IndexOf(termino, StringComparison.OrdinalIgnoreCase);
                        if (index >= 0)
                        {
                            int start = Math.Max(0, index - 30);
                            int length = Math.Min(100, contenido.Length - start);
                            snippet = contenido.Substring(start, length);
                        }

                        resultados.Add(new PdfSearchResult
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            RutaPDF = reader["RutaPDF"].ToString(),
                            RutaImagen = reader["RutaImagen"].ToString(),
                            Snippet = snippet
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Content("Se produjo un error al buscar el término: " + ex.Message);
            }

            // Depuración: escribe en la salida cuántos resultados se encontraron
            System.Diagnostics.Debug.WriteLine("Resultados encontrados: " + resultados.Count);

            return View(resultados);
        }



    }
}