CREATE TABLE USUARIO (
  id_usuario INT PRIMARY KEY IDENTITY(1,1),
  nombre_usuario VARCHAR(100) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  email VARCHAR(100) NOT NULL UNIQUE,
  rol VARCHAR(30) NOT NULL CHECK (rol IN ('Paciente', 'Medico', 'Administrativo', 'SuperAdmin')),
  activo BIT NOT NULL DEFAULT 1,
  debe_cambiar_password BIT NOT NULL DEFAULT 0
);

CREATE TABLE PERSONA (
  id_persona INT PRIMARY KEY IDENTITY(1,1),
  id_usuario INT,
  dni VARCHAR(15) NOT NULL UNIQUE,
  nombre VARCHAR(100) NOT NULL,
  apellido VARCHAR(100) NOT NULL,
  telefono VARCHAR(20) NOT NULL,
  activo BIT NOT NULL DEFAULT 1
);

CREATE TABLE OBRA_SOCIAL (
  id_obra_social INT PRIMARY KEY IDENTITY(1,1),
  nombre VARCHAR(100) NOT NULL
);

CREATE TABLE PACIENTE (
  id_paciente INT PRIMARY KEY,
  fecha_nacimiento DATE NOT NULL,
  sexo VARCHAR(15) NOT NULL,
  calle VARCHAR(100) NOT NULL,
  altura VARCHAR(10) NOT NULL,
  localidad VARCHAR(100) NOT NULL,
  id_obra_social INT,
  nro_afiliado VARCHAR(50),
  id_titular INT,
  parentesco VARCHAR(30)
);

CREATE TABLE ESPECIALIDAD (
  id_especialidad INT PRIMARY KEY IDENTITY(1,1),
  codigo VARCHAR(20) NOT NULL UNIQUE,
  descripcion VARCHAR(150) NOT NULL
);

CREATE TABLE CENTRO_SALUD (
  id_centro INT PRIMARY KEY IDENTITY(1,1),
  codigo VARCHAR(20) NOT NULL UNIQUE,
  nombre VARCHAR(150) NOT NULL,
  direccion VARCHAR(200) NOT NULL,
  localidad VARCHAR(100) NOT NULL
);

CREATE TABLE MEDICO (
  id_medico INT PRIMARY KEY,
  matricula VARCHAR(50) NOT NULL UNIQUE,
  id_especialidad INT NOT NULL
);

CREATE TABLE AGENDA (
  id_agenda INT PRIMARY KEY IDENTITY(1,1),
  id_medico INT NOT NULL,
  id_centro INT NOT NULL,
  vigencia_desde DATETIME NOT NULL,
  vigencia_hasta DATETIME NOT NULL,
  duracion_minutos INT NOT NULL,
  pacientes_por_franja INT NOT NULL
);

CREATE TABLE TURNO (
  id_turno INT PRIMARY KEY IDENTITY(1,1),
  id_agenda INT NOT NULL,
  id_paciente INT,
  fecha_hora DATETIME NOT NULL,
  estado VARCHAR(30) NOT NULL CHECK (estado IN ('Disponible', 'PendienteConfirmacion', 'Confirmado', 'Cancelado', 'Vencido', 'Atendido')),
  fecha_reserva DATETIME,
  limite_confirmacion DATETIME,
  observaciones VARCHAR(255)
);

CREATE TABLE HISTORIAL_TURNO (
  id_historial INT PRIMARY KEY IDENTITY(1,1),
  id_turno INT NOT NULL,
  id_paciente INT,
  fecha_hora DATETIME NOT NULL DEFAULT GETDATE(),
  estado_anterior VARCHAR(30),
  estado_nuevo VARCHAR(30) NOT NULL,
  motivo VARCHAR(255)
);

