using Microsoft.EntityFrameworkCore;
using SchoolEvents.Worker.Services;
using SchoolEventsAPI.Data;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
        .GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailSender, LogEmailSender>();
builder.Services.AddScoped<NotificationJobProcessor>();
builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
await host.RunAsync();