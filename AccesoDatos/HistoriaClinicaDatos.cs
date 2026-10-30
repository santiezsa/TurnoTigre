using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    /// <summary>
    /// Historia clínica. Los datos de salud se guardan cifrados (<see cref="Cifrador"/>): grupo sanguíneo, alergias, enfermedades crónicas,
    /// patologías, los textos de cada consulta y el contenido de los archivos adjuntos. Peso y altura quedan como números.
    /// </summary>
    public class HistoriaClinicaDatos
    {
        private static Cifrador C => Cifrador.Actual;

        public HistoriaClinicaVista Obtener(int idPaciente)
        {
            var hc = Uno(@"SELECT id_paciente, grupo_sanguineo, peso, altura_cm, alergias, enfermedades_cronicas, patologias
                           FROM HISTORIA_CLINICA WHERE id_paciente = @p",
                r => new HistoriaClinicaVista
                {
                    IdPaciente = Ent(r, "id_paciente"),
                    GrupoSanguineo = C.Descifrar(Txt(r, "grupo_sanguineo")),
                    Peso = DecN(r, "peso"),
                    AlturaCm = EntN(r, "altura_cm"),
                    Alergias = C.Descifrar(Txt(r, "alergias")),
                    EnfermedadesCronicas = C.Descifrar(Txt(r, "enfermedades_cronicas")),
                    Patologias = C.Descifrar(Txt(r, "patologias"))
                }, ("@p", idPaciente)) ?? new HistoriaClinicaVista { IdPaciente = idPaciente };

            hc.Consultas = Lista(@"
                SELECT c.id_consulta, c.id_turno, c.fecha, c.id_medico, pm.apellido, pm.nombre, e.descripcion AS esp,
                       c.observaciones, c.diagnostico, c.tratamientos, c.proximos_pasos
                FROM CONSULTA c
                JOIN PERSONA pm ON pm.id_persona = c.id_medico
                JOIN MEDICO m ON m.id_medico = c.id_medico
                LEFT JOIN ESPECIALIDAD e ON e.id_especialidad = m.id_especialidad
                WHERE c.id_paciente = @p ORDER BY c.fecha DESC",
                r => new ConsultaVista
                {
                    IdConsulta = Ent(r, "id_consulta"),
                    IdTurno = EntN(r, "id_turno"),
                    Fecha = Fecha(r, "fecha"),
                    IdMedico = Ent(r, "id_medico"),
                    Medico = Txt(r, "apellido") + ", " + Txt(r, "nombre"),
                    Especialidad = Txt(r, "esp"),
                    Observaciones = C.Descifrar(Txt(r, "observaciones")),
                    Diagnostico = C.Descifrar(Txt(r, "diagnostico")),
                    Tratamientos = C.Descifrar(Txt(r, "tratamientos")),
                    ProximosPasos = C.Descifrar(Txt(r, "proximos_pasos"))
                }, ("@p", idPaciente));

            if (hc.Consultas.Count > 0)
            {
                var archivos = Lista(@"SELECT a.id_archivo, a.id_consulta, a.nombre, a.tipo_contenido FROM CONSULTA_ARCHIVO a
                                       JOIN CONSULTA c ON c.id_consulta = a.id_consulta WHERE c.id_paciente = @p ORDER BY a.id_archivo",
                    r => (Ent(r, "id_consulta"), new ArchivoVista { IdArchivo = Ent(r, "id_archivo"), Nombre = Txt(r, "nombre"), TipoContenido = Txt(r, "tipo_contenido") }),
                    ("@p", idPaciente));
                foreach (var c in hc.Consultas)
                    c.Archivos = archivos.Where(a => a.Item1 == c.IdConsulta).Select(a => a.Item2).ToList();
            }
            return hc;
        }

        public void GuardarDatosClinicos(HistoriaClinicaVista h)
        {
            Ejecutar(@"
                IF EXISTS (SELECT 1 FROM HISTORIA_CLINICA WHERE id_paciente = @p)
                    UPDATE HISTORIA_CLINICA SET grupo_sanguineo = @g, peso = @peso, altura_cm = @alt, alergias = @al,
                           enfermedades_cronicas = @cr, patologias = @pa, fecha_actualizacion = dbo.Ahora() WHERE id_paciente = @p
                ELSE
                    INSERT INTO HISTORIA_CLINICA (id_paciente, grupo_sanguineo, peso, altura_cm, alergias, enfermedades_cronicas, patologias)
                    VALUES (@p, @g, @peso, @alt, @al, @cr, @pa);",
                ("@p", h.IdPaciente), ("@g", Cifrado(h.GrupoSanguineo)), ("@peso", h.Peso), ("@alt", h.AlturaCm),
                ("@al", Cifrado(h.Alergias)), ("@cr", Cifrado(h.EnfermedadesCronicas)), ("@pa", Cifrado(h.Patologias)));
        }

        private static string? NullSiVacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        private static string? Cifrado(string? s) => C.Cifrar(NullSiVacio(s));

        public int AgregarConsulta(int idPaciente, int idMedico, int? idTurno, ConsultaVista c, List<(string Nombre, string Tipo, byte[] Contenido)> archivos)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(@"SET XACT_ABORT ON; BEGIN TRAN;
                INSERT INTO CONSULTA (id_turno, id_paciente, id_medico, observaciones, diagnostico, tratamientos, proximos_pasos)
                VALUES (@turno, @p, @m, @obs, @diag, @trat, @prox);
                DECLARE @id INT = SCOPE_IDENTITY();
                IF @turno IS NOT NULL
                BEGIN
                    DECLARE @ant VARCHAR(30) = (SELECT estado FROM TURNO WHERE id_turno = @turno);
                    UPDATE TURNO SET estado = 'Atendido' WHERE id_turno = @turno AND estado IN ('Presente', 'EnAtencion', 'Confirmado');
                    IF @@ROWCOUNT > 0
                        INSERT INTO HISTORIAL_TURNO (id_turno, id_paciente, estado_anterior, estado_nuevo, motivo) VALUES (@turno, @p, @ant, 'Atendido', 'Consulta registrada');
                END
");
            for (int i = 0; i < archivos.Count; i++)
                sb.Append($"INSERT INTO CONSULTA_ARCHIVO (id_consulta, nombre, tipo_contenido, contenido) VALUES (@id, @an{i}, @at{i}, @ac{i});\n");
            sb.Append("COMMIT;\nSELECT @id;");

            var datos = new ConexionDB();
            try
            {
                datos.setearConsulta(sb.ToString());
                datos.setearParametro("@turno", idTurno);
                datos.setearParametro("@p", idPaciente);
                datos.setearParametro("@m", idMedico);
                datos.setearParametro("@obs", C.Cifrar(c.Observaciones));
                datos.setearParametro("@diag", C.Cifrar(c.Diagnostico));
                datos.setearParametro("@trat", Cifrado(c.Tratamientos));
                datos.setearParametro("@prox", Cifrado(c.ProximosPasos));
                for (int i = 0; i < archivos.Count; i++)
                {
                    datos.setearParametro($"@an{i}", archivos[i].Nombre);
                    datos.setearParametro($"@at{i}", archivos[i].Tipo);
                    datos.setearParametro($"@ac{i}", C.CifrarBytes(archivos[i].Contenido));
                }
                return datos.obtenerId();
            }
            finally { datos.cerrarConexion(); }
        }

        /// <summary>Devuelve el archivo (ya descifrado) y el paciente al que pertenece (para controlar quién puede descargarlo).</summary>
        public (string Nombre, string Tipo, byte[] Contenido, int IdPaciente)? ObtenerArchivo(int idArchivo)
        {
            var l = Lista(@"SELECT a.nombre, a.tipo_contenido, a.contenido, c.id_paciente FROM CONSULTA_ARCHIVO a
                            JOIN CONSULTA c ON c.id_consulta = a.id_consulta WHERE a.id_archivo = @id",
                r => (Txt(r, "nombre"), Txt(r, "tipo_contenido"), C.DescifrarBytes((byte[])r["contenido"]), Ent(r, "id_paciente")), ("@id", idArchivo));
            return l.Count == 0 ? null : l[0];
        }

        /// <summary>
        /// Cifra los datos clínicos que hayan quedado guardados sin cifrar (bases anteriores a este cambio). Se puede correr las veces que haga falta:
        /// lo que ya está cifrado no se toca. Devuelve cuántas filas cifró.
        /// </summary>
        public int CifrarExistentes()
        {
            int n = 0;
            const string sinCifrar = " IS NOT NULL AND {0} NOT LIKE 'enc:v1:%'";
            string Sin(string col) => "(" + col + string.Format(sinCifrar, col) + ")";

            var historias = Lista($@"SELECT id_paciente, grupo_sanguineo, alergias, enfermedades_cronicas, patologias FROM HISTORIA_CLINICA
                                     WHERE {Sin("grupo_sanguineo")} OR {Sin("alergias")} OR {Sin("enfermedades_cronicas")} OR {Sin("patologias")}",
                r => (Ent(r, "id_paciente"), NullSi(r, "grupo_sanguineo"), NullSi(r, "alergias"), NullSi(r, "enfermedades_cronicas"), NullSi(r, "patologias")));
            foreach (var h in historias)
                n += Numero(@"UPDATE HISTORIA_CLINICA SET grupo_sanguineo = @g, alergias = @al, enfermedades_cronicas = @cr, patologias = @pa
                              WHERE id_paciente = @p; SELECT @@ROWCOUNT;",
                    ("@p", h.Item1), ("@g", C.Cifrar(h.Item2)), ("@al", C.Cifrar(h.Item3)), ("@cr", C.Cifrar(h.Item4)), ("@pa", C.Cifrar(h.Item5)));

            var consultas = Lista($@"SELECT id_consulta, observaciones, diagnostico, tratamientos, proximos_pasos FROM CONSULTA
                                     WHERE {Sin("observaciones")} OR {Sin("diagnostico")} OR {Sin("tratamientos")} OR {Sin("proximos_pasos")}",
                r => (Ent(r, "id_consulta"), NullSi(r, "observaciones"), NullSi(r, "diagnostico"), NullSi(r, "tratamientos"), NullSi(r, "proximos_pasos")));
            foreach (var c in consultas)
                n += Numero(@"UPDATE CONSULTA SET observaciones = @o, diagnostico = @d, tratamientos = @t, proximos_pasos = @x
                              WHERE id_consulta = @id; SELECT @@ROWCOUNT;",
                    ("@id", c.Item1), ("@o", C.Cifrar(c.Item2)), ("@d", C.Cifrar(c.Item3)), ("@t", C.Cifrar(c.Item4)), ("@x", C.Cifrar(c.Item5)));

            // Los archivos se procesan de a uno (pueden pesar hasta 5 MB). 0x5454454E4331 = "TTENC1", la marca de cifrado.
            var archivos = Lista("SELECT id_archivo FROM CONSULTA_ARCHIVO WHERE DATALENGTH(contenido) < 6 OR SUBSTRING(contenido, 1, 6) <> 0x5454454E4331",
                r => Ent(r, "id_archivo"));
            foreach (int id in archivos)
            {
                var contenido = Lista("SELECT contenido FROM CONSULTA_ARCHIVO WHERE id_archivo = @id", r => (byte[])r["contenido"], ("@id", id)).Single();
                n += Numero("UPDATE CONSULTA_ARCHIVO SET contenido = @c WHERE id_archivo = @id; SELECT @@ROWCOUNT;", ("@id", id), ("@c", C.CifrarBytes(contenido)));
            }
            return n;
        }

        private static string? NullSi(Microsoft.Data.SqlClient.SqlDataReader r, string col) => r[col] is DBNull ? null : Txt(r, col);
    }
}