CREATE TABLE LISTA_ESPERA (
  id_espera INT PRIMARY KEY IDENTITY(1,1),
  id_paciente INT NOT NULL,
  id_especialidad INT NOT NULL,
  id_centro INT NOT NULL,
  franja VARCHAR(20) NOT NULL CHECK (franja IN ('Mañana', 'Tarde', 'Indistinto')),
  fecha_inscripcion DATETIME NOT NULL DEFAULT GETDATE(),
  fecha_vencimiento DATETIME,
  activa BIT NOT NULL DEFAULT 1
);

CREATE TABLE NOTIFICACION (
  id_notificacion INT PRIMARY KEY IDENTITY(1,1),
  id_usuario INT NOT NULL,
  id_turno INT,
  canal VARCHAR(20) NOT NULL CHECK (canal IN ('Email', 'WhatsApp', 'App')),
  tipo_notificacion VARCHAR(50) NOT NULL,
  mensaje VARCHAR(500) NOT NULL,
  fecha_envio DATETIME NOT NULL DEFAULT GETDATE(),
  limite_respuesta DATETIME,
  leida BIT NOT NULL DEFAULT 0,
  estado VARCHAR(30) NOT NULL
);

-- Atado de relaciones (Foreign Keys)
ALTER TABLE PERSONA ADD FOREIGN KEY (id_usuario) REFERENCES USUARIO (id_usuario);
ALTER TABLE PACIENTE ADD FOREIGN KEY (id_paciente) REFERENCES PERSONA (id_persona);
ALTER TABLE PACIENTE ADD FOREIGN KEY (id_obra_social) REFERENCES OBRA_SOCIAL (id_obra_social);
ALTER TABLE PACIENTE ADD FOREIGN KEY (id_titular) REFERENCES PACIENTE (id_paciente);
ALTER TABLE MEDICO ADD FOREIGN KEY (id_medico) REFERENCES PERSONA (id_persona);
ALTER TABLE MEDICO ADD FOREIGN KEY (id_especialidad) REFERENCES ESPECIALIDAD (id_especialidad);
ALTER TABLE AGENDA ADD FOREIGN KEY (id_medico) REFERENCES MEDICO (id_medico);
ALTER TABLE AGENDA ADD FOREIGN KEY (id_centro) REFERENCES CENTRO_SALUD (id_centro);
ALTER TABLE TURNO ADD FOREIGN KEY (id_agenda) REFERENCES AGENDA (id_agenda);
ALTER TABLE TURNO ADD FOREIGN KEY (id_paciente) REFERENCES PACIENTE (id_paciente);
ALTER TABLE HISTORIAL_TURNO ADD FOREIGN KEY (id_turno) REFERENCES TURNO (id_turno);
ALTER TABLE HISTORIAL_TURNO ADD FOREIGN KEY (id_paciente) REFERENCES PACIENTE (id_paciente);
ALTER TABLE LISTA_ESPERA ADD FOREIGN KEY (id_paciente) REFERENCES PACIENTE (id_paciente);
ALTER TABLE LISTA_ESPERA ADD FOREIGN KEY (id_especialidad) REFERENCES ESPECIALIDAD (id_especialidad);
ALTER TABLE LISTA_ESPERA ADD FOREIGN KEY (id_centro) REFERENCES CENTRO_SALUD (id_centro);
ALTER TABLE NOTIFICACION ADD FOREIGN KEY (id_usuario) REFERENCES USUARIO (id_usuario);
ALTER TABLE NOTIFICACION ADD FOREIGN KEY (id_turno) REFERENCES TURNO (id_turno);

-- Super admin inicial (contraseña: Tigre2026 -> se pide cambiarla al primer ingreso)
INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password)
VALUES ('super.admin', '$2b$11$c/GBkJmJkhBPkJzIadk2huMTFZwBtmA0uZAGdkh.ozgw5rewoU9M2',
        'super.admin@tigre.gob.ar', 'SuperAdmin', 1, 1);
GO

