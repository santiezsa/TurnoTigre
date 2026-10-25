using static AccesoDatos.Sql;

namespace AccesoDatos
{
    /// <summary>Quién puede ver la historia clínica de un paciente y registro de los accesos (AUDITORIA_ACCESO).</summary>
    public class AccesoHistoriaDatos
    {
        /// <summary>
        /// Un médico tiene relación de atención con un paciente si tiene un turno suyo con él que vaya a ocurrir o haya ocurrido
        /// (confirmado, presente, en atención o atendido) o si ya le registró una consulta.
        /// Los turnos sin confirmar, cancelados, vencidos o ausentes no dan acceso.
        /// </summary>
        public bool MedicoAtiende(int idMedico, int idPaciente) =>
            Existe(@"SELECT (SELECT COUNT(*) FROM TURNO t JOIN AGENDA a ON a.id_agenda = t.id_agenda
                             WHERE a.id_medico = @m AND t.id_paciente = @p AND t.estado IN ('Confirmado', 'Presente', 'EnAtencion', 'Atendido'))
                          + (SELECT COUNT(*) FROM CONSULTA WHERE id_medico = @m AND id_paciente = @p)",
                ("@m", idMedico), ("@p", idPaciente));

        public void Registrar(int idUsuario, string rol, int idPaciente, string accion, string? detalle) =>
            Ejecutar("INSERT INTO AUDITORIA_ACCESO (id_usuario, rol, id_paciente, accion, detalle) VALUES (@u, @r, @p, @a, @d)",
                ("@u", idUsuario), ("@r", rol), ("@p", idPaciente), ("@a", accion), ("@d", detalle));
    }
}
