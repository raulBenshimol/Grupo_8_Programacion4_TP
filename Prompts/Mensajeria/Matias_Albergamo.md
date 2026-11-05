# Historial de prompts – Matías Albergamo (Grupo 8)
Componente: Mensajería (Azure Service Bus)

### Contrato del mensaje
Availability publica FechaSolicitud, pero la clase de Booking no tiene ese campo, asi que todas las solicitudes terminan en la dead-letter queue. Simplemente sacandolo para que coincidan los contratos ya se solucionaria el problema no? La idea es arreglarlo del lado de Availability, sin tocar el codigo de Booking.

Revisa el schema del contrato (Contract/Messaging/SolicitudReserva.schema.json) para que quede igual a lo que realmente se publica.

### Auditoría
Necesito agregar logs de auditoria que permitan seguir una solicitud de punta a punta usando el CorrelationId. Que opciones hay para implementarlo?

Revisa como quedo el metodo PublicarSolicitud con el log de error y decime si esta bien.

### Diagrama
Teniendo en cuenta el codigo de AzureBroker y SolicitudReservaMensaje, hace un diagrama de mi componente (MessageBroker) en una carpeta aparte usando draw.io. Para la arquitectura y el diseño, toma como referencia lo que hicieron mis compañeros en sus componentes. No modifiques nada de lo que hicieron ellos: crea solo los elementos y relaciones de mi componente y, si se relaciona con otros servicios, las relaciones con ellos. Hace un primer boceto y lo vamos retocando si hace falta.
