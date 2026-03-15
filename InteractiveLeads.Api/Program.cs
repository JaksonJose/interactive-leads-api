using InteractiveLeads.Api.Middleware;
using InteractiveLeads.Application;
using InteractiveLeads.Infrastructure;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

builder.Services.AddInfraestructureServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Services.GetJwtSettings(builder.Configuration));

builder.Services.AddApplicationServices();

var app = builder.Build();

await app.Services.AddDatabaseInitializerAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();

app.UseInfraestructure();

app.UseMiddleware<TenantAccessValidationMiddleware>();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllers();

app.Run();
