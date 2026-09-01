using Microsoft.EntityFrameworkCore;
using DashboardFinanceiroPessoal.Data;

var builder = WebApplication.CreateBuilder(args);

// Adicionando o DbContext para o projeto reconhecer o banco SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
