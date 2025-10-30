using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SegundopApi.Data;
using SegundopApi.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// CONFIGURACIÓN DE SERVICIOS
// -------------------------------------------------------

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// 🔹 Habilitar CORS (para permitir peticiones desde Flutter Web, Android o Netlify)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});;

// 🔹 Configuración de Swagger con autenticación JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SegundoParcialApi",
        Version = "v1",
        Description = "API para el segundo parcial de Programación Aplicada"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT con el prefijo 'Bearer '.\n\nEjemplo: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 🔹 Configuración de la base de datos
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);

// 🔹 Servicio de Tokens JWT
builder.Services.AddScoped<TokenService>();

// 🔹 Configuración de autenticación JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };
    });

// 🔹 Habilitar autorización
builder.Services.AddAuthorization();

// -------------------------------------------------------
// CONFIGURACIÓN DE LA APLICACIÓN
// -------------------------------------------------------

var app = builder.Build();

// 🔹 Swagger (solo en desarrollo)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ⚠️ Importante: Desactivar HTTPS en desarrollo si Flutter usa HTTP
// Esto evita el error de “mixed content” en navegadores.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// 🔹 Activar CORS antes de autenticación
app.UseCors("AllowAll");

// 🔹 Autenticación y autorización

app.UseAuthentication();
app.UseAuthorization();
// Habilitar Swagger siempre
app.UseSwagger();
app.UseSwaggerUI();

// 🔹 Mapear controladores
app.MapControllers();
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
    await next();
});

// 🔹 Iniciar aplicación
app.Run();

