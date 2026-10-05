# AccessTrust IAM Lite

AccessTrust IAM Lite es una aplicación académica orientada a la gestión de accesos temporales a recursos digitales. El proyecto modela un flujo de solicitud, revisión, aprobación, emisión y uso de credenciales temporales, manteniendo trazabilidad sobre las acciones realizadas.

El objetivo principal es aplicar conceptos de gestión de identidades y accesos en un entorno controlado, con énfasis en mínimo privilegio, separación de responsabilidades y auditoría.

## Flujo de acceso

```text
Solicitud de acceso
        |
        v
Revisión y aprobación
        |
        v
Emisión de credencial temporal
        |
        v
Acceso al recurso
        |
        v
Auditoría y seguimiento
```

## Funcionalidades principales

- Gestión de usuarios y roles.
- Definición de políticas de acceso.
- Solicitud y aprobación de accesos temporales.
- Emisión de credenciales con vigencia y uso limitado.
- Gestión de tickets de acceso externo.
- Registro de eventos de auditoría.
- Generación de reportes y consultas sobre la actividad del sistema.
- Validaciones de esquema e índices en MongoDB.

## Controles de seguridad implementados

El proyecto incorpora controles orientados a proteger credenciales, datos sensibles y trazabilidad:

- Hashing de contraseñas.
- Hashing de tokens y tickets antes de su almacenamiento.
- Cifrado de datos sensibles.
- Auditoría encadenada mediante hashes para detectar alteraciones en la secuencia de eventos.
- Separación de usuarios de base de datos según responsabilidad.
- Respaldos y restauración con credenciales dedicadas.
- Aplicación del principio de mínimo privilegio.

## Modelo de datos

La solución trabaja con entidades relacionadas para representar el ciclo de acceso:

- usuarios;
- roles;
- recursos;
- políticas de acceso;
- solicitudes;
- credenciales temporales;
- tickets externos;
- eventos de auditoría;
- alertas de seguridad.

## Alcance

AccessTrust IAM Lite es un MVP académico. Su propósito es demostrar conceptos y controles de IAM; no pretende reemplazar una plataforma empresarial de gestión de identidades.

La versión actual no implementa MFA real ni integraciones avanzadas con proveedores de identidad empresariales como Microsoft Entra ID, Active Directory, Okta o soluciones PAM.

## Tecnologías

- C#
- MongoDB
- Aplicación web
- Git

## Aspectos trabajados

Durante el desarrollo se abordaron problemas de autorización, trazabilidad, gestión de credenciales, validación de datos y separación de responsabilidades. También se realizaron pruebas de respaldo y restauración para validar la recuperación de la base de datos.

## Uso

Este repositorio tiene fines académicos y de demostración. Antes de utilizar una solución similar en un entorno real deben considerarse controles adicionales de autenticación, gestión de secretos, alta disponibilidad, monitoreo y cumplimiento normativo.

## Autores

Proyecto desarrollado en el contexto académico de Ingeniería de Ciberseguridad de la Universidad Nacional de Ingeniería.
