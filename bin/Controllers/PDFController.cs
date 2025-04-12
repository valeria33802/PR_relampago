using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Proyecto_relampago.Models;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

public class PDFController : Controller
{

    [HttpGet]
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
    [HttpPost]
    public ActionResult Agregar(HttpPostedFileBase ArchivoPDF, HttpPostedFileBase Imagen, string Nombre)
    {
        if (ArchivoPDF == null || Imagen == null || string.IsNullOrEmpty(Nombre))
            return Content("Datos incompletos.");

        // 1) Generar las rutas para guardar archivos
        string pdfPath = System.IO.Path.Combine("/Uploads/PDFs/", System.IO.Path.GetFileName(ArchivoPDF.FileName));
        string imgPath = System.IO.Path.Combine("/Uploads/Imagenes/", System.IO.Path.GetFileName(Imagen.FileName));

        string serverPdfPath = Server.MapPath(pdfPath);
        string serverImgPath = Server.MapPath(imgPath);

        // 2) Guardar en disco
        ArchivoPDF.SaveAs(serverPdfPath);
        Imagen.SaveAs(serverImgPath);

        // 3) Insertar en PDFs y recuperar el ID
        int pdfId = 0;
        string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        using (SqlConnection conn = new SqlConnection(connStr))
        {
            conn.Open();

            // IMPORTANTE: usar OUTPUT INSERTED.Id
            string query = @"
            INSERT INTO PDFs (Nombre, RutaPDF, RutaImagen, UsuarioId) 
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @RutaPDF, @RutaImagen, @UsuarioId);";

            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@Nombre", Nombre);
            cmd.Parameters.AddWithValue("@RutaPDF", pdfPath);
            cmd.Parameters.AddWithValue("@RutaImagen", imgPath);

            // Asegúrate de que Session["UsuarioId"] no sea nulo
            if (Session["UsuarioId"] != null)
                cmd.Parameters.AddWithValue("@UsuarioId", (int)Session["UsuarioId"]);
            else
                cmd.Parameters.AddWithValue("@UsuarioId", 1); // o maneja si no hay usuario logueado

            // Retorna el Id insertado
            pdfId = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 4) Extraer contenido del PDF
        string contenidoPDF = "";
        using (PdfReader reader = new PdfReader(serverPdfPath))
        {
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                contenidoPDF += PdfTextExtractor.GetTextFromPage(reader, i);
            }
        }

        // 5) Insertar en PDFContenido
        using (SqlConnection conn = new SqlConnection(connStr))
        {
            conn.Open();
            string query = "INSERT INTO PDFContenido (PDF_Id, Contenido) VALUES (@PDF_Id, @Contenido)";
            SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@PDF_Id", pdfId);
            cmd.Parameters.AddWithValue("@Contenido", contenidoPDF);
            cmd.ExecuteNonQuery();
        }

        return RedirectToAction("busquedaadmin", "Home");
    }





    public ActionResult BusquedaAdmin()
    {
        List<PDF> listaPDFs = new List<PDF>();

        try
        {
            string conexion = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(conexion))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("ObtenerPDFs", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    listaPDFs.Add(new PDF
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        Nombre = reader["Nombre"].ToString(),
                        RutaPDF = reader["RutaPDF"].ToString(),
                        RutaImagen = reader["RutaImagen"].ToString(),
                        FechaSubida = Convert.ToDateTime(reader["FechaSubida"]),
                        UsuarioId = Convert.ToInt32(reader["UsuarioId"])
                    });
                }
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = "Error al cargar PDFs: " + ex.Message;
        }

        return View("~/Views/Home/busquedaadmin.cshtml", listaPDFs);
    }

    [HttpGet]
    public ActionResult Eliminar(int id)
    {
        string connStr = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        using (SqlConnection conn = new SqlConnection(connStr))
        {
            conn.Open();

            // Eliminar contenido relacionado (dependencias)
            string deleteContenidoQuery = "DELETE FROM PDFContenido WHERE PDF_Id = @Id";
            using (SqlCommand deleteContenidoCmd = new SqlCommand(deleteContenidoQuery, conn))
            {
                deleteContenidoCmd.Parameters.AddWithValue("@Id", id);
                deleteContenidoCmd.ExecuteNonQuery();
            }

            // Obtener rutas de los archivos
            string rutaPDF = "", rutaImagen = "";
            string selectQuery = "SELECT RutaPDF, RutaImagen FROM PDFs WHERE Id = @Id";
            using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn))
            {
                selectCmd.Parameters.AddWithValue("@Id", id);
                using (SqlDataReader reader = selectCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        rutaPDF = reader["RutaPDF"].ToString();
                        rutaImagen = reader["RutaImagen"].ToString();
                    }
                }
            }

            // Eliminar el registro principal
            string deletePDFQuery = "DELETE FROM PDFs WHERE Id = @Id";
            using (SqlCommand deletePDFCmd = new SqlCommand(deletePDFQuery, conn))
            {
                deletePDFCmd.Parameters.AddWithValue("@Id", id);
                deletePDFCmd.ExecuteNonQuery();
            }

            // Eliminar archivos físicos
            string fullPathPDF = Server.MapPath(rutaPDF);
            string fullPathImg = Server.MapPath(rutaImagen);

            if (System.IO.File.Exists(fullPathPDF))
                System.IO.File.Delete(fullPathPDF);

            if (System.IO.File.Exists(fullPathImg))
                System.IO.File.Delete(fullPathImg);
        }

        return RedirectToAction("BusquedaAdmin");
    }

}
