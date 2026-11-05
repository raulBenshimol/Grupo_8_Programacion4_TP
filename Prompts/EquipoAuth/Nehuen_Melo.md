– Qué agrega OpenID Connect sobre OAuth2? Para qué sirve el id_token si ya existe el access token?
– Resumime el flujo Authorization Code + PKCE en pocos pasos.
– Donde saco el ClientId y el ClientSecret en Google Cloud? ¿Qué pongo en la redirect URI si mi sitio corre en localhost?
– Como guardo el ClientSecret de Google con dotnet user-secrets y cómo lo leo después desde la configuración?
– En el TokenController del AuthServer agregá un grant_type=google que reciba un id_token, sin romper el client_credentials que ya funciona.
– Qué clase de .NET uso para bajar las claves públicas de Google desde el .well-known/openid-configuration?
– Hacé en el TokenService un método ValidarTokenGoogleAsync que valide issuer, audience y firma del id_token y devuelva un UsuarioGoogle con email y nombre.
– Por qué es importante validar que la audience del id_token sea mi ClientId?
– Agregá logs: uno cuando se emite el token para un usuario de Google mostrando el email y un warning cuando se rechaza. El token no se tiene que loguear.
– Sumá al AuthServer.http un caso con un id_token falso que devuelva 401.
– En el Program.cs del AvailabilitySite configurá la autenticación con cookie por defecto y OpenID Connect con Google, usando ResponseType = code y SaveTokens = true.
