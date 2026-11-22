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

- **Backend / Lógica de Negocio:** C# .NET 8 (ASP.NET Core Razor Pages)
- **Base de Datos:** Microsoft SQL Server
- **Arquitectura:** monolito en 4 capas: `Dominio` → `AccesoDatos` → `Negocio` → `Web`
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

Guía paso a paso para tener TurnoTigre andando en tu computadora. Los comandos están pensados para **Windows con PowerShell**; en otros sistemas cambian solo las rutas.

### 0. Qué necesitás instalar

| Programa | Para qué | Cómo conseguirlo |
| :--- | :--- | :--- |
| **Git** | Bajar el proyecto | [git-scm.com](https://git-scm.com) |
| **.NET 8 SDK** | Compilar y correr la aplicación | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) (el SDK, no solo el runtime) |
| **SQL Server** | La base de datos | **Con Docker (recomendado):** [Docker Desktop](https://www.docker.com/products/docker-desktop/). **Sin Docker:** SQL Server Express o Developer |
| **sqlcmd** | Ejecutar los scripts de la base | Viene con SQL Server o se instala aparte ("sqlcmd utility"). Alternativa: abrir los scripts en SSMS o Azure Data Studio y ejecutarlos ahí |
| *Mailpit (opcional)* | Ver en el navegador los mails que manda el sistema | Contenedor de Docker (paso 6) o ejecutable de [GitHub](https://github.com/axllent/mailpit/releases) |
| *Visual Studio 2022+ (opcional)* | Correr con F5 | También sirve `dotnet run` desde la terminal |

### 1. Bajar el proyecto

```powershell
git clone https://github.com/santiezsa/TurnoTigre.git
cd TurnoTigre
```

### 2. Levantar SQL Server

**Con Docker** (elegí tu propia clave: mínimo 8 caracteres, con mayúsculas, minúsculas, números y símbolos; en los pasos siguientes se usa `TuClave_Segura1` como ejemplo):

```powershell
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TuClave_Segura1" -p 1433:1433 --name sqlserver-turnotigre -d mcr.microsoft.com/mssql/server:2022-latest
```

Esperá unos 15 segundos a que arranque. Las próximas veces alcanza con `docker start sqlserver-turnotigre`.

**Sin Docker:** usá tu SQL Server instalado y reemplazá en los comandos siguientes `localhost,1433 -U sa -P TuClave_Segura1` por tu servidor y credenciales (por ejemplo `-S .\SQLEXPRESS -E` para Windows Authentication).

### 3. Crear la base de datos

```powershell
sqlcmd -S localhost,1433 -U sa -P "TuClave_Segura1" -C -Q "CREATE DATABASE TurnoTigre"
sqlcmd -S localhost,1433 -U sa -P "TuClave_Segura1" -C -f 65001 -d TurnoTigre -i DB\script_creacion.sql
```

- `DB/script_creacion.sql` crea **todo**: tablas, función de hora de Argentina, datos iniciales (centros, especialidades, obras sociales) y el usuario Super Admin.
- ⚠️ **`-f 65001` es obligatorio** (lee los scripts como UTF-8). Sin eso los acentos y la "ñ" quedan rotos ("MaÃ±ana") y fallan las validaciones. Si te pasó, borrá la base y repetí.
- **Opcional, para tener datos de ejemplo** (usuarios de todos los roles, profesionales y pacientes):

```powershell
sqlcmd -S localhost,1433 -U sa -P "TuClave_Segura1" -C -f 65001 -d TurnoTigre -i DB\datos_prueba.sql
```

- **Si ya tenías la base de una versión anterior**, no la recrees: ejecutá `DB/migracion_funcionalidades.sql`. Se puede correr todas las veces que haga falta y no borra datos.
- `DB/02_indices_rendimiento.sql` (opcional) agrega índices para consultas más rápidas. `DB/script_superadmin.sql` solo sirve para recuperar el Super Admin; ambos están escritos para la base `TurnoTigre`.

### 4. Configurar la conexión

Creá el archivo **`Web/appsettings.Development.json`** (git lo ignora: ahí van tus claves, nunca en el repo) con este contenido, cambiando la clave por la tuya:

```json
{
  "ConnectionStrings": {
    "TurnoTigreDB": "Server=localhost,1433;Database=TurnoTigre;User Id=sa;Password=TuClave_Segura1;TrustServerCertificate=True"
  },
  "Correo": {
    "Host": "localhost",
    "Puerto": "1025",
    "Ssl": "false",
    "Usuario": "",
    "Remitente": "no-responder@tigre.gob.ar",
    "UrlBase": "http://localhost:5238"
  },
  "Demo": { "MostrarCuentasDePrueba": true }
}
```

- ⚠️ El `Web/appsettings.json` del repo trae una cadena de conexión de ejemplo con otra clave: **la tuya en `appsettings.Development.json` tiene prioridad**, no hace falta tocar el otro archivo.
- El bloque `Correo` apunta a Mailpit (paso 6). Si no lo vas a usar, borralo: sin servidor de correo el sistema funciona igual, solo que no manda mails.
- `Demo:MostrarCuentasDePrueba` muestra las cuentas de prueba en las pantallas de ingreso.
- Con Windows Authentication (SQL Server Express) la cadena sería: `Server=.\\SQLEXPRESS;Database=TurnoTigre;Trusted_Connection=True;TrustServerCertificate=True`.

### 5. Ejecutar la aplicación

```powershell
dotnet run --project Web --launch-profile http
```

Se abre en **http://localhost:5238** (o presioná F5 en Visual Studio). La primera vez tarda un poco porque descarga paquetes.

**Cuentas** (si cargaste `datos_prueba.sql`, la contraseña de todas es `Tigre2026`):

| Rol | Ingreso | Usuario |
| :--- | :--- | :--- |
| Super Admin | `/IngresarPersonal` | `super.admin` (te pide cambiar la clave la primera vez; viene con el script de creación) |
| Administrativo | `/IngresarPersonal` | `erika.espeche` |
| Médico | `/IngresarPersonal` | `laura.gomez` |
| Paciente | `/Ingresar` | `ana.perez@mail.com` (también podés registrarte desde "Crear mi cuenta") |

### 6. (Opcional) Mailpit: ver los mails

El sistema avisa por mail cada reserva, confirmación, cancelación, recordatorio y reasignación. Mailpit es una bandeja de entrada de pruebas: **recibe los mails sin mandarlos a nadie**.

```powershell
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
```

Abrí **http://localhost:8025** para ver los mails (las contraseñas provisorias, los enlaces de "olvidé mi contraseña", los recordatorios). Sin Docker, descargá el `.zip` de Windows desde [GitHub](https://github.com/axllent/mailpit/releases) y ejecutá `mailpit.exe`: usa los mismos puertos.

### 7. Los días siguientes

```powershell
docker start sqlserver-turnotigre mailpit
dotnet run --project Web --launch-profile http
```

### 8. Pruebas automáticas

Hay pruebas unitarias (no necesitan base) y de integración (necesitan una **base aparte** llamada exactamente `TurnoTigreTest`):

```powershell
sqlcmd -S localhost,1433 -U sa -P "TuClave_Segura1" -C -Q "CREATE DATABASE TurnoTigreTest"
sqlcmd -S localhost,1433 -U sa -P "TuClave_Segura1" -C -f 65001 -d TurnoTigreTest -i DB\script_creacion.sql
$env:TURNOTIGRE_TEST_CONN = "Server=localhost,1433;Database=TurnoTigreTest;User Id=sa;Password=TuClave_Segura1;TrustServerCertificate=True"
dotnet test TurnoTigre.Tests
```

⚠️ Las pruebas de integración **borran todos los datos** de esa base en cada ejecución, por eso se niegan a correr contra cualquier otra que no se llame `TurnoTigreTest`. Sin la variable `TURNOTIGRE_TEST_CONN` esas pruebas se saltean solas.

### Problemas frecuentes

| Síntoma | Causa y solución |
| :--- | :--- |
| `Login failed for user 'sa'` | La clave de `appsettings.Development.json` no es la del contenedor o la de tu SQL Server |
| El contenedor de SQL Server se apaga solo | La clave no cumple la complejidad pedida. Mirá `docker logs sqlserver-turnotigre` y volvelo a crear con otra |
| "MaÃ±ana", "Ã³" en la base o en pantalla | Faltó `-f 65001` al ejecutar los scripts. Borrá la base y repetí el paso 3 |
| `Cannot open database "TurnoTigre"` | Falta crear la base (paso 3) o el nombre de `Database=` es otro |
| Docker no arranca y dice `dockerInference ... The file cannot be accessed` | Con Docker cerrado, renombrá (no borres) las carpetas `%LOCALAPPDATA%\Docker\run` y `%LOCALAPPDATA%\docker-secrets-engine` si tienen archivos trabados, y abrilo de nuevo |
| El puerto 1433 o 5238 está ocupado | Cerrá lo que lo usa o cambiá el puerto de la izquierda en `-p` (y en la cadena de conexión) |
| `dotnet build` falla por archivos bloqueados | La aplicación sigue corriendo: detenela antes de compilar |
| No llegan los mails | Mailpit no está corriendo, o falta el bloque `Correo` (paso 4). Sin servidor de correo el sistema no manda nada y deja el aviso en la consola |
| Los plazos están corridos 3 horas | El servidor donde corre la aplicación debe estar en hora de Argentina (la base ya lo resuelve sola con `dbo.Ahora()`) |

Más detalle: [`PANTALLAS.md`](PANTALLAS.md) (todas las pantallas y reglas) y [`SUPERADMIN.md`](SUPERADMIN.md).

---
<div align="center">
  <i>Proyecto final desarrollado para la Tecnicatura Universitaria en Programación (UTN FRGP) - 2026.</i>
</div>


## Recuperar contraseña y correo

- "¿Olvidaste tu contraseña?" abre una ventana donde el paciente escribe su correo (el personal, su usuario). El sistema **siempre responde lo mismo**, exista o no la cuenta, y manda un enlace **solo al correo que figura en la cuenta**. El enlace sirve una vez, vence en 1 hora y se guarda solo su hash.
- Para que los correos salgan hay que completar la sección `Correo` (Host, Puerto, Ssl, Usuario, Clave, Remitente) **en `appsettings.Development.json`** en tu máquina, o con variables de entorno en producción (`Correo__Host`, `Correo__Clave`, ...). No poner usuario ni clave en `appsettings.json`, que se sube al repo. Con `Correo:UrlBase` (por ejemplo `https://turnos.tigre.gob.ar`) se fija la dirección de los enlaces de los mails.
- Sin SMTP configurado (`Host` vacío) el correo no se envía: el enlace queda escrito en la consola donde corre `dotnet run`, en el aviso "SMTP no configurado". En desarrollo conviene usar Mailpit (ver el paso 6 de la instalación) para ver los mails en el navegador.
- `Demo:MostrarCuentasDePrueba` muestra u oculta los datos de prueba en las pantallas de ingreso. Viene en `false` en `appsettings.json`; para verlos en tu máquina, ponelo en `true` en `appsettings.Development.json` (que no se sube al repo).

## Cifrado de los datos de salud

- La historia clínica se guarda **cifrada** (AES-256-GCM): grupo sanguíneo, alergias, enfermedades crónicas, patologías, los textos de cada consulta y el contenido de los archivos adjuntos. En la base se ve texto ilegible (`enc:v1:...`); la aplicación lo descifra al mostrarlo. Peso y altura quedan como números.
- **Desarrollo:** no hay que configurar nada; se usa una clave pública de desarrollo que está en el código.
- **Producción:** la aplicación **no arranca** sin una clave propia. Se configura con la variable de entorno `Cifrado__Clave` (32 bytes en base64). Para generarla en PowerShell:
  `$b = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)`
- ⚠️ **Si se pierde la clave, los datos cifrados no se pueden recuperar.** Guardala en un lugar seguro (gestor de secretos del servidor) y con copia de respaldo.
- Si la base tenía historias cargadas antes de este cambio, la aplicación las cifra sola al arrancar (y lo informa en la consola). Antes hay que correr `DB/migracion_funcionalidades.sql`, que agranda las columnas.
- Recomendado además en producción: activar TDE en SQL Server (cifra los archivos de la base y los backups en disco), conexión cifrada a la base (`Encrypt=True` con certificado válido, sin `TrustServerCertificate=True`) y HTTPS con certificado (la aplicación ya redirige a HTTPS y usa HSTS fuera de desarrollo).

## Hora del sistema

- Todos los plazos (vencimientos, ventana de confirmación, recordatorios) usan la **hora de Argentina**. En la base, la función `dbo.Ahora()` la calcula sin importar la zona horaria del servidor: un SQL Server en Docker o en la nube corre en UTC, y `GETDATE()` daría 3 horas de más. En el código SQL hay que usar siempre `dbo.Ahora()` y no `GETDATE()`.
- Las columnas de fecha que completa la base (fecha de envío de avisos, de inscripción, de auditoría, etc.) también usan `dbo.Ahora()` por defecto. `DB/migracion_funcionalidades.sql` crea la función y convierte las columnas de una base existente.
- La aplicación (C#) usa la hora de la máquina donde corre: el servidor de la aplicación tiene que estar configurado en hora de Argentina.

## Foto de perfil

El paciente puede subir una foto de su rostro desde **Mis datos** (JPG o PNG, se recorta y achica sola; máximo 2 MB). La ven el propio paciente y el personal (en el detalle del paciente).
