1. Somos el equipo de autenticación del TP adjunto.
Estilo arquitectónico: microservicios en .NET tiene que ser 9
Arquitectura: AvailabilitySite (MVC, Clean Architecture), Booking.API (REST, N-Layer),
Payment.gRPC (gRPC, N-Layer) y Azure Service Bus (connection string pendiente). Los contratos están en Contract/.
Necesitamos:
* Un AuthServer propio (Web API) con POST /connect/token que emita JWT, clave privada puede ir en user secrets según lo visto en clase.
* Autenticación entre microservicios con oauth2
2. Creá un authServer Web API en NET 9 con POST "/connect/token" que acepte client credentials, valide client\_id/client\_secret desde User Secrets y devuelva un JWT firmado con una clave privada también en User Secrets. Incluí un .http que pruebe 200, 401 y 400
3. ¿Cómo generamos un par de claves con openssl en Git Bash y las guardo en una sola línea en Base64 para usar la privada en el AuthServer y la pública en los servicios que validan?
4. ¿Cómo podemos configurar Booking API para validar el JWT con la clave pública y agregar \[Authorize] al controller?Necesitamos un .http que pruebe 401 sin token y 200 con token.
5. Para Payment, necesitamos lo mismo en payment.gRPC, exigiendo token con RequireAuthorization() en MapGrpcService
6. Cuando Booking llame a Payment por gRPC, que pida su token como booking-business y lo envíe en la Metadata
7. tenemos que verificar que nuestra parte no cumple con clean architecture… hay que reorganizar para que cumpla como lo hace booking
8. hacé todos los test y confírmame si está todo ok

