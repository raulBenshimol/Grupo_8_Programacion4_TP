# TP Grupo 8 : Sistemas distribuidos - Programación 4 2026

## 👥 Integrantes - Asignación

* **[Juan Cruz Borda]** - [Availability]
* **[Juan Carlos Enrique Quinteros]** - [Availability]
* **[Raul Benshimol]** - [Availability]
* **[Matias Albergamo]** - [Mensajeria]
* **[Bruno Scucimarri]** - [Booking]
* **[Tobías Changazo]** - [Booking]
* **[Owen Masson]** - [Payment]
* **[Agustina Rosado]** - [Payment]
* **[Carolina Alvarez]** - [AuthServer]
* **[Nehuen Joel Melo]** - [AuthServer]

---

## 📝 Respuestas
### 1) Interpretación del diagrama, justificacion
   
* **Componente: Availability**

**Rol:** Permitir la interacción del usuario para consultar los asientos disponibles.

**Justificación de su participación:** La consigna exige que el usuario tenga una interfaz que le permita consultar la disponibilidad de los asientos del evento. Es requerido para comenzar el proceso de reserva del asiento, enviando la solucitud a booking a traves del message broker y para solicitar cualquier cambio de estado de los asientos.

* **Componente: Booking**

**Rol:** Permite al usuario autenticado reservar asientos.

**Justificación de su participación:** Si el usuario desea efectuar la reserva, booking comienza el proceso de retención del asiento solicitado hasta que sea confirmado o rechazado, para evitar que otro usuario pueda solicitar el mismo asiento.

* **Componente: Payment gRPC**

**Rol:** Recibir solicitud de pago, responder aprobado o desaprobado.

**Justificación de su participación:** La consigna exige que el pago de la reserva sea solicitado y aprobado para poder confirmar el asiento como ocupado. En el cado de rechazarlo, debe devolver esa respuesta correspondiente a booking.

* **Componente: AuthServer (servidor de autorización)**

**Rol:** Autenticación y emisión de tokens (Authorization Server de OAuth 2.0)	

**Justificación de su participación:** La consigna exige que el usuario obtenga un token para reservar y que los microservicios se autentiquen entre sí "con el servidor de autorización". Es el único que emite y firma tokens (RS256); así los demás servicios no guardan contraseñas ni deciden identidades, solo verifican la firma.

* **Componente: Message Broker**

**Rol:** Intermediario de mensajería asincrónica entre Availability y Booking

**Justificación de su participación:** Permite que Availability responda sin esperar a Booking y que las réplicas de Booking se repartan las solicitudes, sin que se pierdan si Booking no está disponible.

* **Componente: Notifications**

**Rol:** Notificar al usuario el estado del avance de su reserva

**Justificación de su participación:** Dar visibilidad al usuario del avance del proceso completo del estado de la reserva, estado de pago, confirmación final de reserva exitosa o rechazada.

3. **Configurar los secrets (obligatorio antes de levantar el proyecto):**
   Las claves (clave privada del JWT, connection string de Azure Service Bus, client secret de Google y secretos de cliente) no están en el repositorio: se cargan como User Secrets de .NET en AuthServer, AvailabilitySite y Booking.

   1. Colocar el archivo `secrets.json` en la raíz de la solución, al lado de `Programacion4_TP.slnx` y `configurar-secrets.ps1`. Su formato es el de `secrets.example.json`.
   2. Desde esa carpeta, correr el script en PowerShell:
      ```powershell
      .\configurar-secrets.ps1
      ```


### Señalar en el diagrama donde el sistema se vuelve eventualmente consistente.
El sistema se vuelve eventualmente consistente en la comunicacióon entre availability y booking, ya que debe garantizar la **disponibilidad** del asiento solicitado por el usuario, aunque el mismo termine rechazado y liberado.

### 🛠️ Tecnologías utilizadas
* Lenguaje: .NET
* Base de datos: SQLite

---

## 💻 Modo de Uso / Ejecución

Para iniciar la aplicación, ejecuta el siguiente comando en la terminal:

```bash
# Cambia esto por tu comando de inicio (ej: python main.py, npm start, ./main)

```
Una vez levantado el servicio, puedes acceder a:
* **Aplicación:** `http://localhost:3000`
* **Documentación de la API:** `http://localhost:3000/api-docs` (Si aplica)

## 📁 Estructura del Repositorio
```text

├── AuthServer/                    # Proyecto de Auth 
│   ├── controllers/               # Controladores 
│   ├── models/                    # Modelos 
│   ├── services/                  # Servicios 
│   └── Program.cs        
├── Availability.Application/      # Proyecto Availability App
│   ├── Interfaces/      
│   └── UseCases/        
├── Availability.controllers/      # Proyecto de Consoladores de Availability
│   ├── controllers/               # Controladores 
│   ├── DTO/                       # DTO 
│   └── Presenter/                  
├── Availability.Domain/           # Proyecto Availability Dominio
│   ├── Entities/      
│   └── Enum/      
├── Availability.Infrastructure/   # Proyecto de Availability Infra 
│   ├── externalServices/          #  
│   ├── messaging/                 #  
│   ├── migrations/                #  
│   └── persistence/  
├── Availability.Site/             # Proyecto de Availability Site 
│   ├── Auth/                      #  
│   ├── controllers/               # Controladores 
│   ├── models/                    # Modelos 
│   ├── properties/                #
│   ├── views/                     #  
│   ├── wwwroot/                   #
│   └── Program.cs
├── Booking/                       # Proyecto de Booking
│   ├── API/                       #
│   │   └── Program.cs    
│   ├── business/                  #  
│   ├── data/                      #  
│   ├── entities/                  #
│   └── infra/                     #  
├── Payment.gRPC/                       # Proyecto de Payment
│   ├── business/                  #  
│   ├── data/                      #  
│   ├── models/                    #
│   ├── services/                  #
│   └── Program.cs                 #  
│       
├── configurar-secrets.ps1 # Carga los User Secrets de todos los proyectos
├── secrets.example.json   # Plantilla de secrets.json (sin valores)
├── .gitignore            # Archivos excluidos de Git
├── README.md             # Este archivo
```
