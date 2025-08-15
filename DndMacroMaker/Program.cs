var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ScraperService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseDefaultFiles(); // aby index.html dzia³a³ bez œcie¿ki
app.UseStaticFiles();
app.MapFallbackToFile("index.html");
app.UseRouting();
app.MapControllers();

app.Run();
