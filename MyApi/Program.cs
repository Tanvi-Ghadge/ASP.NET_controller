using MyApi.data;
using Microsoft.EntityFrameworkCore;
using MyApi.Mappings;
using MyApi.Repository.Interface;
using MyApi.Repository.Implementation;
using MyApi.Service.Interface;
using MyApi.Service.Implementation;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<Dbcontext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("defaultconnection"));
});
builder.Services.AddScoped<Iemployeerepository, EmployeeRepository>();
builder.Services.AddScoped<Iemployeeservice, EmployeeService>();
// builder.Services.AddDbContext<TeamsDbcontext>(options =>
// {
//     options.UseSqlServer(builder.Configuration.GetConnectionString("connection"));
// });
builder.Services.AddAutoMapper(cfg => { }, typeof(EmployeeProfile).Assembly);
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
