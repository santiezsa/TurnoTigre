# Pantallas y funcionalidades (Razor Pages)

Todas las pantallas del prototipo (`Web/wwwroot/prototipo`) son páginas reales conectadas a SQL Server, con el mismo diseño (modo claro/oscuro, Poppins, logo Tigre) y navegación por rol. El login redirige según el rol. **Antes de usar el sistema hay que correr el script de base de datos** (ver `README.md`).

| Rol | Páginas |
| :--- | :--- |
| **Paciente** | `/Pacientes/Index` (solicitar turno para uno mismo o un familiar, o anotarse en lista de espera), `MisTurnos` (confirmar, cancelar, lista de espera, avisos), `Familiares` (máx. 6), `MisDatos` |
| **Administrativo** | `/Administrativo/TurnosDelDia` (presente/ausente/cancelar), `GenerarAgenda` (alta y edición), `Agendas`, `Profesionales` (alta y edición; la baja es del Super Admin), `Pacientes`, `PacienteForm`, `PacienteDetalle`, `AgendarTurno` (con filtros opcionales de fechas y horario) |
| **Médico** | `/Medico/MiAgenda` (programados, sala de espera, atendiéndose, atendidos), `/Medico/HistoriaClinica` (datos clínicos, consulta y hasta 5 adjuntos de 5 MB), buscar paciente (`/Administrativo/Pacientes`) |
| **Super Admin** | `/Admin/Usuarios`, `Nuevo`, `Editar`, `Auditoria`, `/Administrativo/Catalogos` (especialidades y centros: solo él), baja de profesionales + menú **Paneles** para entrar a los otros tres paneles (en el panel médico elige el profesional) |
| Todos | `/CambiarPassword`, `/MisDatos` (personal), `/Salir`, `/DescargarArchivo` (adjuntos de historia clínica, con control de acceso) |

## Reglas de negocio implementadas
- **Turnos**: la agenda genera un registro `TURNO` "Disponible" por cada cupo de cada franja. Reservar toma un cupo de forma atómica (dos personas no pueden quedarse con el mismo horario).
- Una persona **no puede tener dos turnos activos de la misma especialidad**.
- Un turno reservado hay que **confirmarlo hasta 24 h antes**; si se reserva con menos de 24 h de anticipación queda confirmado automáticamente. Los no confirmados pasan a **Vencido** y el horario se libera (se aplica al abrir las pantallas).
- Al **cancelar** o vencer un turno el horario vuelve a quedar disponible el cupo se **reasigna automáticamente** al primer inscripto compatible de la lista de espera (centro, especialidad y franja; el de inscripción más antigua). Queda como "Reservado" con plazo para confirmar de 24 h (o hasta 1 h antes de la cita si falta menos) y se le avisa en "Mis turnos" y por mail; si no confirma, el turno vence y pasa al siguiente. Si nadie es compatible, el horario queda disponible. Las inscripciones en lista de espera vencen a los 30 días y se cierran al sacar turno en esa especialidad.
- **Avisos al paciente** (por mail siempre; también en "Mis turnos" cuando no fue el propio paciente quien hizo la acción): turno reservado (con profesional, centro, especialidad, fecha, hora y cuándo confirmarlo), confirmado, cancelado (si lo canceló el centro de salud, lo dice), vencido por no confirmar a tiempo, reasignado desde la lista de espera, recordatorio cuando se abre la ventana de confirmación y recordatorio de la cita (una vez, cuando falta un día o menos para un turno confirmado; siempre en la app y por mail). Todos incluyen el número de turno, que también se ve en "Mis turnos". Si el turno es de un familiar sin cuenta, le llega al titular. Los mails no salen después de la hora del turno.
- **Agendas**: solo se pueden editar o eliminar si no tienen turnos asignados. No se permite que un profesional tenga dos agendas superpuestas.
- **Profesionales**: el personal administrativo los da de alta y los edita; solo se editan si no tienen agendas. Eliminarlos es solo del Super Admin (si tienen usuario o consultas se dan de baja en lugar de borrarlos). El alta no crea usuario: si va a usar el sistema, el Super Admin le crea la cuenta desde Usuarios → Nuevo con el mismo DNI.
- **Reserva de turnos**: además de centro y especialidad hay filtros opcionales (desde, hasta y horario mañana/tarde). Si el paciente ya tiene un turno activo de esa especialidad, se le avisa y puede reservar otro de todas formas; nunca dos turnos a la misma hora.
- **Pacientes**: domicilio dentro de las localidades de Tigre; nombre y apellido normalizados; el DNI no se edita. Si el personal ya había cargado la ficha, al registrarse el vecino se **vincula** si coinciden DNI y fecha de nacimiento.
- **Familiares**: hasta 6; no se puede quitar uno con turnos próximos.
- **Historia clínica**: solo la ven médicos y Super Admin; los adjuntos se bajan con control de acceso. Registrar una consulta pasa el turno a **Atendido**.
- El sistema nunca queda sin un Super Admin activo (ver `SUPERADMIN.md`).

## Estados de un turno
`Disponible` → `PendienteConfirmacion` (se muestra "Reservado") → `Confirmado` → `Presente` / `Ausente` → `EnAtencion` → `Atendido`. También `Cancelado` y `Vencido`.

## Capas
`Dominio` (clases y modelos) · `AccesoDatos` (SQL con parámetros; `Sql.cs` tiene los atajos) · `Negocio` (reglas) · `Web` (páginas). Las páginas no tienen SQL.

## Ventanas modales (estilo del prototipo)

- **Confirmación** antes de eliminar, cancelar, quitar, deshabilitar o reservar (ícono de advertencia, botones "Cancelar" y "Sí, …").
- **Resultado** después de cada acción: aviso verde "¡Listo!" o rojo "No se pudo completar" con el motivo.
- **Recuperar contraseña**: ventana con el candado, campo de correo (paciente) o usuario (personal) y pantalla "Revisá tu correo".
- En el código: `data-confirm="..."` en un `<form>` o `<button>` pide confirmación; `ttAviso()` y `ttConfirmar()` (site.js) abren los avisos.

## Ingreso

- `/Ingresar` (pacientes, por correo) y `/IngresarPersonal` (personal, por usuario) con letra grande, botón deshabilitado hasta completar los datos y enlaces claros ("Crear mi cuenta", "Soy personal del CAPS").
- `/RestablecerPassword?token=...`: pantalla a la que llega el enlace del correo.
