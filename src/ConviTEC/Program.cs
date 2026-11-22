using Microsoft.AspNetCore.Builder; //WebApplication y middlewares

var builder = WebApplication.CreateBuilder(args); //configura la aplicación
var app = builder.Build(); //construye la aplicación

app.UseDefaultFiles(); //en "/" sirve wwwroot/index.html
app.UseStaticFiles(); //sirve los archivos de wwwroot (frontend)

app.Run(); //arranca el servidor web