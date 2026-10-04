CREATE TABLE USUARIO (
  id_usuario INT PRIMARY KEY IDENTITY(1,1),
  nombre_usuario VARCHAR(50) NOT NULL UNIQUE,
  password_hash VARCHAR(255) NOT NULL,
  email VARCHAR(100) NOT NULL UNIQUE,
  rol VARCHAR(30) NOT NULL CHECK (rol IN ('Paciente', 'Medico', 'Administrativo')),
  activo BIT NOT NULL DEFAULT 1
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