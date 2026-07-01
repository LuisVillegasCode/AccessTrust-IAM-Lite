use("AccessTrustIAMLite");

function aplicarValidacion(nombreColeccion, validator) {
  const existe = db.getCollectionNames().includes(nombreColeccion);

  if (!existe) {
    db.createCollection(nombreColeccion, {
      validator: validator,
      validationLevel: "strict",
      validationAction: "error"
    });
    print(`Colección creada con validación: ${nombreColeccion}`);
  } else {
    db.runCommand({
      collMod: nombreColeccion,
      validator: validator,
      validationLevel: "strict",
      validationAction: "error"
    });
    print(`Validación actualizada: ${nombreColeccion}`);
  }
}

aplicarValidacion("usuarios", {
  $jsonSchema: {
    bsonType: "object",
    required: ["Nombre", "Correo", "PasswordHash", "Roles", "Estado", "FailedLoginCount", "CreatedAt"],
    properties: {
      Nombre: { bsonType: "string" },
      Correo: { bsonType: "string" },
      PasswordHash: { bsonType: "string" },
      Roles: {
        bsonType: "array",
        items: { bsonType: "string" }
      },
      Estado: {
        enum: ["Activo", "Inactivo", "Bloqueado"]
      },
      FailedLoginCount: {
        bsonType: "int",
        minimum: 0
      },
      LockedUntil: {
        bsonType: ["date", "null"]
      },
      CreatedAt: {
        bsonType: "date"
      }
    }
  }
});

aplicarValidacion("roles", {
  $jsonSchema: {
    bsonType: "object",
    required: ["Nombre", "Permisos", "Descripcion"],
    properties: {
      Nombre: { bsonType: "string" },
      Permisos: {
        bsonType: "array",
        items: { bsonType: "string" }
      },
      Descripcion: { bsonType: "string" }
    }
  }
});

aplicarValidacion("recursos", {
  $jsonSchema: {
    bsonType: "object",
    required: ["Nombre", "Tipo", "Sensibilidad", "Activo", "CreatedAt"],
    properties: {
      Nombre: { bsonType: "string" },
      Tipo: { bsonType: "string" },
      Sensibilidad: {
        enum: ["Baja", "Media", "Alta"]
      },
      Activo: { bsonType: "bool" },
      ResponsableId: {
        bsonType: ["objectId", "null"]
      },
      PoliticaId: {
        bsonType: ["objectId", "null"]
      },
      CreatedAt: {
        bsonType: "date"
      }
    }
  }
});

aplicarValidacion("politicas_acceso", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "Nombre",
      "Sensibilidad",
      "DuracionMaxMin",
      "RequiereOtp",
      "MaxUsos",
      "RequiereAprobacion",
      "IntentosOtpMax"
    ],
    properties: {
      Nombre: { bsonType: "string" },
      Sensibilidad: {
        enum: ["Baja", "Media", "Alta"]
      },
      DuracionMaxMin: {
        bsonType: "int",
        minimum: 1
      },
      RequiereOtp: { bsonType: "bool" },
      MaxUsos: {
        bsonType: "int",
        minimum: 1
      },
      RequiereAprobacion: { bsonType: "bool" },
      IntentosOtpMax: {
        bsonType: "int",
        minimum: 1
      }
    }
  }
});

aplicarValidacion("solicitudes_acceso", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "UsuarioId",
      "RecursoId",
      "Motivo",
      "DuracionSolicitadaMin",
      "Prioridad",
      "Estado",
      "CreatedAt"
    ],
    properties: {
      UsuarioId: { bsonType: "objectId" },
      RecursoId: { bsonType: "objectId" },
      Motivo: { bsonType: "string" },
      DuracionSolicitadaMin: {
        bsonType: "int",
        minimum: 1
      },
      Prioridad: { bsonType: "string" },
      Estado: {
        enum: ["Pendiente", "Aprobada", "Rechazada"]
      },
      AprobadorId: {
        bsonType: ["objectId", "null"]
      },
      Observacion: {
        bsonType: ["string", "null"]
      },
      CreatedAt: {
        bsonType: "date"
      },
      ResolvedAt: {
        bsonType: ["date", "null"]
      }
    }
  }
});

aplicarValidacion("credenciales_temporales", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "UsuarioId",
      "RecursoId",
      "SolicitudId",
      "TokenHash",
      "Estado",
      "IssuedAt",
      "ExpiresAt",
      "MaxUsos",
      "UsosRealizados"
    ],
    properties: {
      UsuarioId: { bsonType: "objectId" },
      RecursoId: { bsonType: "objectId" },
      SolicitudId: { bsonType: "objectId" },
      TokenHash: { bsonType: "string" },
      Estado: {
        enum: ["Activa", "Expirada", "Revocada", "Usada"]
      },
      IssuedAt: {
        bsonType: "date"
      },
      ExpiresAt: {
        bsonType: "date"
      },
      MaxUsos: {
        bsonType: "int",
        minimum: 1
      },
      UsosRealizados: {
        bsonType: "int",
        minimum: 0
      },
      RevokedAt: {
        bsonType: ["date", "null"]
      }
    }
  }
});

aplicarValidacion("otp_codigos", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "UsuarioId",
      "Accion",
      "CodigoHash",
      "ExpiresAt",
      "Intentos",
      "Estado",
      "CreatedAt"
    ],
    properties: {
      UsuarioId: { bsonType: "objectId" },
      Accion: { bsonType: "string" },
      CodigoHash: { bsonType: "string" },
      ExpiresAt: {
        bsonType: "date"
      },
      Intentos: {
        bsonType: "int",
        minimum: 0
      },
      Estado: {
        enum: ["Pendiente", "Usado", "Expirado", "Bloqueado"]
      },
      CreatedAt: {
        bsonType: "date"
      }
    }
  }
});

aplicarValidacion("eventos_auditoria", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "Seq",
      "Accion",
      "EntidadTipo",
      "Resultado",
      "Detalle",
      "PrevHash",
      "Hash",
      "CreatedAt"
    ],
    properties: {
      Seq: {
        bsonType: ["int", "long"],
        minimum: 1
      },
      ActorUserId: {
        bsonType: ["objectId", "null"]
      },
      Accion: { bsonType: "string" },
      EntidadTipo: { bsonType: "string" },
      EntidadId: {
        bsonType: ["objectId", "null"]
      },
      Resultado: {
        enum: ["Exitoso", "Fallido", "Permitido", "Denegado"]
      },
      Detalle: {
        bsonType: "object"
      },
      PrevHash: {
        bsonType: "string"
      },
      Hash: {
        bsonType: "string"
      },
      CreatedAt: {
        bsonType: "date"
      }
    }
  }
});

aplicarValidacion("alertas_seguridad", {
  $jsonSchema: {
    bsonType: "object",
    required: [
      "Tipo",
      "Severidad",
      "Descripcion",
      "Estado",
      "CreatedAt"
    ],
    properties: {
      Tipo: { bsonType: "string" },
      Severidad: {
        enum: ["Baja", "Media", "Alta", "Critica"]
      },
      UsuarioId: {
        bsonType: ["objectId", "null"]
      },
      RecursoId: {
        bsonType: ["objectId", "null"]
      },
      Descripcion: { bsonType: "string" },
      Estado: {
        enum: ["Abierta", "Revisada", "Cerrada"]
      },
      CreatedAt: {
        bsonType: "date"
      },
      ReviewedAt: {
        bsonType: ["date", "null"]
      }
    }
  }
});

print("Validaciones JSON Schema aplicadas correctamente.");