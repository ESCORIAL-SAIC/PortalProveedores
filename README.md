# Portal de proveedores

Portal web de Escorial para proveedores (Blazor Server, .NET 10). Consume `ProveedoresApiCore`.
Va en IIS sobre Windows Server, sin contenedor.

## Desarrollo

```bash
cd PortalProveedores.Web
cp appsettings.ejemplo.json appsettings.json   # ajustar ApiUrl y las rutas de PDF
dotnet run --launch-profile http
dotnet test PortalProveedores.sln
```

`appsettings.json` no se versiona. `RutaPdfOrdenPago` y `RutaPdfOrdenCompra` apuntan al share con
las carpetas `{uuid}` de cada proveedor.

## Versionado y publicación

La versión vive en **tags anotados** (`0.1.0-rc.1`, mensaje `versionCode=N`), sin prefijo `v`.
El CI nunca commitea versiones. Cada PR compila el artefacto con su versión definitiva y, al
mergear, se publica **ese mismo archivo** (no se recompila).

| Destino de la PR | Resultado |
|---|---|
| PR a **`dev`** | pre-release `X.Y.Z-rc.N` en GitHub Releases |
| PR **`dev` → `main`** | release estable `X.Y.Z` |

Etiqueta de la PR a `dev` (sin etiqueta = `bugfix`): `bugfix` sube PATCH, `feature` sube MINOR,
`breaking` sube MAJOR.

Checks de cada PR: `test` (xUnit, un check por test) y `Compilar portal`. Solo merge commit.
Probar el cálculo de versión sin tocar GitHub: `bash .github/scripts/test-version.sh`.

## Instalar una release en IIS (Windows Server)

El artefacto es `portal-proveedores-X.Y.Z-win-x64.zip`, publicado para Windows y dependiente del
runtime.

Una sola vez en el servidor: rol IIS con **WebSocket Protocol**, **.NET 10 Hosting Bundle**, sitio
con app pool **sin código administrado**, y un `appsettings.json` propio (ver
`appsettings.ejemplo.json`) con `ApiUrl` y las rutas UNC de los PDF. El app pool debe poder leer el
share: usar una cuenta de servicio, no `IIS AppPool\...`.

Para actualizar:

1. Descargar el zip de la release en GitHub.
2. Crear `app_offline.htm` en la carpeta del sitio (IIS detiene la app sin reiniciar el servidor).
3. Descomprimir encima. El zip **no incluye `appsettings.json`**, así que no pisa la configuración (solo trae `appsettings.ejemplo.json`).
4. Borrar `app_offline.htm`.

Sin HTTPS la clave del proveedor viaja en texto plano: poner certificado en el binding del sitio.

## Despliegue automático

Al publicarse una release **estable** (PR `dev` -> `main`), el job `desplegar` de `publicar.yml` corre en un
runner propio del servidor (etiqueta `iis-portal`) y ejecuta `C:\deploy\desplegar-portal.ps1`. Ese script vive
en el servidor, no en el repo. Baja el zip de la release, lo prueba aparte, cambia el sitio de IIS sin corte y
vuelve a la versión anterior si falla. Los rc no despliegan.
La configuración (`ApiUrl`, rutas de PDF) queda en el servidor, en `C:\deploy\appsettings.portal.json`.

El runner ejecuta código con privilegios altos: exigir aprobación de workflows de colaboradores externos
(Settings > Actions > General) y no usar la etiqueta `iis-portal` en otros workflows.
