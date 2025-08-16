var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot"
});

// dodaj us�ug� scraper
builder.Services.AddSingleton<ScraperService>();

// kontrolery + swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Railway wymaga nas�uchiwania na $PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://*:{port}");

// obs�uga plik�w statycznych (frontend)
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapGet("/", async context =>
{
    context.Response.Redirect("/index.html");
});
app.MapFallbackToFile("index.html");

// API
app.MapControllers();

app.Run();
