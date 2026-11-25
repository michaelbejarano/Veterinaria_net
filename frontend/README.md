# VetCare · frontend React

Una interfaz de administración veterinaria original, responsiva y conectada a la API local de .NET 10.

## Instalación en el proyecto que ya tienes

Este paquete reemplaza **solo** los archivos de la carpeta `frontend/` dentro de `C:\Veterinaria_net`. Mantén `frontend/package.json`, `frontend/package-lock.json`, `frontend/src/main.tsx`, sus dependencias instaladas y las carpetas del backend.

1. Comprueba que estás en la rama `feat/frontend-valen`.
2. Extrae el ZIP en `C:\Veterinaria_net\frontend` (permitiendo reemplazar los archivos incluidos).
3. Ejecuta la API en la raíz del repositorio:

   ```powershell
   dotnet run --project .\vet_api\vet_api.csproj --no-build -e DOTNET_ENVIRONMENT=Development -e ASPNETCORE_ENVIRONMENT=Development
   ```

4. En otra terminal, ejecuta:

   ```powershell
   cd C:\Veterinaria_net\frontend
   npm.cmd run dev
   ```

5. Navega a `http://localhost:5173` (frontend). Swagger sigue separado en `http://localhost:5265/swagger`.

## Diseño y funcionamiento

- Login con credenciales proporcionadas por tu equipo y `POST /api/auth/login`.
- El token de sesión se mantiene **solo en memoria** y se borra al actualizar o salir.
- Catálogo de módulos generado desde el OpenAPI real de `GET /swagger/v1/swagger.json`.
- Consultas GET, filtros locales, detalle de registros, paginación de los registros recibidos.
- Formularios de acción para los endpoints del backend; editor JSON opcional para tipos complejos.
- No se inventan datos: si la API no responde, la interfaz lo indica claramente.
- Para acciones DELETE se exige una confirmación manual.

## Estructura

```text
frontend/
  index.html                 Título y favicon
  public/favicon.svg         Ícono SVG propio
  vite.config.ts             Proxy local /api y /swagger → puerto 5265
  src/
    App.tsx                  Login, layout, dashboard y módulos
    App.css                  Sistema visual responsive
    index.css                Estilos globales
    components/
      Icon.tsx               Iconos SVG
      OperationModal.tsx     Formularios de acciones Swagger
    lib/
      http.ts                Cliente HTTP con JWT
      openapi.ts             Lectura de contratos Swagger
```

## Alcance y límites

Esta es una **base funcional de frontend administrativo** capaz de aprovechar la documentación Swagger para obtener pantallas de consulta y ejecutar operaciones. Todavía no sustituye diseños especializados de flujos complejos: agendas drag-and-drop, carrito de ventas, cierre de caja, reportes clínicos detallados, roles de interfaz, etc. Esos componentes requieren comparar sus contratos concretos, reglas de negocio y flujos con el backend y probarlos. Las pantallas generadas dinámicamente pueden exigir ajustar validaciones o el editor JSON.

Para producción deberán configurarse: autenticación persistente y segura, control de roles en UI, ruta de API pública, validaciones completas y pruebas end-to-end. El backend es quien debe mantener la autorización.

**Seguridad:** No incorpores contraseñas, tokens, claves JWT ni cadenas de conexión al frontend ni a GitHub. El proxy es solo para desarrollo local.

## Compromisos técnicos

- React + TypeScript + Vite
- Sin dependencias nuevas respecto a la plantilla react-ts
- Solo llamadas a la API real, sin fixtures o datos inventados
- Formatos de respuesta admitidos: arrays y envoltorios `items`, `data`, `results`, `records`, `content`, `value`

## Ajuste de Mascotas (v2)

- El formulario POST/PUT de mascotas carga especies mediante GET /api/species y razas mediante GET /api/species/{speciesId}/breeds.
- Permite sugerencias de propietarios a partir de registros de GET /api/pets, con ingreso manual del ownerId para propietarios sin mascotas.
- La ficha del registro muestra acciones de editar (PUT) y desactivar (PATCH) segun los endpoints de Swagger.
- Desactivar requiere confirmacion explicita antes de enviar la solicitud.
- No se ha identificado un GET /api/owners: se necesita verificar la relacion de propietarios con el backend antes de un selector completo.
- Las operaciones se deben probar primero con datos de desarrollo. No hay que ejecutar acciones sobre registros reales sin revisar.
