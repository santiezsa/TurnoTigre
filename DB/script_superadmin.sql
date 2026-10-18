-- =====================================================================
-- TurnoTigre - Rol SuperAdmin
-- Para bases de datos YA creadas con script_creacion.sql (versión vieja).
-- Si creás la base desde cero con script_creacion.sql actualizado, NO hace falta.
-- Es seguro ejecutarlo más de una vez.
-- =====================================================================
USE TurnoTigre;
GO

-- 1) Columna para forzar el cambio de contraseña en el primer ingreso
IF COL_LENGTH('USUARIO', 'debe_cambiar_password') IS NULL
    ALTER TABLE USUARIO ADD debe_cambiar_password BIT NOT NULL CONSTRAINT DF_USUARIO_debe_cambiar DEFAULT 0;
GO

-- 2) Reemplazar el CHECK del rol para permitir 'SuperAdmin'
DECLARE @ck NVARCHAR(200);
SELECT @ck = cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('USUARIO') AND cc.definition LIKE '%rol%';
IF @ck IS NOT NULL EXEC('ALTER TABLE USUARIO DROP CONSTRAINT [' + @ck + ']');
ALTER TABLE USUARIO ADD CONSTRAINT CK_USUARIO_rol
    CHECK (rol IN ('Paciente', 'Medico', 'Administrativo', 'SuperAdmin'));
GO

-- 3) Super admin inicial (contraseña: Tigre2026  -> cambiarla al ingresar)
IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE nombre_usuario = 'super.admin')
BEGIN
    INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password)
    VALUES ('super.admin',
            '$2b$11$c/GBkJmJkhBPkJzIadk2huMTFZwBtmA0uZAGdkh.ozgw5rewoU9M2',
            'super.admin@tigre.gob.ar', 'SuperAdmin', 1, 1);
END
GO
