using System;
using System.Net.Http;

namespace PortalProveedores
{
    public class Config
    {
        public static string RutaPdfOrdenPago { get => @"\\localhost\PortalProveedores\PDF\OrdenPago\{" + Proveedor + "}"; }
        public static string RutaPdfOrdenCompra { get => @"\\localhost\PortalProveedores\PDF\OrdenCompra\{" + Proveedor + "}"; }
        private const string ApiUrl = "http://localhost";
        private const string ApiPort = "50000";
        public static readonly HttpClient SharedClient = new HttpClient()
        {
            BaseAddress = new Uri($"{ApiUrl}:{ApiPort}"),
        };
        public static string Proveedor;
    }
}