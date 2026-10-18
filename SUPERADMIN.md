# Rol Super Admin

Gestiona los usuarios internos del sistema (Administrativo, Médico, Super Admin) y puede habilitar/deshabilitar cualquier cuenta.

## Base de datos
- **BD nueva**: `DB/script_creacion.sql` ya incluye el rol `SuperAdmin`, la columna `debe_cambiar_password` y el usuario inicial.
- **BD ya creada**: ejecutar `DB/script_superadmin.sql` (se puede correr más de una vez).
- Usuario inicial (se ingresa por "Soy personal", con usuario, no con correo): `super.admin` / `Tigre2026` (pide cambiar la clave al ingresar).

## Qué se agregó
- `Dominio`: `Roles`, `UsuarioListado`, `Usuario.DebeCambiarPassword`.
- `AccesoDatos/UsuarioDatos.cs`: listar con filtros, alta (USUARIO + PERSONA [+ MEDICO]), edición, estado, cambio de clave.
- `Negocio/UsuarioNegocio.cs`: usuario automático `nombre.apellido` (+ número si existe), contraseña provisoria `Tigre` + 4 dígitos + mayúscula + minúscula, reglas (no deshabilitarte a vos mismo ni al último Super Admin activo, email/DNI/matrícula únicos).
- `Web/Pages/Admin/` (`Usuarios`, `Nuevo`, `Editar`), `CambiarPassword`, `Salir`, menú del layout según sesión.
- `Web/Servicios/CorreoService.cs`: envío por SMTP (sección `Correo` de `appsettings.json`). Sin Host configurado no envía: lo deja en el log y, **solo en Development**, muestra la clave provisoria en pantalla.
- `Program.cs`: servicio de correo + redirección obligatoria a `/CambiarPassword` mientras la clave sea provisoria.

## Pendiente
- Las pantallas de Médico y Administrativo ya existen (ver `PANTALLAS.md`), pero con datos de ejemplo.
- Para mails reales: completar `Correo:Host/Usuario/Clave` (mejor en `appsettings.Development.json` o variables de entorno, no en el repo).
