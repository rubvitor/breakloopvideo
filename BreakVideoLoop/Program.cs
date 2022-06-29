using BreakVideoLoop.Domain;
using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Models;
using System.IO.Abstractions;
using BreakVideoLoop.Domain.DI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IFileSystem, FileSystem>();
builder.Services.AddScoped<ISearchTools, SearchTools>();

builder.Services.Configure<ConfigurationBreakLoop>(builder.Configuration.GetSection("Configuration"));

builder.Services.AddVoskInjection();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
