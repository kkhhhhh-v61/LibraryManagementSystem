var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "If you see this message, the program works correctly. :)");

app.Run();
