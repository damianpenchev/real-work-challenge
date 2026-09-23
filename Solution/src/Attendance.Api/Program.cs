using Attendance.Api;
using Attendance.Application;
using Attendance.Domain;
using Attendance.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=attendance.db";

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(o => o.InvalidModelStateResponseFactory = ApiProblems.FromModelState);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication(o => builder.Configuration.GetSection(AttendanceOptions.SectionName).Bind(o));
builder.Services.AddInfrastructure(db => db.UseSqlite(connectionString));

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerPathFeature>()?.Error;
    var problem = error is UniqueInvariantViolationException conflict
        ? ApiProblems.Build(StatusCodes.Status409Conflict, "The record was changed by someone else.",
            new[] { new ApiError("CONFLICT", $"Another submission already satisfied {conflict.Invariant}.", null, null) })
        : ApiProblems.Build(StatusCodes.Status500InternalServerError, "An unexpected error occurred.",
            Array.Empty<ApiError>());

    context.Response.StatusCode = problem.Status!.Value;
    context.Response.ContentType = ApiProblems.ContentType;
    await context.Response.WriteAsJsonAsync(problem);
}));

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

await app.Services.InitialiseAsync();

app.Run();

public partial class Program;
