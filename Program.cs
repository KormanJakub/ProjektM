using Microsoft.EntityFrameworkCore;
using ProjektM.Middleware;
using ProjektM.Models;
using ProjektM.Services;
using ProjektM.Settings;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddSingleton<PasswordHasherService>();
builder.Services.AddSingleton<GenerateResetTokenService>();
builder.Services.AddDbContext<PmiContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

//app.UseMiddleware<AdminMiddleware>();

app.MapControllers();

app.Run();
