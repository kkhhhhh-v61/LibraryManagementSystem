global using LibraryManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSqlServer<LMSDB>($@"
    Data Source=(LocalDB)\MSSQLLocalDB;
    AttachDbFilename={builder.Environment.ContentRootPath}\LMSDB.mdf;
");

builder.Services.AddWebOptimizer(pipeline =>
{
    pipeline.AddCssBundle(
        "/bundle.css",
        "/css/app.css",
        "/css/booking.css",
        "/css/catalogue.css",
        "/css/room.css"
    );

    pipeline.AddJavaScriptBundle(
        "/bundle.js",
        "/js/app.js",
        "/js/catalogue.js",
        "/js/jquery.min.js",
        "/js/jquery.unobtrusive-ajax.min.js",
        "/js/jquery.validate.min.js",
        "/js/jquery.validate.unobtrusive.min.js"
    );
});

var app = builder.Build();
app.UseWebOptimizer();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.MapDefaultControllerRoute();
app.Run();