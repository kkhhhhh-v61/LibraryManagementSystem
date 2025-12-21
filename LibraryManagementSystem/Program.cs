global using LibraryManagementSystem.Models;
using LibraryManagementSystem.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddTransient<LibraryManagementSystem.Services.IEmailSender, LibraryManagementSystem.Services.IEmailSender>();
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
var connectionString = $@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={builder.Environment.ContentRootPath}\LMSDB.mdf;";

builder.Services.AddSqlServer<LMSDB>(
    connectionString,
    sqlOptions => sqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorNumbersToAdd: null
    ));

// Configure cookie authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied"; // Optional
    });

builder.Services.AddTransient<IEmailService, EmailService>();

builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IFineService, FineService>();
builder.Services.AddHostedService<FineBackgroundService>();

builder.Services.AddHostedService<BookingReminderService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("MemberOnly", policy => policy.RequireRole(User.UserRole.MEMBER.ToString()));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole(User.UserRole.STAFF.ToString()));
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(User.UserRole.ADMIN.ToString()));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
