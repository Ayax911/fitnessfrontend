DO $$
DECLARE
    v_admin_id UUID := 'a0000000-0000-0000-0000-000000000001';
    v_entrenador1_id UUID := 'e0000000-0000-0000-0000-000000000001';
    v_entrenador2_id UUID := 'e0000000-0000-0000-0000-000000000002';
    v_usuario1_id UUID := '10000000-0000-0000-0000-000000000001';
    v_usuario2_id UUID := '10000000-0000-0000-0000-000000000002';
    
    v_role_usuario_id UUID := '11111111-1111-1111-1111-111111111111';
    v_role_entrenador_id UUID := '22222222-2222-2222-2222-222222222222';
    v_role_admin_id UUID := '33333333-3333-3333-3333-333333333333';

    v_evento1_id UUID := 'e1000000-0000-0000-0000-000000000001';
    v_evento2_id UUID := 'e2000000-0000-0000-0000-000000000002';
    v_evento3_id UUID := 'e3000000-0000-0000-0000-000000000003';

    v_hash TEXT := 'AQAAAAIAAYagAAAAEG3EbJghQqT9AFpqNI+XAG5tw3Ca0nLiiD75Buhx5m4tvz1m54YYZZHzyq+BXyzI3w==';
BEGIN
    -- Asegurar roles
    INSERT INTO "Roles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp")
    VALUES 
        (v_role_usuario_id, 'Usuario', 'USUARIO', gen_random_uuid()::text),
        (v_role_entrenador_id, 'Entrenador', 'ENTRENADOR', gen_random_uuid()::text),
        (v_role_admin_id, 'Administrador', 'ADMINISTRADOR', gen_random_uuid()::text)
    ON CONFLICT ("Id") DO NOTHING;

    -- Insertar Usuarios
    -- Administrador
    INSERT INTO "Usuarios" ("Id", "NombreCompleto", "FechaRegistro", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
    VALUES (v_admin_id, 'Administrador Gimnasio ITM', NOW(), 'admin@itm.edu.co', 'ADMIN@ITM.EDU.CO', 'admin@itm.edu.co', 'ADMIN@ITM.EDU.CO', TRUE, v_hash, gen_random_uuid()::text, gen_random_uuid()::text, FALSE, FALSE, FALSE, 0)
    ON CONFLICT ("Id") DO UPDATE SET "PasswordHash" = v_hash, "NombreCompleto" = 'Administrador Gimnasio ITM';

    -- Entrenador 1: Carlos Mendoza
    INSERT INTO "Usuarios" ("Id", "NombreCompleto", "FechaRegistro", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
    VALUES (v_entrenador1_id, 'Prof. Carlos Mendoza', NOW(), 'carlos.entrenador@itm.edu.co', 'CARLOS.ENTRENADOR@ITM.EDU.CO', 'carlos.entrenador@itm.edu.co', 'CARLOS.ENTRENADOR@ITM.EDU.CO', TRUE, v_hash, gen_random_uuid()::text, gen_random_uuid()::text, FALSE, FALSE, FALSE, 0)
    ON CONFLICT ("Id") DO UPDATE SET "PasswordHash" = v_hash, "NombreCompleto" = 'Prof. Carlos Mendoza';

    -- Entrenador 2: Andrea Morales
    INSERT INTO "Usuarios" ("Id", "NombreCompleto", "FechaRegistro", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
    VALUES (v_entrenador2_id, 'Prof. Andrea Morales', NOW(), 'andrea.entrenador@itm.edu.co', 'ANDREA.ENTRENADOR@ITM.EDU.CO', 'andrea.entrenador@itm.edu.co', 'ANDREA.ENTRENADOR@ITM.EDU.CO', TRUE, v_hash, gen_random_uuid()::text, gen_random_uuid()::text, FALSE, FALSE, FALSE, 0)
    ON CONFLICT ("Id") DO UPDATE SET "PasswordHash" = v_hash, "NombreCompleto" = 'Prof. Andrea Morales';

    -- Usuario 1: Juan Perez (Estudiante)
    INSERT INTO "Usuarios" ("Id", "NombreCompleto", "FechaRegistro", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
    VALUES (v_usuario1_id, 'Juan Pérez (Estudiante)', NOW(), 'juan.estudiante@itm.edu.co', 'JUAN.ESTUDIANTE@ITM.EDU.CO', 'juan.estudiante@itm.edu.co', 'JUAN.ESTUDIANTE@ITM.EDU.CO', TRUE, v_hash, gen_random_uuid()::text, gen_random_uuid()::text, FALSE, FALSE, FALSE, 0)
    ON CONFLICT ("Id") DO UPDATE SET "PasswordHash" = v_hash, "NombreCompleto" = 'Juan Pérez (Estudiante)';

    -- Usuario 2: Maria Gómez (Estudiante)
    INSERT INTO "Usuarios" ("Id", "NombreCompleto", "FechaRegistro", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
    VALUES (v_usuario2_id, 'María Gómez (Estudiante)', NOW(), 'maria.estudiante@itm.edu.co', 'MARIA.ESTUDIANTE@ITM.EDU.CO', 'maria.estudiante@itm.edu.co', 'MARIA.ESTUDIANTE@ITM.EDU.CO', TRUE, v_hash, gen_random_uuid()::text, gen_random_uuid()::text, FALSE, FALSE, FALSE, 0)
    ON CONFLICT ("Id") DO UPDATE SET "PasswordHash" = v_hash, "NombreCompleto" = 'María Gómez (Estudiante)';

    -- Asignación de Roles en UsuarioRoles
    DELETE FROM "UsuarioRoles" WHERE "UserId" IN (v_admin_id, v_entrenador1_id, v_entrenador2_id, v_usuario1_id, v_usuario2_id);
    INSERT INTO "UsuarioRoles" ("UserId", "RoleId") VALUES
        (v_admin_id, v_role_admin_id),
        (v_entrenador1_id, v_role_entrenador_id),
        (v_entrenador2_id, v_role_entrenador_id),
        (v_usuario1_id, v_role_usuario_id),
        (v_usuario2_id, v_role_usuario_id);

    -- Indicadores de Priorización para usuarios
    INSERT INTO "IndicadoresConfiabilidad" ("UsuarioId", "TotalSesionesAsistidas", "TotalInasistencias", "RachaActualCumplimiento", "NivelPriorizacion", "FechaUltimoRecalculo")
    VALUES 
        (v_usuario1_id, 8, 0, 5, 'Prioritario', NOW()),
        (v_usuario2_id, 0, 0, 0, 'Nuevo', NOW())
    ON CONFLICT ("UsuarioId") DO UPDATE SET 
        "TotalSesionesAsistidas" = EXCLUDED."TotalSesionesAsistidas",
        "NivelPriorizacion" = EXCLUDED."NivelPriorizacion";

    -- Puntos de acceso generales (Torniquetes)
    INSERT INTO "PuntosAcceso" ("Id", "Nombre", "TokenQR", "Activo")
    VALUES 
        ('f0000000-0000-0000-0000-000000000001', 'Torniquete Principal - Campus Robledo', 'QR-TORNIQUETE-ROBLEDO-01', TRUE),
        ('f0000000-0000-0000-0000-000000000002', 'Torniquete Acceso - Campus Fraternidad', 'QR-TORNIQUETE-FRATERNIDAD-01', TRUE)
    ON CONFLICT ("Id") DO NOTHING;

    -- Evento 1: Spinning & Cardio Intensivo (Entrenador: Carlos Mendoza)
    INSERT INTO "Eventos" ("Id", "Nombre", "Descripcion", "EntrenadorId", "EsRecurrente", "CupoMaximoPorDefecto", "FechaAperturaReservas", "Activo")
    VALUES (v_evento1_id, 'Spinning & Cardio Intensivo', 'Clase grupal de ciclismo bajo techo para trabajo cardiovascular y resistencia.', v_entrenador1_id, TRUE, 20, NOW() - INTERVAL '1 day', TRUE)
    ON CONFLICT ("Id") DO NOTHING;

    -- Evento 2: Entrenamiento Funcional y Cross (Entrenador: Andrea Morales)
    INSERT INTO "Eventos" ("Id", "Nombre", "Descripcion", "EntrenadorId", "EsRecurrente", "CupoMaximoPorDefecto", "FechaAperturaReservas", "Activo")
    VALUES (v_evento2_id, 'Entrenamiento Funcional y Cross', 'Circuitos de alta intensidad para tonificación muscular y coordinación.', v_entrenador2_id, TRUE, 15, NOW() - INTERVAL '2 days', TRUE)
    ON CONFLICT ("Id") DO NOTHING;

    -- Evento 3: Taller de Levantamiento Olímpico (Sesión Única Especial)
    INSERT INTO "Eventos" ("Id", "Nombre", "Descripcion", "EntrenadorId", "EsRecurrente", "CupoMaximoPorDefecto", "FechaAperturaReservas", "Activo")
    VALUES (v_evento3_id, 'Taller de Levantamiento Olímpico y Fuerza', 'Técnica de arranque, envión y sentadilla pesada con supervisión personalizada.', v_entrenador1_id, FALSE, 10, NOW() - INTERVAL '1 day', TRUE)
    ON CONFLICT ("Id") DO NOTHING;

    -- Sesiones para Evento 1 (Spinning)
    -- Sesión 1: Próxima en 4 horas
    INSERT INTO "SesionesEvento" ("Id", "EventoId", "FechaHoraInicio", "FechaHoraFin", "CupoMaximo", "VentanaAsistenciaInicio", "VentanaAsistenciaFin", "TokenQR", "Consolidada")
    VALUES (
        '11000000-0000-0000-0000-000000000001',
        v_evento1_id,
        NOW() + INTERVAL '4 hours',
        NOW() + INTERVAL '5 hours',
        20,
        NOW() + INTERVAL '3 hours 45 minutes',
        NOW() + INTERVAL '4 hours 15 minutes',
        'QR-SPINNING-SESION-01',
        FALSE
    ) ON CONFLICT ("Id") DO NOTHING;

    -- Sesión 2: Mañana
    INSERT INTO "SesionesEvento" ("Id", "EventoId", "FechaHoraInicio", "FechaHoraFin", "CupoMaximo", "VentanaAsistenciaInicio", "VentanaAsistenciaFin", "TokenQR", "Consolidada")
    VALUES (
        '12000000-0000-0000-0000-000000000002',
        v_evento1_id,
        NOW() + INTERVAL '1 day 2 hours',
        NOW() + INTERVAL '1 day 3 hours',
        20,
        NOW() + INTERVAL '1 day 1 hour 45 minutes',
        NOW() + INTERVAL '1 day 2 hours 15 minutes',
        'QR-SPINNING-SESION-02',
        FALSE
    ) ON CONFLICT ("Id") DO NOTHING;

    -- Sesiones para Evento 2 (Funcional)
    INSERT INTO "SesionesEvento" ("Id", "EventoId", "FechaHoraInicio", "FechaHoraFin", "CupoMaximo", "VentanaAsistenciaInicio", "VentanaAsistenciaFin", "TokenQR", "Consolidada")
    VALUES (
        '21000000-0000-0000-0000-000000000001',
        v_evento2_id,
        NOW() + INTERVAL '6 hours',
        NOW() + INTERVAL '7 hours',
        15,
        NOW() + INTERVAL '5 hours 45 minutes',
        NOW() + INTERVAL '6 hours 15 minutes',
        'QR-FUNCIONAL-SESION-01',
        FALSE
    ) ON CONFLICT ("Id") DO NOTHING;

    -- Sesión para Evento 3 (Taller Levantamiento)
    INSERT INTO "SesionesEvento" ("Id", "EventoId", "FechaHoraInicio", "FechaHoraFin", "CupoMaximo", "VentanaAsistenciaInicio", "VentanaAsistenciaFin", "TokenQR", "Consolidada")
    VALUES (
        '31000000-0000-0000-0000-000000000001',
        v_evento3_id,
        NOW() + INTERVAL '2 days 3 hours',
        NOW() + INTERVAL '2 days 5 hours',
        10,
        NOW() + INTERVAL '2 days 2 hours 45 minutes',
        NOW() + INTERVAL '2 days 3 hours 15 minutes',
        'QR-TALLER-SESION-01',
        FALSE
    ) ON CONFLICT ("Id") DO NOTHING;

END $$;
