using InteractiveLeads.Api.Middleware;
using InteractiveLeads.Application;
using Microsoft.AspNetCore.Http.Features;
using InteractiveLeads.Application.Realtime.Services;
using InteractiveLeads.Api.Realtime.Hubs;
using InteractiveLeads.Api.Realtime.Services;
using InteractiveLeads.Infrastructure;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder
            // Quando a requisição é feita com credentials=true,
            // o browser não aceita Access-Control-Allow-Origin="*".
            // Então usamos SetIsOriginAllowed para refletir a origem real.
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: true));
    });

builder.Services.AddSignalR(options =>
    {
        options.KeepAliveInterval = TimeSpan.FromSeconds(15);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    })
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddInfraestructureServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Services.GetJwtSettings(builder.Configuration));

builder.Services.AddApplicationServices();

builder.Services.AddScoped<IRealtimeService, RealtimeService>();
builder.Services.AddScoped<IRealtimeJoinAuthorizationService, RealtimeJoinAuthorizationService>();

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
app.MapHub<ChatHub>("/hubs/chat");

app.Run();
