# Plan de testing unitario

## Objetivo y alcance

Las pruebas verifican la lógica de negocio de Payment, Booking, AuthServer y Availability con xUnit y Moq. Se prueban las clases de negocio y los casos de uso sin ejecutar la aplicación completa ni acceder a bases de datos.

Las dependencias externas se sustituyen por mocks o configuración en memoria: repositorios, pasarela de pago, cliente de pago, broker y output port. Los `ILogger<T>` se sustituyen por dobles simples. Payment utiliza su logger de auditoría real apuntando a una carpeta temporal única, que se elimina al terminar cada test; no se usa una base de datos.

## Patrón AAA

Cada test está organizado en tres fases, identificadas por comentarios en el código:

- **Arrange:** preparar datos, mocks, respuestas y el objeto probado (SUT).
- **Act:** invocar una única operación del SUT.
- **Assert:** comprobar el resultado y, cuando corresponde, verificar llamadas e interacciones mediante Moq.

En los tests de excepciones, la llamada se realiza en Act y la excepción capturada se verifica en Assert, junto con las interacciones esperadas.

## Suites y escenarios cubiertos

### AuthServer — `TokenServiceTests`

- Valida credenciales correctas.
- Rechaza secreto incorrecto, cliente inexistente y secreto vacío (Theory).
- Comprueba que el JWT contiene `sub` y el claim adicional `email`, y que informa la expiración configurada.
- Comprueba que se puede crear un token sin claims adicionales.
- La configuración de JWT y la clave RSA se generan para la prueba; no se utilizan secretos del entorno.
- La validación contra Google no se incluye en esta suite unitaria porque su configuración de claves requiere una consulta externa.

### Availability — `AvailabilityUseCasesTests`

- Devuelve solo los asientos disponibles y devuelve todos los asientos del evento.
- Obtiene una solicitud existente o `null` si no existe.
- Comprueba que una nueva solicitud se guarda, se publica y se presenta con los datos esperados.
- Comprueba que un fallo al guardar se propaga y evita publicar o presentar la solicitud.
- Verifica la traducción de estados de Booking a estados de solicitud y asiento.
- Verifica la traducción de los motivos de rechazo.
- Comprueba que estados desconocidos, estados vacíos e identificadores vacíos no provocan sincronización.

### Booking — `BookingServiceTests`

- Obtiene el estado de una reserva existente o devuelve `null` si no existe.
- Procesa una reserva nueva y confirma su estado tras el pago aprobado.
- Devuelve la reserva existente en mensajes duplicados sin volver a cobrar.
- Rechaza la reserva ante un pago fallido o no disponible y asigna el motivo correspondiente.
- Devuelve las reservas vencidas.
- Mantiene la reserva retenida y omite el cobro cuando la configuración lo indica.

### Payment — `PaymentBusinessTests`

- Devuelve el resultado de un pago aprobado.
- Devuelve y persiste un pago rechazado.
- Aplica idempotencia si ya existe un pago y evita llamar de nuevo a la pasarela.
- Comprueba el comportamiento de rechazo simulado.
- Verifica los datos del pago enviado al repositorio.

## Ejecución

Desde la raíz de la solución:

```powershell
dotnet test UnitTests/UnitTests.csproj
```

Para compilar la solución completa:

```powershell
dotnet build
```
