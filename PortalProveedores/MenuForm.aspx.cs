using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace PortalProveedores
{
    public partial class MenuForm : Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Config.Proveedor))
                Response.Redirect("LoginForm.aspx");
        }
        protected void Page_Load(object sender, EventArgs e)
        {
            Title = "Escorial - Portal Proveedores";
            var ordenesCompraAnchor = Library.HtmlAnchor("OrdenesCompraAnchor", "Ordenes de compra pendientes", "OrdenesCompraForm.aspx");
            var cuentaCorrienteAnchor = Library.HtmlAnchor("FacturasPendientesAnchor", "Facturas pendientes", "FacturasPendientesForm.aspx");
            var ordenesPagoAnchor = Library.HtmlAnchor("OrdenesPagoAnchor", "Ordenes de pago", "OrdenesPagoForm.aspx");
            Library.AddLoadingScreen(this, new List<HtmlControl>() { ordenesCompraAnchor, cuentaCorrienteAnchor, ordenesPagoAnchor });
            form1.Controls.Add(ordenesCompraAnchor);
            form1.Controls.Add(Library.Br());
            form1.Controls.Add(cuentaCorrienteAnchor);
            form1.Controls.Add(Library.Br());
            form1.Controls.Add(ordenesPagoAnchor);
            form1.Controls.Add(Library.Br());
        }
    }
}