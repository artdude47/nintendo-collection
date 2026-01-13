using Collection.Infrastructure;
using Collection.Web.Endpoints;
using Collection.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=app.db";

builder.Services.AddDbContextFactory<AppDbContext>(opt => opt.UseSqlite(cs));

builder.Services.AddScoped<AppDbContext>(p =>
    p.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

builder.Services.AddScoped<CsvImportService>();
builder.Services.AddScoped<InventoryService>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddCircuitOptions(o => o.DetailedErrors = true);
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter("tight", o => { o.PermitLimit = 100; o.Window = TimeSpan.FromMinutes(1); }));


builder.Services.ConfigureHttpJsonOptions(opt => {
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();


app.MapItemEndpoints();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();