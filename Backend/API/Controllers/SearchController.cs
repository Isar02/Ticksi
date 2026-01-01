using Ticksi.Application.DTOs;
using API.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace API.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController : ControllerBase
{
    private readonly IAppDbContext _db;

    public SearchController(IAppDbContext db)
    {
        _db = db;
    }

    // GET: /api/search/suggestions?q=mu&limit=10
    [HttpGet("suggestions")]
    public async Task<ActionResult<List<SearchSuggestionDto>>> GetSuggestions(
        [FromQuery] string q,
        [FromQuery] int limit = 10)
    {
        q = (q ?? "").Trim();
        if (q.Length < 2) return Ok(new List<SearchSuggestionDto>());

        limit = Math.Clamp(limit, 1, 20);

        // širi fuzzy kandidati: uzmi prva 3 slova
        var key = q.Length <= 3 ? q : q.Substring(0, 3);

        var eventCandidates = await _db.Events.AsNoTracking()
    .Where(e => EF.Functions.Like(e.Name, $"%{key}%"))
    .Select(e => new { Label = e.Name, PublicId = e.PublicId })
    .Distinct()
    .Take(50)
    .ToListAsync();

        var categoryCandidates = await _db.EventCategories.AsNoTracking()
            .Where(c => EF.Functions.Like(c.Name, $"%{key}%"))
            .Select(c => new { Label = c.Name, PublicId = c.PublicId })
            .Distinct()
            .Take(50)
            .ToListAsync();

        var locationCandidates = await _db.Locations.AsNoTracking()
            .Where(l => EF.Functions.Like(l.Name, $"%{key}%") || EF.Functions.Like(l.City, $"%{key}%"))
            .Select(l => new { Label = l.Name, PublicId = l.PublicId })
            .Distinct()
            .Take(50)
            .ToListAsync();

        var suggestions = new List<SearchSuggestionDto>();

        foreach (var e in eventCandidates)
        {
            var score = FuzzyMatcher.Score(q, e.Label);
            if (score > 0.15)
                suggestions.Add(new SearchSuggestionDto
                {
                    Type = "event",
                    Label = e.Label,
                    PublicId = e.PublicId,
                    Score = score
                });
        }

        foreach (var c in categoryCandidates)
        {
            var score = FuzzyMatcher.Score(q, c.Label);
            if (score > 0.15)
                suggestions.Add(new SearchSuggestionDto
                {
                    Type = "category",
                    Label = c.Label,
                    PublicId = c.PublicId,
                    Score = score
                });
        }

        foreach (var l in locationCandidates)
        {
            var score = FuzzyMatcher.Score(q, l.Label);
            if (score > 0.15)
                suggestions.Add(new SearchSuggestionDto
                {
                    Type = "location",
                    Label = l.Label,
                    PublicId = l.PublicId,
                    Score = score
                });
        }


        // dedupe po (Type + Label)
        var final = suggestions
            .GroupBy(s => (s.Type, Label: FuzzyMatcher.Norm(s.Label)))
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.Type)
            .Take(limit)
            .ToList();

        return Ok(final);
    }
}
