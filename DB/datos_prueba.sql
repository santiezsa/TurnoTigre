-- =====================================================================
-- TurnoTigre - Datos de PRUEBA (opcional). Ejecutar DESPUÉS de migracion_funcionalidades.sql
-- Crea usuarios de ejemplo (contraseña de todos: Tigre2026), profesionales y pacientes.
--   Administrativo: erika.espeche@tigre.gob.ar
--   Médico:         laura.gomez@tigre.gob.ar
--   Paciente:       ana.perez@mail.com (con 2 familiares a cargo)
-- Se puede ejecutar varias veces.
-- =====================================================================
DECLARE @hash VARCHAR(255) = '$2b$11$c/GBkJmJkhBPkJzIadk2huMTFZwBtmA0uZAGdkh.ozgw5rewoU9M2';
DECLARE @clin INT = (SELECT id_especialidad FROM ESPECIALIDAD WHERE codigo = 'CLIN');
DECLARE @card INT = (SELECT id_especialidad FROM ESPECIALIDAD WHERE codigo = 'CARD');
DECLARE @pedi INT = (SELECT id_especialidad FROM ESPECIALIDAD WHERE codigo = 'PEDI');
DECLARE @gine INT = (SELECT id_especialidad FROM ESPECIALIDAD WHERE codigo = 'GINE');
DECLARE @u INT, @p INT;

-- Administrativo
IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE nombre_usuario = 'erika.espeche')
BEGIN
    INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, debe_cambiar_password)
    VALUES ('erika.espeche', @hash, 'erika.espeche@tigre.gob.ar', 'Administrativo', 0);
    SET @u = SCOPE_IDENTITY();
    INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono) VALUES (@u, '39832220', 'Erika', 'Espeche', '11 5555-0001');
END

-- Médica con usuario
IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE nombre_usuario = 'laura.gomez')
BEGIN
    INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, debe_cambiar_password)
    VALUES ('laura.gomez', @hash, 'laura.gomez@tigre.gob.ar', 'Medico', 0);
    SET @u = SCOPE_IDENTITY();
    INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono, email) VALUES (@u, '28114520', 'Laura', 'Gómez', '', 'laura.gomez@tigre.gob.ar');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@p, 'MP 10234', @clin);
    INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES (@p, @clin);
END

-- Profesionales sin usuario
IF NOT EXISTS (SELECT 1 FROM PERSONA WHERE dni = '30221987')
BEGIN
    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('30221987', 'Marta', 'Salas', '11 5555-1201');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@p, 'MP 11877', @clin);
    INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES (@p, @clin), (@p, @card);
END
IF NOT EXISTS (SELECT 1 FROM PERSONA WHERE dni = '27665012')
BEGIN
    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('27665012', 'Julia', 'Rivas', '');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@p, 'MP 09541', @card);
    INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES (@p, @card);
END
IF NOT EXISTS (SELECT 1 FROM PERSONA WHERE dni = '33450871')
BEGIN
    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('33450871', 'Sofía', 'Benítez', '');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@p, 'MP 13112', @pedi);
    INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES (@p, @pedi);
END
IF NOT EXISTS (SELECT 1 FROM PERSONA WHERE dni = '26771903')
BEGIN
    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('26771903', 'Elena', 'Domínguez', '');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@p, 'MN 08812', @gine);
    INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES (@p, @gine);
END

-- Paciente titular con usuario y 2 familiares
DECLARE @titular INT;
IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE email = 'ana.perez@mail.com')
BEGIN
    INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, debe_cambiar_password)
    VALUES ('ana.perez@mail.com', @hash, 'ana.perez@mail.com', 'Paciente', 0);
    SET @u = SCOPE_IDENTITY();
    INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono) VALUES (@u, '32456789', 'Ana', 'Pérez', '11 4567-8900');
    SET @titular = SCOPE_IDENTITY();
    INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad)
    VALUES (@titular, '1986-05-14', 'Femenino', 'Av. Liniers', '1240', 'Benavídez');

    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('55678901', 'Sofía', 'Pérez', '11 4567-8900');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad, id_titular, parentesco)
    VALUES (@p, '2018-04-09', 'Femenino', 'Av. Liniers', '1240', 'Benavídez', @titular, 'Hijo/a');

    INSERT INTO PERSONA (dni, nombre, apellido, telefono) VALUES ('57112233', 'Mateo', 'Pérez', '11 4567-8900');
    SET @p = SCOPE_IDENTITY();
    INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad, id_titular, parentesco)
    VALUES (@p, '2021-08-22', 'Masculino', 'Av. Liniers', '1240', 'Benavídez', @titular, 'Hijo/a');
END

-- Pacientes cargados por el personal (sin usuario)
DECLARE @pacientes TABLE (dni VARCHAR(15), nombre VARCHAR(100), apellido VARCHAR(100), tel VARCHAR(20), email VARCHAR(100), nac DATE, sexo VARCHAR(15), calle VARCHAR(100), altura VARCHAR(10), loc VARCHAR(100), cp VARCHAR(10));
INSERT INTO @pacientes VALUES
 ('40112233', 'Lucas', 'Fernández', '11 5522-3344', 'lucasf@mail.com', '2001-03-12', 'Masculino', 'Av. Cazón', '1200', 'Tigre', '1648'),
 ('38445566', 'Camila', 'Ruiz', '11 6633-4455', 'camiru@mail.com', '1995-07-30', 'Femenino', 'Italia', '450', 'Tigre', '1648'),
 ('45998877', 'Mateo', 'Sosa', '11 4455-6677', 'sosa.mateo@mail.com', '2012-11-05', 'Masculino', 'Liniers', '88', 'Don Torcuato', '1611'),
 ('36778899', 'Valentina', 'Paz', '11 7788-9900', 'valepaz@mail.com', '1992-01-21', 'Femenino', 'Cazón', '760', 'Tigre', '1648');

DECLARE @d VARCHAR(15), @n VARCHAR(100), @a VARCHAR(100), @t VARCHAR(20), @e VARCHAR(100), @f DATE, @s VARCHAR(15), @c VARCHAR(100), @h VARCHAR(10), @l VARCHAR(100), @cp VARCHAR(10);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT dni, nombre, apellido, tel, email, nac, sexo, calle, altura, loc, cp FROM @pacientes;
OPEN cur;
FETCH NEXT FROM cur INTO @d, @n, @a, @t, @e, @f, @s, @c, @h, @l, @cp;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM PERSONA WHERE dni = @d)
    BEGIN
        INSERT INTO PERSONA (dni, nombre, apellido, telefono, email) VALUES (@d, @n, @a, @t, @e);
        SET @p = SCOPE_IDENTITY();
        INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad, codigo_postal, provincia)
        VALUES (@p, @f, @s, @c, @h, @l, @cp, 'Buenos Aires');
    END
    FETCH NEXT FROM cur INTO @d, @n, @a, @t, @e, @f, @s, @c, @h, @l, @cp;
END
CLOSE cur;
DEALLOCATE cur;
