

using Microsoft.EntityFrameworkCore;
using ReportGen.Models;
using ReportGen.Data;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ReportGenDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ReportGenDb");
    options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
