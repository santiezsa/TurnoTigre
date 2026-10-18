using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class HistoriaClinicaDatos
    {
        public HistoriaClinicaVista Obtener(int idPaciente)
        {
            var hc = Uno(@"SELECT id_paciente, grupo_sanguineo, peso, altura_cm, alergias, enfermedades_cronicas, patologias
                           FROM HISTORIA_CLINICA WHERE id_paciente = @p",
                r => new HistoriaClinicaVista
                {
                    IdPaciente = Ent(r, "id_paciente"),
                    GrupoSanguineo = Txt(r, "grupo_sanguineo"),
                    Peso = DecN(r, "peso"),
                    AlturaCm = EntN(r, "altura_cm"),
                    Alergias = Txt(r, "alergias"),
                    EnfermedadesCronicas = Txt(r, "enfermedades_cronicas"),
                    Patologias = Txt(r, "patologias")
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
                    Observaciones = Txt(r, "observaciones"),
                    Diagnostico = Txt(r, "diagnostico"),
                    Tratamientos = Txt(r, "tratamientos"),
                    ProximosPasos = Txt(r, "proximos_pasos")
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
                           enfermedades_cronicas = @cr, patologias = @pa, fecha_actualizacion = GETDATE() WHERE id_paciente = @p
                ELSE
                    INSERT INTO HISTORIA_CLINICA (id_paciente, grupo_sanguineo, peso, altura_cm, alergias, enfermedades_cronicas, patologias)
                    VALUES (@p, @g, @peso, @alt, @al, @cr, @pa);",
                ("@p", h.IdPaciente), ("@g", NullSiVacio(h.GrupoSanguineo)), ("@peso", h.Peso), ("@alt", h.AlturaCm),
                ("@al", NullSiVacio(h.Alergias)), ("@cr", NullSiVacio(h.EnfermedadesCronicas)), ("@pa", NullSiVacio(h.Patologias)));
        }

        private static string? NullSiVacio(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

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
                datos.setearParametro("@obs", c.Observaciones);
                datos.setearParametro("@diag", c.Diagnostico);
                datos.setearParametro("@trat", NullSiVacio(c.Tratamientos));
                datos.setearParametro("@prox", NullSiVacio(c.ProximosPasos));
                for (int i = 0; i < archivos.Count; i++)
                {
                    datos.setearParametro($"@an{i}", archivos[i].Nombre);
                    datos.setearParametro($"@at{i}", archivos[i].Tipo);
                    datos.setearParametro($"@ac{i}", archivos[i].Contenido);
                }
                return datos.obtenerId();
            }
            finally { datos.cerrarConexion(); }
        }

        /// <summary>Devuelve el archivo y el paciente al que pertenece (para controlar quién puede descargarlo).</summary>
        public (string Nombre, string Tipo, byte[] Contenido, int IdPaciente)? ObtenerArchivo(int idArchivo)
        {
            var l = Lista(@"SELECT a.nombre, a.tipo_contenido, a.contenido, c.id_paciente FROM CONSULTA_ARCHIVO a
                            JOIN CONSULTA c ON c.id_consulta = a.id_consulta WHERE a.id_archivo = @id",
                r => (Txt(r, "nombre"), Txt(r, "tipo_contenido"), (byte[])r["contenido"], Ent(r, "id_paciente")), ("@id", idArchivo));
            return l.Count == 0 ? null : l[0];
        }
    }
}
