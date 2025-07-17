

using Microsoft.EntityFrameworkCore;
using ReportGen.Data;
using ReportGen.Services;
using ReportGen.Middleware;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ICsvProcessingService, CsvProcessingService>();
builder.Services.AddControllers();

builder.Services.AddDbContext<ReportGenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportGenDb");
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage(); 
}
else
{
    app.UseErrorHandlingMiddleware(); 
}

app.MapControllers();

app.Run();
