using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PortalProveedores
{
    public class FacturaPendiente
    {
        public string FechaFactura { get; set; }
        public string NumeroFactura { get; set; }
        public string TipoTransaccion { get; set; }
        public string Proveedor { get; set; }
        public string Moneda { get; set; }
        public string Total { get; set; }
        public string Saldo { get; set; }
        public string FechaVencimiento { get; set; }
        public string SaldoConvertido { get; set; }
        public string Cotizacion { get; set; }
        public string FechaActualizacion { get; set; }
    }
    public partial class FacturasPendientesForm : System.Web.UI.Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Config.Proveedor))
                Response.Redirect("LoginForm.aspx");
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            Title = "Portal Proveedores - Facturas Pendientes";
            var facturasPendientesGridView = Library.GridView("FacturasPendientesGridView", false);
            var fechaFacturaColumn = Library.BoundField(nameof(FacturaPendiente.FechaFactura), "Fecha");
            facturasPendientesGridView.Columns.Add(fechaFacturaColumn);
            var numeroFacturaColumn = Library.BoundField(nameof(FacturaPendiente.NumeroFactura), "Numero Factura");
            facturasPendientesGridView.Columns.Add(numeroFacturaColumn);
            var tipoTransaccionColumn = Library.BoundField(nameof(FacturaPendiente.TipoTransaccion), "Tipo Transaccion");
            facturasPendientesGridView.Columns.Add(tipoTransaccionColumn);
            var proveedorColumn = Library.BoundField(nameof(FacturaPendiente.Proveedor), "Proveedor");
            facturasPendientesGridView.Columns.Add(proveedorColumn);
            var monedaColumn = Library.BoundField(nameof(FacturaPendiente.Moneda), "Moneda");
            facturasPendientesGridView.Columns.Add(monedaColumn);
            var totalColumn = Library.BoundField(nameof(FacturaPendiente.Total), "Total");
            facturasPendientesGridView.Columns.Add(totalColumn);
            var saldoColumn = Library.BoundField(nameof(FacturaPendiente.Saldo), "Saldo");
            facturasPendientesGridView.Columns.Add(saldoColumn);
            var fechaVencimientoColumn = Library.BoundField(nameof(FacturaPendiente.FechaVencimiento), "Fecha Vencimiento");
            facturasPendientesGridView.Columns.Add(fechaVencimientoColumn);
            var saldoConvertidoColumn = Library.BoundField(nameof(FacturaPendiente.SaldoConvertido), "Saldo Convertido");
            facturasPendientesGridView.Columns.Add(saldoConvertidoColumn);
            var cotizacionColumn = Library.BoundField(nameof(FacturaPendiente.Cotizacion), "Cotizacion");
            facturasPendientesGridView.Columns.Add(cotizacionColumn);
            var fechaActualizacion = Library.BoundField(nameof(FacturaPendiente.FechaActualizacion), "Fecha Actualizacion");
            facturasPendientesGridView.Columns.Add(fechaActualizacion);
            form1.Controls.Add(facturasPendientesGridView);
            var listaFacturasPendientes = await GetFacturasPendientes();
            facturasPendientesGridView.DataSource = listaFacturasPendientes;
            facturasPendientesGridView.DataBind();
        }
        private async Task<List<FacturaPendiente>> GetFacturasPendientes()
        {
            var responseData = string.Empty;
            var response = await Config.SharedClient.GetAsync($"api/portalproveedores/facturapendiente/proveedor/{Config.Proveedor}");
            if (response.IsSuccessStatusCode)
                responseData = await response.Content.ReadAsStringAsync();
            var dataList = JsonConvert.DeserializeObject<List<FacturaPendiente>>(responseData);
            return dataList;
        }
    }
}