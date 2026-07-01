use("AccessTrustIAMLite");

db.usuarios.createIndex(
  { Correo: 1 },
  { unique: true, name: "idx_usuarios_correo_unique" }
);

db.usuarios.createIndex(
  { Estado: 1 },
  { name: "idx_usuarios_estado" }
);

db.roles.createIndex(
  { Nombre: 1 },
  { unique: true, name: "idx_roles_nombre_unique" }
);

db.recursos.createIndex(
  { Sensibilidad: 1 },
  { name: "idx_recursos_sensibilidad" }
);

db.recursos.createIndex(
  { Activo: 1 },
  { name: "idx_recursos_activo" }
);

db.politicas_acceso.createIndex(
  { Sensibilidad: 1 },
  { unique: true, name: "idx_politicas_sensibilidad_unique" }
);

db.solicitudes_acceso.createIndex(
  { Estado: 1, CreatedAt: -1 },
  { name: "idx_solicitudes_estado_fecha" }
);

db.solicitudes_acceso.createIndex(
  { UsuarioId: 1 },
  { name: "idx_solicitudes_usuario" }
);

db.solicitudes_acceso.createIndex(
  { RecursoId: 1 },
  { name: "idx_solicitudes_recurso" }
);

db.credenciales_temporales.createIndex(
  { UsuarioId: 1, Estado: 1 },
  { name: "idx_credenciales_usuario_estado" }
);

db.credenciales_temporales.createIndex(
  { RecursoId: 1 },
  { name: "idx_credenciales_recurso" }
);

db.credenciales_temporales.createIndex(
  { ExpiresAt: 1 },
  { name: "idx_credenciales_expiracion" }
);

db.otp_codigos.createIndex(
  { UsuarioId: 1, Accion: 1, Estado: 1 },
  { name: "idx_otp_usuario_accion_estado" }
);

db.otp_codigos.createIndex(
  { ExpiresAt: 1 },
  { name: "idx_otp_expiracion" }
);

db.eventos_auditoria.createIndex(
  { Seq: 1 },
  { unique: true, name: "idx_auditoria_seq_unique" }
);

db.eventos_auditoria.createIndex(
  { CreatedAt: -1 },
  { name: "idx_auditoria_fecha" }
);

db.eventos_auditoria.createIndex(
  { ActorUserId: 1 },
  { name: "idx_auditoria_actor" }
);

db.eventos_auditoria.createIndex(
  { Accion: 1, Resultado: 1 },
  { name: "idx_auditoria_accion_resultado" }
);

db.alertas_seguridad.createIndex(
  { Estado: 1, CreatedAt: -1 },
  { name: "idx_alertas_estado_fecha" }
);

db.alertas_seguridad.createIndex(
  { Severidad: 1 },
  { name: "idx_alertas_severidad" }
);

print("Índices básicos creados correctamente.");