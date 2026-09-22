using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Web.UI;

namespace PortalProveedores
{
    public partial class LoginForm : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Title = "Escorial - Portal Proveedores";
            var userLabel = Library.Label("UserLabel", "Usuario:");
            form1.Controls.Add(userLabel);
            form1.Controls.Add(Library.Br());
            var userTextBox = Library.TextBox("UserTextBox");
            form1.Controls.Add(userTextBox);
            form1.Controls.Add(Library.Br());
            var passwordLabel = Library.Label("PasswordLabel", "Contraseña:");
            form1.Controls.Add(passwordLabel);
            form1.Controls.Add(Library.Br());
            var passwordTextBox = Library.TextBox("PasswordTextBox");
            form1.Controls.Add(passwordTextBox);
            form1.Controls.Add(Library.Br());
            var submitButton = Library.Button("LoginButton", "Ingresar");
            submitButton.ServerClick += SubmitButton_ServerClick;
            form1.Controls.Add(submitButton);
            form1.Controls.Add(Library.Br());

        }
        public async void SubmitButton_ServerClick(object sender, EventArgs e)
        {
            var passwordTextBox = (TextBox)form1.FindControl("PasswordTextBox");
            if (passwordTextBox is null)
                return;
            var userTextBox = (TextBox)form1.FindControl("UserTextBox");
            if (userTextBox is null) 
                return;
            var login = await Login(userTextBox.Text, passwordTextBox.Text);
            if ((login is HttpStatusCode statusCode && statusCode == HttpStatusCode.BadRequest) || (string.IsNullOrEmpty(passwordTextBox.Text) && string.IsNullOrEmpty(userTextBox.Text)))
            {
                Library.Alert(this, "Error en inicio de sesión");
                return;
            }
            Config.Proveedor = ((dynamic)login).Proveedor;
            Response.Redirect("MenuForm.aspx");
        }
        private async Task<object> Login(string user, string password)
        {
            try
            {
                var requestData = new
                {
                    User = user,
                    Password = password
                };
                var jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, System.Text.Encoding.UTF8, "application/json");

                var response = await Config.SharedClient.PostAsync("api/portalproveedores/login", content);
                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<object>(jsonString);
                return data;
            }
            catch (HttpRequestException)
            {
                return HttpStatusCode.BadRequest;
            }
        }
    }
}