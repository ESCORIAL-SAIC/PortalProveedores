using System.Web.UI.WebControls;
using System.Web.UI;
using Ionic.Zip;
using System.Collections.Generic;
using System.IO;
using System.Web.UI.HtmlControls;

namespace PortalProveedores
{
    public static class Library
    {
        public static string GenerateZipFile(List<string> archivos, string nombreZip)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "ArchivosTemporales");
            Directory.CreateDirectory(tempDir);
            var tempZipFilePath = Path.Combine(tempDir, nombreZip);
            using (var zip = new ZipFile())
            {
                foreach (var archivo in archivos)
                {
                    zip.AddFile(archivo, string.Empty);
                }
                zip.Save(tempZipFilePath);
            }
            return tempZipFilePath;
        }
        public static void AddLoadingScreen(Page page, List<HtmlControl> controls)
        {
            foreach (var control in controls)
            {
                control.Attributes["onclick"] = "showLoading();";
            }
            var cssLiteral = new LiteralControl(@"
                <style>
                    #overlay {
                        position: fixed;
                        top: 0;
                        left: 0;
                        width: 100%;
                        height: 100%;
                        background-color: rgba(0, 0, 0, 0.5);
                        z-index: 9999;
                        display: none;
                    }

                    #loading {
                        position: absolute;
                        top: 50%;
                        left: 50%;
                        transform: translate(-50%, -50%);
                        text-align: center;
                        color: white;
                    }

                    .loader {
                        border: 4px solid #f3f3f3;
                        border-radius: 50%;
                        border-top: 4px solid #3498db;
                        width: 30px;
                        height: 30px;
                        -webkit-animation: spin 1s linear infinite; /* Safari */
                        animation: spin 1s linear infinite;
                    }

                    /* Safari */
                    @-webkit-keyframes spin {
                        0% { -webkit-transform: rotate(0deg); }
                        100% { -webkit-transform: rotate(360deg); }
                    }

                    @keyframes spin {
                        0% { transform: rotate(0deg); }
                        100% { transform: rotate(360deg); }
                    }
                </style>
            ");
            page.Header.Controls.Add(cssLiteral);
            const string script = @"
                <script>
                    window.onpageshow = function(event) {
                        if (event.persisted) {
                            hideLoading();
                        }
                    };
                </script>
            ";
            page.ClientScript.RegisterClientScriptBlock(page.GetType(), "reloadScript", script);
            const string loadingScript = @"
                <script runat=""server"">
                    var overlay = document.getElementById('overlay');
                    var loading = document.getElementById('loading');
                    function showLoading() {
                        overlay.style.display = 'block';
                    }
                    function hideLoading() {
                        overlay.style.display = 'none';
                    }
                </script>";
            page.ClientScript.RegisterStartupScript(page.GetType(), "loadingScript", loadingScript);
            var overlay = new HtmlGenericControl("div") { ID = "overlay" };
            var loading = new HtmlGenericControl("div") { ID = "loading", InnerHtml = @"<div class=""loader""></div>" };
            overlay.Controls.Add(loading);
            page.FindControl("form1").Controls.Add(overlay);
        }
        public static BoundField BoundField(string dataField, string headerText) =>
            new BoundField() { DataField = dataField, HeaderText = headerText };
        public static ButtonField ButtonField(string text, string commandName) =>
            new ButtonField() { Text = text, CommandName = commandName };
        public static ButtonField ButtonField(ButtonType buttonType, string text, string commandName) =>
            new ButtonField() { Text = text, CommandName = commandName, ButtonType = buttonType };
        public static GridView GridView(string id, bool autoGenerateColumns) =>
            new GridView() { ID = id, AutoGenerateColumns = autoGenerateColumns };
        public static HtmlButton Button(string id, string text) =>
            new HtmlButton() { ID = id, InnerText = text };
        public static Label Label(string id, string text) =>
            new Label() { ID = id, Text = text };
        public static LiteralControl LiteralControl(string control) =>
            new LiteralControl(control);
        public static TextBox TextBox(string id) =>
            new TextBox() { ID = id };
        public static DropDownList DropDownList(string id, List<string> datos) =>
            new DropDownList() { ID = id, DataSource = datos };
        public static HtmlAnchor HtmlAnchor(string id, string text, string href) =>
            new HtmlAnchor() { ID = id, InnerText = text, HRef = href };
        public static LiteralControl Br() =>
            new LiteralControl("<br />");
        public static Panel Panel(string id, string clase) =>
            new Panel() { ID = id, CssClass = clase };
        public static Image Image(string id, string clase, string src) =>
            new Image() { ID = id, CssClass = clase, ImageUrl = src };
        public static void DoPostback(Page page) =>
            ScriptManager.RegisterStartupScript(page, page.GetType(), "PostBack", "__doPostBack('', '');", true);
        public static void Alert(Page page, string mensaje) =>
            ScriptManager.RegisterClientScriptBlock(page, page.GetType(), "alertMessage", $"alert('{mensaje}')", true);
    }
}