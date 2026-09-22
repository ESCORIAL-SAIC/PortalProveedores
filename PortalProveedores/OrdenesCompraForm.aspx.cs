using System;
using System.Web.UI.WebControls;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Web.UI;
using System.IO;
using System.Linq;

namespace PortalProveedores
{
    public class OrdenCompra
    {
        public string Id { get; set; }
        public string NumeroDocumento { get; set; }
        public string Transaccion { get; set; }
        public string NombreProveedor { get; set; }
        public string FechaTransaccion { get; set; }
    }
    public class ItemOrdenCompra
    {
        public string Id { get; set; }
        public string CodigoProducto { get; set; }
        public string DescripcionProducto { get; set; }
        public string CantidadPendiente { get; set; }
        public string FechaEstimada { get; set; }
    }
    public partial class OrdenesCompraForm : Page
    {
        private string ValorBusqueda
        {
            get { return ViewState["ValorBusqueda"] as string ?? string.Empty; }
            set { ViewState["ValorBusqueda"] = value; }
        }
        private List<OrdenCompra> OrdenesCompra = new List<OrdenCompra>();
        protected void Page_Init(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Config.Proveedor))
                Response.Redirect("LoginForm.aspx");
        }
        protected async void Page_Load(object sender, EventArgs e)
        {
            Title = "Portal Proveedores - Ordenes de Compra";
            var buscarLabel = Library.Label("BuscarLabel", "Buscar: ");
            form1.Controls.Add(buscarLabel);
            var buscarTextBox = Library.TextBox("BuscarTextBox");
            form1.Controls.Add(buscarTextBox);
            var buscarButton = Library.Button("BuscarButton", "Buscar");
            buscarButton.ServerClick += BuscarButton_ServerClick;
            form1.Controls.Add(buscarButton);
            var limpiarFiltrosButton = Library.Button("LimpiarFiltrosButton", "Limpiar filtros");
            limpiarFiltrosButton.ServerClick += LimpiarFiltrosButton_ServerClick;
            form1.Controls.Add(limpiarFiltrosButton);
            form1.Controls.Add(Library.Br());
            var ordenCompraPendienteGridView = Library.GridView("OrdenesCompraGridView", false);
            ordenCompraPendienteGridView.RowCommand += OrdenCompraPendiente_RowCommand;
            ordenCompraPendienteGridView.RowDataBound += OrdenCompraPendiente_RowDataBound;
            var descargarOrdenCompraColumn = Library.ButtonField(ButtonType.Link, "D", "DescargarOrdenCompra");
            ordenCompraPendienteGridView.Columns.Add(descargarOrdenCompraColumn);
            var transaccionColumn = Library.BoundField(nameof(OrdenCompra.Transaccion), "Orden Compra");
            ordenCompraPendienteGridView.Columns.Add(transaccionColumn);
            var proveedorColumn = Library.BoundField(nameof(OrdenCompra.NombreProveedor), "Proveedor");
            ordenCompraPendienteGridView.Columns.Add(proveedorColumn);
            var fechaTransaccionColumn = Library.BoundField(nameof(OrdenCompra.FechaTransaccion), "Fecha");
            ordenCompraPendienteGridView.Columns.Add(fechaTransaccionColumn);
            form1.Controls.Add(ordenCompraPendienteGridView);
            if (string.IsNullOrEmpty(ValorBusqueda))
                OrdenesCompra = await GetOrdenesCompra("Todos", string.Empty);
            else
                OrdenesCompra = await GetOrdenesCompra("Busqueda", ValorBusqueda);
            ordenCompraPendienteGridView.DataSource = OrdenesCompra;
            ordenCompraPendienteGridView.DataBind();
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
            if (buscarTextBox is null || string.IsNullOrEmpty(buscarTextBox.Text))
                return;
            var ordenCompraPendienteGridView = (GridView)form1.FindControl("OrdenesCompraGridView");
            if (ordenCompraPendienteGridView is null)
                return;
            ValorBusqueda = buscarTextBox.Text;
            OrdenesCompra = await GetOrdenesCompra("Busqueda", ValorBusqueda);
            ordenCompraPendienteGridView.DataSource = OrdenesCompra;
            ordenCompraPendienteGridView.DataBind();
        }
        protected async void OrdenCompraPendiente_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var fila = e.Row;
                var itemOrdenCompraGridView = Library.GridView("ItemsGridView", false);
                var codigoProductoColumn = Library.BoundField(nameof(ItemOrdenCompra.CodigoProducto), "Codigo Producto");
                itemOrdenCompraGridView.Columns.Add(codigoProductoColumn);
                var descripcionProductoColumn = Library.BoundField(nameof(ItemOrdenCompra.DescripcionProducto), "Descripcion Producto");
                itemOrdenCompraGridView.Columns.Add(descripcionProductoColumn);
                var cantidadPendienteColumn = Library.BoundField(nameof(ItemOrdenCompra.CantidadPendiente), "Cantidad Pendiente");
                itemOrdenCompraGridView.Columns.Add(cantidadPendienteColumn);
                var fechaEstimadaColumn = Library.BoundField(nameof(ItemOrdenCompra.FechaEstimada), "Fecha Estimada");
                itemOrdenCompraGridView.Columns.Add(fechaEstimadaColumn);
                var idTransaccion = DataBinder.Eval(e.Row.DataItem, nameof(OrdenCompra.Id)).ToString();
                var items = await GetItemOrdenCompra(idTransaccion);
                itemOrdenCompraGridView.DataSource = items;
                itemOrdenCompraGridView.DataBind();
                var cell = new TableCell { ColumnSpan = 4 };
                cell.Controls.Add(itemOrdenCompraGridView);
                fila.Cells.Add(cell);
            }
        }
        private void OrdenCompraPendiente_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName.Equals("DescargarOrdenCompra"))
            {
                var filaSeleccionada = Convert.ToInt32(e.CommandArgument);
                var ordenesCompraGridView = (GridView)form1.FindControl("OrdenesCompraGridView");
                if (ordenesCompraGridView is null)
                    return;
                var nombreOrdenCompra = ordenesCompraGridView.Rows[filaSeleccionada].Cells[1].Text;
                if (nombreOrdenCompra is null)
                    return;
                var numeroOrdenCompra = OrdenesCompra.FirstOrDefault(x => x.Transaccion.Equals(nombreOrdenCompra))?.NumeroDocumento;
                if (numeroOrdenCompra is null)
                    return;
                var ordenesCompra = Directory.GetFiles(Config.RutaPdfOrdenCompra).ToList();
                var pdfs = ordenesCompra.Where(x => x.Contains(numeroOrdenCompra));
                if (!pdfs.Any())
                {
                    Library.Alert(this, $"No se encontraron documentos asociados a la transacción {nombreOrdenCompra}");
                    return;
                }
                var nombreZip = $"Escorial-Pago{numeroOrdenCompra}.zip";
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
        private async Task<List<OrdenCompra>> GetOrdenesCompra(string tipo, string valorBusqueda)
        {
            var responseData = string.Empty;
            var api = string.Empty;
            switch (tipo)
            {
                case "Todos":
                    api = $"api/portalproveedores/ordencompra/lista/proveedor/{Config.Proveedor}";
                    break;
                case "Busqueda":
                    if (string.IsNullOrEmpty(valorBusqueda))
                        return null;
                    api = $"api/portalproveedores/ordencompra/lista/proveedor/{Config.Proveedor}/buscar/{valorBusqueda}";
                    break;
                default:
                    break;
            }
            var response = await Config.SharedClient.GetAsync(api);
            if (response.IsSuccessStatusCode)
                responseData = await response.Content.ReadAsStringAsync();
            var dataList = JsonConvert.DeserializeObject<List<OrdenCompra>>(responseData);
            return dataList;
        }
        private async Task<List<ItemOrdenCompra>> GetItemOrdenCompra(string idTransaccion)
        {
            var responseData = string.Empty;
            var response = await Config.SharedClient.GetAsync($"api/portalproveedores/transaccion/ordencompra/item/lista/proveedor/{idTransaccion}");
            if (response.IsSuccessStatusCode)
                responseData = await response.Content.ReadAsStringAsync();
            var dataList = JsonConvert.DeserializeObject<List<ItemOrdenCompra>>(responseData);
            return dataList;
        }
    }
}