import { readdirSync, readFileSync } from 'fs';
import { join } from 'path';
import pdfParse from 'pdf-parse';

// Carpeta donde están los PDFs

// Palabras clave a buscar (puedes modificarlas)
//const palabrasClave = ['contrato', 'confidencialidad', 'pago'];

// Función para leer todos los PDFs y buscar las palabras clave
export async function filtrarPDFsPorPalabras( palabras) {
  const carpetaPDFs = join(__dirname, 'uploads');
  console.log(carpetaPDFs)
  const archivos = readdirSync(carpeta).filter(file => file.endsWith('.pdf'));
  const resultados = [];

  for (const archivo of archivos) {
    const ruta = join(carpeta, archivo);
    const dataBuffer = readFileSync(ruta);

    try {
      const data = await pdfParse(dataBuffer);
      const texto = data.text.toLowerCase();

      // Verificar si el PDF contiene alguna de las palabras clave
      const contiene = palabras.some(palabra => texto.includes(palabra.toLowerCase()));

      if (contiene) {
        resultados.push(archivo);
      }
    } catch (err) {
      console.error(`Error procesando ${archivo}:`, err);
    }
  }

  console.log('PDFs que contienen alguna palabra clave:');
  resultados.forEach(nombre => console.log(`- ${nombre}`));
}

//filtrarPDFsPorPalabras(carpetaPDFs, palabrasClave);