-- =====================================================================
-- TurnoTigre - Estructura para agendas, turnos, profesionales, familiares,
-- historia clínica y datos iniciales.
-- Se puede ejecutar varias veces (no duplica nada) y sirve tanto para una base
-- recién creada con script_creacion.sql como para una base que ya existía.
-- Ejecutar con la base TurnoTigre seleccionada.
-- =====================================================================

-- ---------- USUARIO: rol SuperAdmin + cambio de clave obligatorio ----------
IF COL_LENGTH('USUARIO', 'debe_cambiar_password') IS NULL
    ALTER TABLE USUARIO ADD debe_cambiar_password BIT NOT NULL CONSTRAINT DF_USUARIO_debe_cambiar DEFAULT 0;
GO

DECLARE @ckUsuario NVARCHAR(200);
SELECT @ckUsuario = cc.name FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('USUARIO') AND cc.definition LIKE '%rol%' AND cc.definition NOT LIKE '%SuperAdmin%';
IF @ckUsuario IS NOT NULL EXEC('ALTER TABLE USUARIO DROP CONSTRAINT [' + @ckUsuario + ']');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('USUARIO') AND definition LIKE '%SuperAdmin%')
    ALTER TABLE USUARIO ADD CONSTRAINT CK_USUARIO_rol CHECK (rol IN ('Paciente', 'Medico', 'Administrativo', 'SuperAdmin'));
GO

IF NOT EXISTS (SELECT 1 FROM USUARIO WHERE nombre_usuario = 'super.admin')
    INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password)
    VALUES ('super.admin', '$2b$11$c/GBkJmJkhBPkJzIadk2huMTFZwBtmA0uZAGdkh.ozgw5rewoU9M2', 'super.admin@tigre.gob.ar', 'SuperAdmin', 1, 1);
GO

-- ---------- PERSONA / PACIENTE: datos adicionales ----------
IF COL_LENGTH('PERSONA', 'email') IS NULL ALTER TABLE PERSONA ADD email VARCHAR(100) NULL;
IF COL_LENGTH('PACIENTE', 'tipo_documento') IS NULL ALTER TABLE PACIENTE ADD tipo_documento VARCHAR(15) NOT NULL CONSTRAINT DF_PACIENTE_tipodoc DEFAULT 'DNI';
IF COL_LENGTH('PACIENTE', 'telefono_fijo') IS NULL ALTER TABLE PACIENTE ADD telefono_fijo VARCHAR(20) NULL;
IF COL_LENGTH('PACIENTE', 'codigo_postal') IS NULL ALTER TABLE PACIENTE ADD codigo_postal VARCHAR(10) NULL;
IF COL_LENGTH('PACIENTE', 'provincia') IS NULL ALTER TABLE PACIENTE ADD provincia VARCHAR(50) NULL;
GO

-- ---------- AGENDA: tipo, horario y días ----------
IF COL_LENGTH('AGENDA', 'id_especialidad') IS NULL ALTER TABLE AGENDA ADD id_especialidad INT NULL;
IF COL_LENGTH('AGENDA', 'tipo') IS NULL ALTER TABLE AGENDA ADD tipo VARCHAR(10) NOT NULL CONSTRAINT DF_AGENDA_tipo DEFAULT 'Mensual';
IF COL_LENGTH('AGENDA', 'hora_inicio') IS NULL ALTER TABLE AGENDA ADD hora_inicio VARCHAR(5) NULL;
IF COL_LENGTH('AGENDA', 'hora_fin') IS NULL ALTER TABLE AGENDA ADD hora_fin VARCHAR(5) NULL;
IF COL_LENGTH('AGENDA', 'dias_semana') IS NULL ALTER TABLE AGENDA ADD dias_semana VARCHAR(40) NULL;
GO

