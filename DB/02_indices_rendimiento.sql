-- ============================================================================
-- TurnoTigre - Script de Índices para Rendimiento y Optimización de Consultas
-- ============================================================================
USE TurnoTigre;
GO

-- 1. Búsqueda y filtrado de turnos por estado y fecha_hora (confirmaciones, cancelaciones, reportes)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_TURNO_estado_fecha' AND object_id = OBJECT_ID('TURNO'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TURNO_estado_fecha
    ON TURNO (estado, fecha_hora)
    INCLUDE (id_agenda, id_paciente);
    PRINT 'Índice IX_TURNO_estado_fecha creado.';
END
GO

-- 2. Búsqueda de agendas por profesional, centro de salud y especialidad
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AGENDA_medico_centro' AND object_id = OBJECT_ID('AGENDA'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AGENDA_medico_centro
    ON AGENDA (id_medico, id_centro, id_especialidad)
    INCLUDE (vigencia_desde, vigencia_hasta);
    PRINT 'Índice IX_AGENDA_medico_centro creado.';
END
GO

-- 3. Fechas puntuales habilitadas para cada agenda
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AGENDA_FECHA_agenda_fecha' AND object_id = OBJECT_ID('AGENDA_FECHA'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_AGENDA_FECHA_agenda_fecha
    ON AGENDA_FECHA (id_agenda, fecha);
    PRINT 'Índice IX_AGENDA_FECHA_agenda_fecha creado.';
END
GO

-- 4. Búsqueda y ordenamiento en lista de espera por prioridad cronológica
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_LISTA_ESPERA_activa_esp_centro' AND object_id = OBJECT_ID('LISTA_ESPERA'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LISTA_ESPERA_activa_esp_centro
    ON LISTA_ESPERA (activa, id_especialidad, id_centro, fecha_inscripcion);
    PRINT 'Índice IX_LISTA_ESPERA_activa_esp_centro creado.';
END
GO

-- 5. Búsqueda inversa de médicos por especialidad
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_MEDICO_ESP_especialidad' AND object_id = OBJECT_ID('MEDICO_ESPECIALIDAD'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_MEDICO_ESP_especialidad
    ON MEDICO_ESPECIALIDAD (id_especialidad, id_medico);
    PRINT 'Índice IX_MEDICO_ESP_especialidad creado.';
END
GO
