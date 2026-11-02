1-Estamos trabajando en el diseño de un sistema de reserva de asientos basado en microservicios. 
Ya casi finalizamos los diagramas de clases y ahora necesito analizar y diseñar los contratos de comunicacion entre los distintos coponentes.

2-Continuando con el tp practico, ahora nos enfocaremos en el microservicio AvailabilitySite, desarrollado con Clean Arquitecture.
Vamos a trabajar en el anillo 2 aplicacion de casos de uso y su implementacion en el proyecto.

3- Continuando con el tp del microservicio AvailabilitySite, ahora vamos a trabajar en el Anillo 4 de Clean Architecture. 
Necesito crear los controladores API REST para consultar los asientos disponibles, solicitar una reserva y consultar su estado. 
También vamos a configurar Swagger para poder probar los endpoints.

4- Continuando con el trabajo practico del microservicio AvailabilitySite, necesito implementar un sistema sencillo de registros de logs 
para controlar las principales operaciones del Anillo 2 de Clean Architecture.
Quiero registrar el inicio, la finalizacion y los posibles errores de los casos de uso. Ademas, necesito que estos registros se puedan visualizar en la 
consola y guardar en archivos para consultarlos posteriormente.
Implementacion sencilla

5- Ahora vamos a desarrollar una interfaz web sencilla utilizando MVC y Bootstrap.
Necesito poder seleccionar eventos, consultar asientos disponibles y mostrarlos en pantalla, tambien quiero agregar botones para solicitar reservas 
y consultar su estado, aunque por ahora quedaran deshabilitados hasta integrar la autenticacion.
Vamos a utilizar datos de prueba para verificar su funcionamiento y configurar la aplicacion para que abra directamente en la pagina de reservas.
Quiero hacerlo paso a paso, aprovechando lo que ya tenemos desarrollado.   

6- Continuando con el TP, quiero mejorar la navegacion de la interfaz web.
Vamos modificar las paginas complementarias actuales, con informacion sencilla del proyecto.
Vamos a utilizar Bootstrap y aprovechar lo que ya tenemos desarrollado, sin afectar el funcionamiento de las reservas.

7- Continuando con los logs ahora quiero implementar en el Anillo 4 de Clean Architecture.
Quiero registrar el inicio, la finalización y los posibles errores de las operaciones de la API REST, aprovechando el sistema de logs que ya tenemos.
Vamos a probar su funcionamiento desde Swagger y verificar que se guarden en los archivos de registros.

8-Continuando con el TP, ahora vamos a habitar las funcionalidades de reservas y consultas de estado en la interfaz web.
Necesito integrar la autenticación de usuarios para poder habilitar los botones de solicitud de reservas y consultas de estado.
Tambien vamos a obtener el identificador del usuario autenticado y relacionarlo con la reserva que se solicita.
Quiero habilitar los botones para solicitar y consultar el estado, aprovechando los casos de uso que ya tenemos implementados y la comunicacion con booking.
Ademas mejorar la intefzaz mostrando mensajes, quiero hacerlo paso a paso sin modificar innecesariamente lo que ya tenemos desarrollado.

9-Ahora necesito actualizar el estado de los asientos cuando el microservicio de Booking confirme la reserva.
Vamos a aprovechar el get rest que ya tenemos implementado para consultar el estado de la solicitud y actualizar la informacion de la Db.
Tambien implementar una modificacion en la interfaz para mostrar el estado recomienda accion.
Hay que respetar la arquitectura y no modificar innecesariamente lo que ya tenemos desarrollado, vamos a hacerlo paso a paso.

10-Continuando con el TP ahora vamos a implementar la actualizacion automatica de los estados de los asientos sin necesidad de refrescar la pagina.
Lo vamos a realizar con Partial Views y Ajax, aprovechando lo que ya tenemos desarrollado.
Como siempre vamos a respetar la arquitectura y no modificar innecesariamente lo que ya tenemos desarrollado.