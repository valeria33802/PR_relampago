import express from "express";
import { dirname, join } from "path";
import { fileURLToPath } from "url";

const app = express();

// Middleware
app.use(express.urlencoded({ extended: true }));
app.use(express.json());

// Configuración del directorio y archivos estáticos
const dir_name = dirname(fileURLToPath(import.meta.url));
app.set("views", join(dir_name, "views"));
app.use(express.static(join(dir_name, "public")));

// Ruta para servir el archivo index.html
app.get("/", async (req, res) => {
    try {
        res.sendFile(join(dir_name, "public", "index.html")); // Servir index.html estático
    } catch (err) {
        console.error(err);
        res.status(500).send("Error al servir el archivo.");
    }
});

app.get("/login", async (req, res) => {
    try {
        res.sendFile(join(dir_name, "public", "login.html")); // Servir index.html estático
    } catch (err) {
        console.error(err);
        res.status(500).send("Error al servir el archivo.");
    }
});

// Iniciar el servidor
app.listen(process.env.PORT || 3000, () => {
    console.log(
        `Server is listening on port http://localhost:${process.env.PORT || 3000}`
    );
});