IF OBJECT_ID('AGENDA_FECHA', 'U') IS NULL
CREATE TABLE AGENDA_FECHA (
  id_agenda_fecha INT PRIMARY KEY IDENTITY(1,1),
  id_agenda INT NOT NULL,
  fecha DATE NOT NULL,
  hora_inicio VARCHAR(5) NOT NULL,
  hora_fin VARCHAR(5) NOT NULL,
  CONSTRAINT FK_AGENDA_FECHA_agenda FOREIGN KEY (id_agenda) REFERENCES AGENDA (id_agenda)
);
GO

IF OBJECT_ID('MEDICO_ESPECIALIDAD', 'U') IS NULL
CREATE TABLE MEDICO_ESPECIALIDAD (
  id_medico INT NOT NULL,
  id_especialidad INT NOT NULL,
  CONSTRAINT PK_MEDICO_ESPECIALIDAD PRIMARY KEY (id_medico, id_especialidad),
  CONSTRAINT FK_MEDESP_medico FOREIGN KEY (id_medico) REFERENCES MEDICO (id_medico),
  CONSTRAINT FK_MEDESP_esp FOREIGN KEY (id_especialidad) REFERENCES ESPECIALIDAD (id_especialidad)
);
GO

INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad)
SELECT m.id_medico, m.id_especialidad FROM MEDICO m
WHERE NOT EXISTS (SELECT 1 FROM MEDICO_ESPECIALIDAD me WHERE me.id_medico = m.id_medico AND me.id_especialidad = m.id_especialidad);
GO

UPDATE AGENDA SET id_especialidad = (SELECT m.id_especialidad FROM MEDICO m WHERE m.id_medico = AGENDA.id_medico)
WHERE id_especialidad IS NULL;
GO

-- ---------- TURNO: estados nuevos (Presente, Ausente, EnAtencion) ----------
DECLARE @ckTurno NVARCHAR(200);
SELECT @ckTurno = cc.name FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID('TURNO') AND cc.definition LIKE '%estado%' AND cc.definition NOT LIKE '%EnAtencion%';
IF @ckTurno IS NOT NULL EXEC('ALTER TABLE TURNO DROP CONSTRAINT [' + @ckTurno + ']');
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('TURNO') AND definition LIKE '%EnAtencion%')
    ALTER TABLE TURNO ADD CONSTRAINT CK_TURNO_estado CHECK (estado IN
      ('Disponible', 'PendienteConfirmacion', 'Confirmado', 'Cancelado', 'Vencido', 'Atendido', 'Presente', 'Ausente', 'EnAtencion'));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TURNO_agenda_fecha' AND object_id = OBJECT_ID('TURNO'))
    CREATE INDEX IX_TURNO_agenda_fecha ON TURNO (id_agenda, fecha_hora);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TURNO_paciente' AND object_id = OBJECT_ID('TURNO'))
    CREATE INDEX IX_TURNO_paciente ON TURNO (id_paciente);
GO

-- ---------- Historia clínica ----------
IF OBJECT_ID('HISTORIA_CLINICA', 'U') IS NULL
CREATE TABLE HISTORIA_CLINICA (
  id_paciente INT PRIMARY KEY,
  grupo_sanguineo VARCHAR(5) NULL,
  peso DECIMAL(5,1) NULL,
  altura_cm INT NULL,
  alergias VARCHAR(120) NULL,
  enfermedades_cronicas VARCHAR(120) NULL,
  patologias VARCHAR(120) NULL,
  fecha_actualizacion DATETIME NOT NULL DEFAULT GETDATE(),
  CONSTRAINT FK_HC_paciente FOREIGN KEY (id_paciente) REFERENCES PACIENTE (id_paciente)
);
GO

