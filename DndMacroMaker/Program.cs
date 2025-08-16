var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// dodaj us³ugê scraper
builder.Services.AddSingleton<ScraperService>();

// kontrolery + swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddControllersWithViews();


var app = builder.Build();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Railway wymaga nas³uchiwania na $PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://*:{port}");
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1"));
}
// obs³uga plików statycznych (frontend)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/", () => "Hello World!");
app.MapFallbackToFile("index.html");

// API
app.MapControllers();

app.Run();
