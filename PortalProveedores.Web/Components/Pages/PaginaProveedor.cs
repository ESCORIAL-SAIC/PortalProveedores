using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace PortalProveedores.Web.Components.Pages;

// Reemplaza los tres Page_Init que leían el static Config.Proveedor. Ahora el proveedor
// sale del claim de la cookie, así que cada usuario ve lo suyo.
public abstract class PaginaProveedor : ComponentBase
{
    [CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;
    [Inject] protected ApiClient Api { get; set; } = default!;

    protected string Proveedor { get; private set; } = string.Empty;
    protected bool Cargando { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        Proveedor = (await AuthState).User.FindFirst("Proveedor")?.Value ?? string.Empty;
        await Recargar();
    }

    protected abstract Task Cargar();

    protected async Task Recargar()
    {
        Cargando = true;
        try { await Cargar(); }
        finally { Cargando = false; }
    }
}
