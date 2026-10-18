using AccesoDatos;
using Dominio;
using System.Text.RegularExpressions;

namespace Negocio
{
    public class AgendaNegocio
    {
        private readonly AgendaDatos datos = new AgendaDatos();
        private readonly ProfesionalDatos profesionales = new ProfesionalDatos();
        private const int MaxTurnos = 15000;

        public List<AgendaVista> Listar(string? texto = null, int? idMedico = null) => datos.Listar(texto, idMedico);
        public AgendaVista? Obtener(int id) => datos.Obtener(id);
        public List<FechaAgenda> Fechas(int id) => datos.Fechas(id);

        public int Crear(AgendaNueva a) => Guardar(a, null);

        public int Editar(int idAgenda, AgendaNueva a)
        {
            var actual = datos.Obtener(idAgenda) ?? throw new Exception("La agenda no existe.");
            if (!actual.Editable) throw new Exception("La agenda ya tiene turnos asignados a pacientes: no se puede modificar.");
            return Guardar(a, idAgenda);
        }

        public void Eliminar(int idAgenda)
        {
            var actual = datos.Obtener(idAgenda) ?? throw new Exception("La agenda no existe.");
            if (!actual.Editable) throw new Exception("La agenda tiene turnos asignados a pacientes: no se puede eliminar.");
            datos.Eliminar(idAgenda);
        }

        private int Guardar(AgendaNueva a, int? reemplazar)
        {
            var prof = profesionales.Obtener(a.IdMedico) ?? throw new Exception("Elegí un profesional.");
            if (!prof.Especialidades.Any(e => e.IdEspecialidad == a.IdEspecialidad))
                throw new Exception("Elegí una especialidad del profesional.");
            if (a.IdCentro <= 0) throw new Exception("Elegí el centro de salud.");
            if (a.DuracionMinutos < 5 || a.DuracionMinutos > 120) throw new Exception("La duración del turno debe estar entre 5 y 120 minutos.");
            if (a.PacientesPorFranja < 1 || a.PacientesPorFranja > 10) throw new Exception("Los pacientes por franja deben ser entre 1 y 10.");
            if (a.Tipo != "Mensual" && a.Tipo != "PorFecha") throw new Exception("Tipo de agenda inválido.");

            var franjas = new List<DateTime>();
            if (a.Tipo == "Mensual")
            {
                ValidarHoras(a.HoraInicio, a.HoraFin);
                if (a.Dias.Count == 0) throw new Exception("Elegí al menos un día de la semana.");
                if (a.Hasta.Date < a.Desde.Date) throw new Exception("La fecha 'hasta' no puede ser anterior a 'desde'.");
                if ((a.Hasta.Date - a.Desde.Date).TotalDays > 366) throw new Exception("La agenda no puede durar más de un año.");
                for (var d = a.Desde.Date; d <= a.Hasta.Date; d = d.AddDays(1))
                    if (a.Dias.Contains((int)d.DayOfWeek)) franjas.AddRange(Generar(d, a.HoraInicio, a.HoraFin, a.DuracionMinutos));
            }
            else
            {
                if (a.Fechas.Count == 0) throw new Exception("Cargá al menos una fecha.");
                if (a.Fechas.Select(f => f.Fecha.Date).Distinct().Count() != a.Fechas.Count) throw new Exception("Hay fechas repetidas.");
                foreach (var f in a.Fechas)
                {
                    ValidarHoras(f.HoraInicio, f.HoraFin);
                    franjas.AddRange(Generar(f.Fecha.Date, f.HoraInicio, f.HoraFin, a.DuracionMinutos));
                }
                a.Desde = a.Fechas.Min(f => f.Fecha.Date);
                a.Hasta = a.Fechas.Max(f => f.Fecha.Date);
                a.HoraInicio = a.Fechas[0].HoraInicio;
                a.HoraFin = a.Fechas[0].HoraFin;
            }

            if (franjas.Count == 0) throw new Exception("Con esos datos no se genera ningún turno futuro. Revisá las fechas y los horarios.");
            if (franjas.Count * a.PacientesPorFranja > MaxTurnos) throw new Exception("La agenda genera demasiados turnos. Acortá el período.");

            var ocupados = datos.HorariosOcupados(a.IdMedico, franjas.Min(), franjas.Max(), reemplazar);
            var choque = franjas.FirstOrDefault(f => ocupados.Contains(f));
            if (choque != default)
                throw new Exception($"El profesional ya tiene turnos en otra agenda el {choque:dd/MM/yyyy} a las {choque:HH:mm}.");

            return datos.Crear(a, franjas.Distinct().OrderBy(f => f).ToList(), reemplazar);
        }

        private static void ValidarHoras(string ini, string fin)
        {
            if (!Regex.IsMatch(ini ?? "", @"^([01]\d|2[0-3]):[0-5]\d$") || !Regex.IsMatch(fin ?? "", @"^([01]\d|2[0-3]):[0-5]\d$"))
                throw new Exception("Los horarios deben tener formato HH:mm.");
            if (TimeSpan.Parse(fin!) <= TimeSpan.Parse(ini!)) throw new Exception("La hora de fin debe ser posterior a la de inicio.");
        }

        private static IEnumerable<DateTime> Generar(DateTime dia, string ini, string fin, int minutos)
        {
            var desde = dia + TimeSpan.Parse(ini);
            var hasta = dia + TimeSpan.Parse(fin);
            for (var t = desde; t.AddMinutes(minutos) <= hasta; t = t.AddMinutes(minutos))
                if (t > DateTime.Now) yield return t;
        }
    }
}
