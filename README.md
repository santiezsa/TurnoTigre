# 🏥 TurnoTigre - Sistema de Gestión de Turnos Municipales

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-5C2D91?style=for-the-badge&logo=.net&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQLServer-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Figma](https://img.shields.io/badge/Figma-F24E1E?style=for-the-badge&logo=figma&logoColor=white)

> **TurnoTigre** es una solución digital desarrollada por **PentaCode** orientada a facilitar el acceso a turnos médicos en los centros de salud del Municipio de Tigre. La plataforma busca reducir las esperas presenciales de madrugada y optimizar los cupos médicos mediante un motor de reasignación automática.

---

## 🚀 Características Principales

- 👤 **Registro y Autenticación:** Alta de pacientes con validación de domicilio en el municipio.
- 📅 **Gestión de Agendas Médicas:** Creación de profesionales y apertura de horarios de atención.
- ♿ **Reserva Accesible:** Interfaz de alto contraste y compatibilidad con lectores de pantalla, optimizada para adultos mayores.
- ✅ **Confirmación Dinámica:** Sistema de validación de asistencia 24 horas previas a la cita.
- ⚙️ **Motor de Reasignación (Automático):** Si un turno es cancelado o no confirmado, el sistema lo reasigna automáticamente al siguiente paciente en la lista de espera inteligente.

## 🛠️ Stack Tecnológico

- **Backend / Lógica de Negocio:** C# .NET
- **Base de Datos:** Microsoft SQL Server
- **Arquitectura:** Monolito / MVC (a definir)
- **Diseño UI/UX:** Figma

## 👥 Equipo de Desarrollo | PentaCode

| Desarrollador | GitHub |
| :--- | :--- |
| **Gabriel Alejandro Bottillo** | [@GabrielBottillo123](https://github.com/GabrielBottillo123) |
| **Erika Salomé Espeche** | [@erikaespeche](https://github.com/erikaespeche) |
| **Julián Facundo Parodi** | [@Julianfparodi](https://github.com/Julianfparodi) |
| **Maia Nair Pérez Rodríguez** | [@MaiaPerez](https://github.com/MaiaPerez) |
| **Santiago Ezequiel Sánchez** | [@santiezsa](https://github.com/santiezsa) |

---

## 🖥️ Prototipo del Frontend

El diseño de todas las pantallas (paciente, administrativo, médico y super administrador) está en un prototipo navegable: [`Web/wwwroot/prototipo/index.html`](Web/wwwroot/prototipo/index.html). Se puede abrir con doble clic o, con el proyecto `Web` corriendo, en `/prototipo/index.html`. Las cuentas de prueba y las reglas de negocio que muestra están en [`LEEME.md`](Web/wwwroot/prototipo/LEEME.md).

---

## ⚙️ Instalación y Uso (Desarrollo)

1. **Base de datos** (SQL Server, base `TurnoTigre`):
   - Base nueva: ejecutar `DB/script_creacion.sql` (ya incluye todo lo necesario, los datos iniciales y el Super Admin).
   - Base que ya tenías creada: ejecutar `DB/migracion_funcionalidades.sql` (se puede correr más de una vez; no borra datos).
   - Opcional, para probar con datos de ejemplo: `DB/datos_prueba.sql` (usuarios `erika.espeche`, `laura.gomez` y `ana.perez@mail.com`, contraseña `Tigre2026`).
2. **Cadena de conexión**: en `Web/appsettings.Development.json` (clave `ConnectionStrings:TurnoTigreDB`). Ejemplo con Windows Authentication: `Server=NOMBRE-PC\\SQLEXPRESS;Database=TurnoTigre;Trusted_Connection=True;TrustServerCertificate=True`.
   - **Importante:** si ya tenías la base creada, volvé a ejecutar `DB/migracion_funcionalidades.sql` (es repetible): agrega la foto de perfil y la tabla de recuperación de contraseña.
3. Ejecutar el proyecto `Web` (F5 en Visual Studio). El personal ingresa desde "Ingresá con tu usuario" (`/IngresarPersonal`): `super.admin` / `Tigre2026` (pide cambiar la clave), o los usuarios de prueba `erika.espeche` y `laura.gomez`. Los pacientes ingresan con su correo (`ana.perez@mail.com`).

Más detalle: [`PANTALLAS.md`](PANTALLAS.md) (todas las pantallas y reglas) y [`SUPERADMIN.md`](SUPERADMIN.md).

---
<div align="center">
  <i>Proyecto final desarrollado para la Tecnicatura Universitaria en Programación (UTN FRGP) - 2026.</i>
</div>


## Recuperar contraseña y correo

- "¿Olvidaste tu contraseña?" abre una ventana donde el paciente escribe su correo (el personal, su usuario). El sistema **siempre responde lo mismo**, exista o no la cuenta, y manda un enlace **solo al correo que figura en la cuenta**. El enlace sirve una vez, vence en 1 hora y se guarda solo su hash.
- Para que los correos salgan hay que completar la sección `Correo` de `appsettings.json` (Host, Usuario, Clave, Remitente). Con `Correo:UrlBase` (por ejemplo `https://turnos.tigre.gob.ar`) se fija la dirección de los enlaces.
- Sin SMTP configurado (desarrollo) el correo no se envía: el enlace queda escrito en la consola donde corre `dotnet run`, en el aviso "SMTP no configurado".
- `Demo:MostrarCuentasDePrueba` (appsettings) muestra u oculta los datos de prueba en las pantallas de ingreso. Ponerlo en `false` al publicar.

## Foto de perfil

El paciente puede subir una foto de su rostro desde **Mis datos** (JPG o PNG, se recorta y achica sola; máximo 2 MB). La ven el propio paciente y el personal (en el detalle del paciente).
