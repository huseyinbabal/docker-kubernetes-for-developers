using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;
using ProductService.Services.Messaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
    $"Host={builder.Configuration["Database:Host"] ?? "localhost"};" +
    $"Port={builder.Configuration["Database:Port"] ?? "5432"};" +
    $"Database={builder.Configuration["Database:Name"] ?? "productdb"};" +
    $"Username={builder.Configuration["Database:User"] ?? "postgres"};" +
    $"Password={builder.Configuration["Database:Password"] ?? "password"};";

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