

using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Services;
using ReportGen.Middleware;
using ReportGen.Interfaces;
using ReportGen.Repositories;
using Microsoft.OpenApi.Models;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICsvProcessingService, CsvProcessingService>();
builder.Services.AddScoped<IResultRepository, ResultRepository>();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ReportGen API", 
        Version = "v1", 
        Description = "API для работы с timescale данными.", 
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath)) 
    {
        c.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<ReportGenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportGenDb");
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
    
    app.UseSwagger(); 
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReportGen API v1"); 
        c.RoutePrefix = "swagger"; 
        c.DocumentTitle = "Документация API"; 
    });
}
else
{
    app.UseErrorHandlingMiddleware();
}

app.MapControllers();

app.Run();
