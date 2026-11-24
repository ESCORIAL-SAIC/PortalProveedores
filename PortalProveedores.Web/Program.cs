using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using PortalProveedores.Web;
using PortalProveedores.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddHttpClient<ApiClient>(c =>
    c.BaseAddress = new Uri(builder.Configuration["ApiUrl"]
        ?? throw new InvalidOperationException("Falta ApiUrl en la configuración")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
// Antiforgery va despues de authentication: el token se emite ligado al claim del
// usuario, asi que validarlo con ctx.User todavia anonimo rechaza el form de un
// usuario ya logueado (reenviar /login daba 400).
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// El login es un POST HTTP normal, no un evento Blazor: una vez abierto el circuito
// SignalR la respuesta ya se envió y no se puede escribir la cookie de sesión.
app.MapPost("/auth/login", async (
    HttpContext ctx, ApiClient api,
    [FromForm] string? user, [FromForm] string? password) =>
{
    var login = await api.Login(user ?? string.Empty, password ?? string.Empty);
    if (login?.Proveedor is not { Length: > 0 } proveedor)
        return Results.Redirect("/login?error=1");

    var identity = new ClaimsIdentity(
        [new Claim("Proveedor", proveedor), new Claim(ClaimTypes.Name, login.Nombre is { Length: > 0 } n ? n : user ?? proveedor)],
        CookieAuthenticationDefaults.AuthenticationScheme);
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
        new ClaimsPrincipal(identity));
    return Results.Redirect("/");
});

// Sin binding [FromForm] el middleware de antiforgery no valida solo: se valida acá
// para que un sitio de terceros no pueda desloguear al usuario.
app.MapPost("/auth/logout", async (HttpContext ctx, IAntiforgery antiforgery) =>
{
    try { await antiforgery.ValidateRequestAsync(ctx); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }

    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

// El ZIP se escribe directo a la respuesta: sin directorio temporal compartido y sin
// el borrado posterior que hacía que dos descargas simultáneas se pisaran.
app.MapGet("/descargar/{tipo}/{numero}", async (
    string tipo, string numero, HttpContext ctx, IConfiguration cfg) =>
{
    if (tipo is not ("ordenpago" or "ordencompra") || !Pdf.NombreValido(numero))
        return Results.BadRequest();

    var proveedor = ctx.User.FindFirst("Proveedor")?.Value;
    if (!Pdf.NombreValido(proveedor))
        return Results.Unauthorized();

    var raiz = cfg[tipo == "ordenpago" ? "RutaPdfOrdenPago" : "RutaPdfOrdenCompra"];
    if (string.IsNullOrEmpty(raiz))
        return Results.Problem("Ruta de PDFs sin configurar");

    // Las carpetas del share llevan las llaves literales: {<uuid>} (igual que Config.cs del WebForms).
    var archivos = Pdf.Buscar(Path.Combine(raiz, $"{{{proveedor}}}"), numero);
    if (archivos.Length == 0)
        return Results.Content(
            $"<!doctype html><meta charset=\"utf-8\"><title>Sin documentos</title>" +
            $"<p>No se encontraron documentos asociados a {System.Net.WebUtility.HtmlEncode(numero)}.</p>" +
            "<p><a href=\"javascript:history.back()\">Volver</a></p>",
            "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);

    ctx.Response.ContentType = "application/zip";
    ctx.Response.Headers.ContentDisposition = $"attachment; filename=Escorial-{numero}.zip";
    using (var zip = new ZipArchive(ctx.Response.BodyWriter.AsStream(), ZipArchiveMode.Create))
        foreach (var f in archivos)
            zip.CreateEntryFromFile(f, Path.GetFileName(f));

    return Results.Empty;
}).RequireAuthorization();

app.Run();
