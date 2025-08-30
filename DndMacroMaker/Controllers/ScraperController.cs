using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ScraperController : ControllerBase
{
    private readonly ScraperService _scraper;

    public ScraperController(ScraperService scraper)
    {
        _scraper = scraper;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Brak URL");

        var spell = await _scraper.ScrapeAsync(url);
        if (spell == null)
            return NotFound("Nie uda³o siê pobraæ zaklêcia");

        var macro = _scraper.GenerateMacro(spell);
        return Ok(macro);
    }


    [HttpGet("raw")]
    public async Task<IActionResult> GetRaw([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return BadRequest("Brak URL");

        var spell = await _scraper.ScrapeAsync(url);
        if (spell == null)
            return NotFound("Nie uda³o siê pobraæ zaklêcia");

        return Ok(spell);
    }


    [HttpPost("generate")]
    public IActionResult Generate([FromBody] ScraperService.Spell spell)
    {
        if (spell == null)
            return BadRequest("Brak danych zaklêcia");

        var macro = _scraper.GenerateMacro(spell);
        return Ok(macro);
    }

}
