using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;
using ProductService.Services.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database - Environment variables take priority
string connectionString;

// First priority: Complete connection string from environment variable
var envConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (!string.IsNullOrEmpty(envConnectionString))
{
    connectionString = envConnectionString;
}
else
{
    // Second priority: Build from individual environment variables
    var host = Environment.GetEnvironmentVariable("DATABASE_HOST")
               ?? Environment.GetEnvironmentVariable("DB_HOST");
    var port = Environment.GetEnvironmentVariable("DATABASE_PORT")
               ?? Environment.GetEnvironmentVariable("DB_PORT");
    var database = Environment.GetEnvironmentVariable("DATABASE_NAME")
                   ?? Environment.GetEnvironmentVariable("DB_NAME");
    var username = Environment.GetEnvironmentVariable("DATABASE_USER")
                   ?? Environment.GetEnvironmentVariable("DB_USER");
    var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD")
                   ?? Environment.GetEnvironmentVariable("DB_PASSWORD");

    // If any environment variables are set, use them (with defaults for missing ones)
    if (!string.IsNullOrEmpty(host) || !string.IsNullOrEmpty(port) ||
        !string.IsNullOrEmpty(database) || !string.IsNullOrEmpty(username) ||
        !string.IsNullOrEmpty(password))
    {
        host ??= "localhost";
        port ??= "5432";
        database ??= "productdb";
        username ??= "postgres";
        password ??= "password";

        connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
    }
    else
    {
        // Third priority: Configuration file connection string
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        // Fourth priority: Build from configuration file individual settings
        if (string.IsNullOrEmpty(connectionString))
        {
            host = builder.Configuration["Database:Host"] ?? "localhost";
            port = builder.Configuration["Database:Port"] ?? "5432";
            database = builder.Configuration["Database:Name"] ?? "productdb";
            username = builder.Configuration["Database:User"] ?? "postgres";
            password = builder.Configuration["Database:Password"] ?? "password";

            connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};";
        }
    }
}

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(connectionString));

// Services
builder.Services.AddScoped<IProductService, ProductServiceImpl>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
