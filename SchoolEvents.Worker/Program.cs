using Microsoft.EntityFrameworkCore;
using SchoolEvents.Worker.Services;
using SchoolEventsAPI.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("../appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"../appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IEmailSender, LogEmailSender>();
builder.Services.AddScoped<NotificationJobProcessor>();
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
await host.RunAsync();
