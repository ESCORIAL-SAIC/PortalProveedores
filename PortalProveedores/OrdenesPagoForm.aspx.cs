using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.Expressions;

namespace PortalProveedores
{
    public class OrdenPago
    {
        public string NumeroPago { get; set; }
        public string Fecha { get; set; }
        public string Proveedor { get; set; }
        public string Cuit { get; set; }
        public string NombreDocumento { get; set; }
        public string Factura { get; set; }
        public string Importe { get; set; }
        public string Total { get; set; }
        public string Moneda { get; set; }
        public string Cotizacion { get; set; }
    }
    public partial class OrdenesPagoForm : Page
    {
        private string ValorBusqueda
        {
            get { return ViewState["ValorBusqueda"] as string ?? string.Empty; }
            set { ViewState["ValorBusqueda"] = value; }
        }
        public List<OrdenPago> OrdenesPago = new List<OrdenPago>();
        protected void Page_Init(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Config.Proveedor))
                Response.Redirect("LoginForm.aspx");
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            Title = "Portal Proveedores - Ordenes de Pago";
            var buscarLabel = Library.Label("BuscarLabel", "Buscar: ");
            form1.Controls.Add(buscarLabel);
            var buscarTextBox = Library.TextBox("BuscarTextBox");
            form1.Controls.Add(buscarTextBox); 
            var buscarButton = Library.Button("BuscarButton", "Buscar");
            buscarButton.ServerClick += BuscarButton_ServerClick;
            Library.AddLoadingScreen(this, new List<HtmlControl>() { buscarButton });
            form1.Controls.Add(buscarButton);
            var limpiarFiltrosButton = Library.Button("LimpiarFiltrosButton", "Limpiar filtros");
            limpiarFiltrosButton.ServerClick += LimpiarFiltrosButton_ServerClick;
            form1.Controls.Add(limpiarFiltrosButton);
            form1.Controls.Add(Library.Br());
            var ordenesPagoGridView = Library.GridView("OrdenesPagoGridView", false);
            ordenesPagoGridView.RowCommand += OrdenesPagoGridView_RowCommand;
            var fechaColumn = Library.BoundField(nameof(OrdenPago.Fecha), "Fecha");
            ordenesPagoGridView.Columns.Add(fechaColumn);
            var proveedorColumn = Library.BoundField(nameof(OrdenPago.Proveedor), "Proveedor");
            ordenesPagoGridView.Columns.Add(proveedorColumn);
            var cuitColumn = Library.BoundField(nameof(OrdenPago.Cuit), "CUIT");
            ordenesPagoGridView.Columns.Add(cuitColumn);
            var nombreDocumentoColumn = Library.BoundField(nameof(OrdenPago.NombreDocumento), "Orden de Pago");
            ordenesPagoGridView.Columns.Add(nombreDocumentoColumn);
            var facturaColumn = Library.BoundField(nameof(OrdenPago.Factura), "Factura");
            ordenesPagoGridView.Columns.Add(facturaColumn);
            var importeColumn = Library.BoundField(nameof(OrdenPago.Importe), "Importe");
            ordenesPagoGridView.Columns.Add(importeColumn);
            var totalColumn = Library.BoundField(nameof(OrdenPago.Total), "Total");
            ordenesPagoGridView.Columns.Add(totalColumn);
            var monedaColumn = Library.BoundField(nameof(OrdenPago.Moneda), "Moneda");
            ordenesPagoGridView.Columns.Add(monedaColumn);
            var cotizacionColumn = Library.BoundField(nameof(OrdenPago.Cotizacion), "Cotizacion");
            ordenesPagoGridView.Columns.Add(cotizacionColumn);
            var descargarOrdenPagoButton = Library.ButtonField(ButtonType.Link, "D", "DescargarOrdenPago");
            ordenesPagoGridView.Columns.Add(descargarOrdenPagoButton);
            form1.Controls.Add(ordenesPagoGridView);
            if (string.IsNullOrEmpty(ValorBusqueda))
                OrdenesPago = await GetOrdenesPago("Todos", string.Empty);
            else
                OrdenesPago = await GetOrdenesPago("Busqueda", ValorBusqueda);
            ordenesPagoGridView.DataSource = OrdenesPago;
            ordenesPagoGridView.DataBind();
        }

        private void LimpiarFiltrosButton_ServerClick(object sender, EventArgs e)
        {
            var buscarTextBox = (TextBox)form1.FindControl("BuscarTextBox");
            buscarTextBox.Text = string.Empty;
            ValorBusqueda = string.Empty;
            Library.DoPostback(this);
        }

        private async void BuscarButton_ServerClick(object sender, EventArgs e)
        {
            var buscarTextBox = (TextBox)form1.FindControl("BuscarTextBox");
            if (buscarTextBox is null)
                return;
            var ordenesPagoGridView = (GridView)form1.FindControl("OrdenesPagoGridView");
            if (ordenesPagoGridView is null)
                return;
            ValorBusqueda = buscarTextBox.Text;
            OrdenesPago = await GetOrdenesPago("Busqueda", ValorBusqueda);
            ordenesPagoGridView.DataSource = OrdenesPago;
            ordenesPagoGridView.DataBind();
        }
        protected void OrdenesPagoGridView_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName.Equals("DescargarOrdenPago"))
            {
                var filaSeleccionada = Convert.ToInt32(e.CommandArgument);
                var ordenesPagoGridView = (GridView)form1.FindControl("OrdenesPagoGridView");
                if (ordenesPagoGridView is null)
                    return;
                var nombreOrdenPago = ordenesPagoGridView.Rows[filaSeleccionada].Cells[3].Text;
                if (nombreOrdenPago is null)
                    return;
                var numeroOrdenPago = OrdenesPago.FirstOrDefault(x => x.NombreDocumento.Equals(nombreOrdenPago)).NumeroPago;
                if (numeroOrdenPago is null)
                    return;
                var ordenesPago = Directory.GetFiles(Config.RutaPdfOrdenPago).ToList();
                var pdfs = ordenesPago.Where(x => x.Contains(numeroOrdenPago));
                if (!pdfs.Any())
                {
                    Library.Alert(this, $"No se encontraron documentos asociados a la transacción {nombreOrdenPago}");
                    return;
                }
                var nombreZip = $"Escorial-Pago{numeroOrdenPago}.zip";
                var tempZipFilePath = Library.GenerateZipFile(pdfs.ToList(), nombreZip);
                Response.Clear();
                Response.ContentType = "application/zip";
                Response.AddHeader("Content-Disposition", $"attachment; filename={nombreZip}");
                Response.WriteFile(tempZipFilePath);
                Response.Flush();
                File.Delete(tempZipFilePath);
                Directory.Delete(Path.GetDirectoryName(tempZipFilePath));
                Response.End();
            }
        }
        private async Task<List<OrdenPago>> GetOrdenesPago(string tipo, string valorBusqueda)
        {
            var responseData = string.Empty;
            var api = string.Empty;
            switch (tipo)
            {
                case "Todos":
                    api = $"api/portalproveedores/ordenpago/lista/proveedor/{Config.Proveedor}";
                    break;
                case "Busqueda":
                    if (string.IsNullOrEmpty(valorBusqueda))
                        return null;
                    api = $"api/portalproveedores/ordenpago/lista/proveedor/{Config.Proveedor}/buscar/{valorBusqueda}";
                    break;
                default:
                    break;
            }
            var response = await Config.SharedClient.GetAsync(api);
            if (response.IsSuccessStatusCode)
                responseData = await response.Content.ReadAsStringAsync();
            var dataList = JsonConvert.DeserializeObject<List<OrdenPago>>(responseData);
            return dataList;
        }
    }
}