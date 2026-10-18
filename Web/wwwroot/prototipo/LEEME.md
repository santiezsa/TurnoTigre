# Prototipo navegable de TurnoTigre (frontend)

Prototipo clickeable en un único archivo (`index.html`, sin dependencias ni build) con el diseño visual del sistema: paleta Tigre (rojo `#D20A11`, celeste `#1FB3FF`, plata), tipografía Poppins, modo claro/oscuro y diseño responsive.

Los datos son **simulados en memoria** (se reinician al recargar la página). No se conecta a la base de datos ni al backend.

## Cómo verlo

- Abriendo `index.html` directamente en el navegador (doble clic), o
- Ejecutando el proyecto `Web` y entrando a `/prototipo/index.html`.

## Paneles y cuentas de prueba (contraseña de todas: `Tigre2026`)

| Panel | Cómo ingresar |
| :--- | :--- |
| **Paciente** (pantalla de inicio) | correo `ana.perez@mail.com` (tiene familiares a cargo). También se puede crear una cuenta con **Registrarme**. |
| **Administrativo** | *Soy personal del CAPS* → usuario `erika.espeche` |
| **Médico** | *Soy personal del CAPS* → usuario `laura.gomez` |
| **Super administrador** | *Soy personal del CAPS* → usuario `super.admin` (menú **PANELES** para entrar a cada panel y **USUARIOS** para gestionar cuentas) |

Para probar un registro que se vincula a una ficha existente: DNI `38445566`, fecha de nacimiento `30/07/1995`.

## Qué incluye

- **Paciente:** ingreso (grande y simple, pensado para adultos mayores), registro con validación de domicilio en Tigre, solicitar turno (calendario de días con atención + horarios), mis turnos (confirmar asistencia hasta 24 h antes, cancelar, lista de espera, historial), familiares a cargo (alta, edición, baja) y mis datos.
- **Administrativo:** turnos del día (calendario, estados Presente/Ausente/Cancelado), generar y listar agendas, profesionales, pacientes (listado, alta, edición, detalle) y perfil.
- **Médico:** mi agenda (calendario con días de atención, sala de espera / atendiéndose / atendido), carga de historia clínica, buscar paciente (solo lectura, con edición de datos clínicos).
- **Super administrador:** acceso a todos los paneles y gestión de usuarios (alta de Administrativo / Médico / Super administrador, usuario `nombre.apellido` automático, contraseña provisoria aleatoria enviada por correo con cambio obligatorio en el primer ingreso, edición y activación/desactivación).

## Reglas de negocio que ya se ven en el prototipo (a tener en cuenta en el backend)

- Un solo turno activo por especialidad y por persona.
- El turno reservado debe confirmarse hasta 24 h antes; si se reserva con menos de 24 h de anticipación queda confirmado automáticamente.
- Un familiar a cargo no se puede quitar si tiene turnos próximos.
- Apellido y nombre se guardan en campos separados y se normalizan (mayúscula inicial).
- DNI no editable una vez cargado; domicilio dentro del partido de Tigre.
- Usuarios del personal: nunca puede quedar el sistema sin un super administrador activo.
