# TP Grupo 8 : Sistemas distribuidos - Programación 4 2026

## 👥 Integrantes - Asignación

* **[Juan Cruz Borda]** - [Availability]
* **[Juan Carlos Enrique Quinteros]** - [Availability]
* **[Raul Benshimol]** - [Availability]
* **[Bruno Scucimarri]** - [Booking]
* **[Tobías Changazo]** - [Booking]
* **[Owen Masson]** - [Payment]
* **[Agustina Rosado]** - [Payment]
* **[Carolina Alvarez]** - [Padrón/Legajo]
* **[Nehuen Joel Melo]** - [Padrón/Legajo]

---

## 📝 Descripción


### 🛠️ Tecnologías utilizadas
* Lenguaje: .NET
* Base de datos: SQLite

---

## 🚀 Instalación y Configuración

Sigue estos pasos para ejecutar el proyecto de forma local.

### 📋 Requisitos previos
Es necesario tener instalado:


### 🔧 Pasos para la configuración
1. **Clonar el repositorio:**
   ```bash
   git clone https://github.com
   cd TU_REPOSITORIO
   ```

2. **Instalar dependencias:**
   ```bash
   #
   
   ```

3. **Variables de entorno (Si aplica):**
   Duplica el archivo de ejemplo y configura tus credenciales:
   ```bash
  
   ```

---

## 💻 Modo de Uso / Ejecución

Para iniciar la aplicación, ejecuta el siguiente comando en la terminal:

```bash
# Cambia esto por tu comando de inicio (ej: python main.py, npm start, ./main)

```

Una vez levantado el servicio, puedes acceder a:
* **Aplicación:** `http://localhost:3000`
* **Documentación de la API:** `http://localhost:3000/api-docs` (Si aplica)

### 🧪 Ejecución de Tests
Si el TP incluye pruebas automatizadas, puedes correrlas con:
```bash

```

---

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
├── .gitignore            # Archivos excluidos de Git
├── README.md             # Este archivo
```
