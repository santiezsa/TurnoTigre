# Pantallas y funcionalidades (Razor Pages)

Todas las pantallas del prototipo (`Web/wwwroot/prototipo`) son páginas reales conectadas a SQL Server, con el mismo diseño (modo claro/oscuro, Poppins, logo Tigre) y navegación por rol. El login redirige según el rol. **Antes de usar el sistema hay que correr el script de base de datos** (ver `README.md`).

| Rol | Páginas |
| :--- | :--- |
| **Paciente** | `/Pacientes/Index` (solicitar turno para uno mismo o un familiar, o anotarse en lista de espera), `MisTurnos` (confirmar, cancelar, lista de espera, avisos), `Familiares` (máx. 6), `MisDatos` |
| **Administrativo** | `/Administrativo/TurnosDelDia` (presente/ausente/cancelar), `GenerarAgenda` (alta y edición), `Agendas`, `Profesionales` (alta/edición/baja), `Pacientes`, `PacienteForm`, `PacienteDetalle`, `AgendarTurno`, `Catalogos` (especialidades y centros) |
| **Médico** | `/Medico/MiAgenda` (programados, sala de espera, atendiéndose, atendidos), `/Medico/HistoriaClinica` (datos clínicos, consulta y hasta 5 adjuntos de 5 MB), buscar paciente (`/Administrativo/Pacientes`) |
| **Super Admin** | `/Admin/Usuarios`, `Nuevo`, `Editar` + menú **Paneles** para entrar a los otros tres paneles (en el panel médico elige el profesional) |
| Todos | `/CambiarPassword`, `/MisDatos` (personal), `/Salir`, `/DescargarArchivo` (adjuntos de historia clínica, con control de acceso) |

## Reglas de negocio implementadas
- **Turnos**: la agenda genera un registro `TURNO` "Disponible" por cada cupo de cada franja. Reservar toma un cupo de forma atómica (dos personas no pueden quedarse con el mismo horario).
- Una persona **no puede tener dos turnos activos de la misma especialidad**.
- Un turno reservado hay que **confirmarlo hasta 24 h antes**; si se reserva con menos de 24 h de anticipación queda confirmado automáticamente. Los no confirmados pasan a **Vencido** y el horario se libera (se aplica al abrir las pantallas).
- Al **cancelar** o vencer un turno el horario vuelve a quedar disponible y se **avisa a la lista de espera** (aviso dentro del sistema, en "Mis turnos"). Las inscripciones en lista de espera vencen a los 30 días y se cierran al sacar turno en esa especialidad.
- **Agendas**: solo se pueden editar o eliminar si no tienen turnos asignados. No se permite que un profesional tenga dos agendas superpuestas.
- **Profesionales**: solo se editan o eliminan si no tienen agendas (ni usuario, ni consultas).
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
