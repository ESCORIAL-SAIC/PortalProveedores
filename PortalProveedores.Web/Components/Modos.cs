using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PortalProveedores.Web.Components;

public static class Modos
{
    // Sin prerender: el layout aparece al instante y la página arranca una sola vez ya conectada,
    // en vez de pedirle los datos a la API dos veces (prerender + circuito) antes de mostrar algo.
    public static readonly IComponentRenderMode SinPrerender = new InteractiveServerRenderMode(prerender: false);
}
