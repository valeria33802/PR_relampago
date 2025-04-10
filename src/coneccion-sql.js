import sql from 'mssql';

export async function openSQL() {
  try {
    const config = {
      user: 'usuariopyrelamp',
      password: 'E9lsd104?',
      server: 'tiusr11pl.cuc-carrera-ti.ac.cr', 
      database: 'tiusr12pl_sqlcoral',
      options: {
        encrypt: true, // Si estás usando una conexión cifrada
        trustServerCertificate: true, // Si tienes problemas con certificados
      },
    };

    // Crear la conexión
    const connection = await sql.connect(config);
    console.log("Conexión exitosa a la base de datos.");

    return connection; // Retorna el pool de conexiones, que se puede usar para hacer consultas
  } catch (error) {
    console.log("Error en openSQL", error);
    throw error; // Lanza el error para que pueda ser manejado en el código que llama a esta función
  }
}