IF OBJECT_ID('CONSULTA', 'U') IS NULL
CREATE TABLE CONSULTA (
  id_consulta INT PRIMARY KEY IDENTITY(1,1),
  id_turno INT NULL,
  id_paciente INT NOT NULL,
  id_medico INT NOT NULL,
  fecha DATETIME NOT NULL DEFAULT GETDATE(),
  observaciones VARCHAR(800) NOT NULL,
  diagnostico VARCHAR(800) NOT NULL,
  tratamientos VARCHAR(800) NULL,
  proximos_pasos VARCHAR(800) NULL,
  CONSTRAINT FK_CONSULTA_turno FOREIGN KEY (id_turno) REFERENCES TURNO (id_turno),
  CONSTRAINT FK_CONSULTA_paciente FOREIGN KEY (id_paciente) REFERENCES PACIENTE (id_paciente),
  CONSTRAINT FK_CONSULTA_medico FOREIGN KEY (id_medico) REFERENCES MEDICO (id_medico)
);
GO

IF OBJECT_ID('CONSULTA_ARCHIVO', 'U') IS NULL
CREATE TABLE CONSULTA_ARCHIVO (
  id_archivo INT PRIMARY KEY IDENTITY(1,1),
  id_consulta INT NOT NULL,
  nombre VARCHAR(200) NOT NULL,
  tipo_contenido VARCHAR(100) NOT NULL,
  contenido VARBINARY(MAX) NOT NULL,
  CONSTRAINT FK_ARCHIVO_consulta FOREIGN KEY (id_consulta) REFERENCES CONSULTA (id_consulta)
);
GO

-- ---------- Datos iniciales (solo si las tablas están vacías) ----------
IF NOT EXISTS (SELECT 1 FROM ESPECIALIDAD)
    INSERT INTO ESPECIALIDAD (codigo, descripcion) VALUES
      ('CARD', 'Cardiología'), ('CLIN', 'Clínica médica'), ('GINE', 'Ginecología'),
      ('ODON', 'Odontología'), ('PEDI', 'Pediatría'), ('TRAU', 'Traumatología');
GO

IF NOT EXISTS (SELECT 1 FROM CENTRO_SALUD)
    INSERT INTO CENTRO_SALUD (codigo, nombre, direccion, localidad) VALUES
      ('BENA', 'CAPS Benavídez', 'Av. Cazón 1514', 'Benavídez'),
      ('DTOR', 'CAPS Don Torcuato', 'Av. Liniers 800', 'Don Torcuato'),
      ('ETAL', 'CAPS El Talar', 'Av. Hipólito Yrigoyen 450', 'El Talar');
GO

IF NOT EXISTS (SELECT 1 FROM OBRA_SOCIAL)
    INSERT INTO OBRA_SOCIAL (nombre) VALUES
      ('OSDE'), ('Swiss Medical'), ('Galeno'), ('Medifé'), ('OMINT'),
      ('Sancor Salud'), ('IOMA'), ('PAMI'), ('OSECAC'), ('Unión Personal');
GO

-- ---------- Foto de perfil y recuperación de contraseña ----------
IF COL_LENGTH('PERSONA', 'foto') IS NULL ALTER TABLE PERSONA ADD foto VARBINARY(MAX) NULL;
IF COL_LENGTH('PERSONA', 'foto_tipo') IS NULL ALTER TABLE PERSONA ADD foto_tipo VARCHAR(50) NULL;
GO

-- Enlaces de "Olvidé mi contraseña": se guarda solo el hash del enlace, vence en 1 hora y sirve una vez
IF OBJECT_ID('RECUPERACION_PASSWORD', 'U') IS NULL
CREATE TABLE RECUPERACION_PASSWORD (
  id_recuperacion INT PRIMARY KEY IDENTITY(1,1),
  id_usuario INT NOT NULL,
  token_hash CHAR(64) NOT NULL,
  creado DATETIME NOT NULL DEFAULT GETDATE(),
  vence DATETIME NOT NULL,
  usado BIT NOT NULL DEFAULT 0,
  CONSTRAINT FK_RECUP_usuario FOREIGN KEY (id_usuario) REFERENCES USUARIO (id_usuario)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RECUP_token' AND object_id = OBJECT_ID('RECUPERACION_PASSWORD'))
    CREATE INDEX IX_RECUP_token ON RECUPERACION_PASSWORD (token_hash);
GO
