using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // Wissensdatenbank / Lösungsvorschläge.
    // Route: api/knowledge
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class KnowledgeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public KnowledgeController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsStaff =>
            User.IsInRole("Admin") || User.IsInRole("Support");

        // GET /api/knowledge/suggest?q=...  -> Vorschläge anhand des Suchtexts.
        // Einfache Stichwortsuche: Der Text wird in Wörter zerlegt; ein Artikel
        // wird vorgeschlagen, wenn eines der Wörter in Titel/Stichwörtern/Lösung
        // vorkommt. Treffer werden nach Anzahl passender Wörter sortiert.
        // Jeder eingeloggte Nutzer darf das (z.B. beim Ticket-Erstellen).
        [HttpGet("suggest")]
        public async Task<ActionResult<IEnumerable<KnowledgeSuggestionDto>>> Suggest([FromQuery] string? q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 3)
                return Ok(Array.Empty<KnowledgeSuggestionDto>());

            // Suchwörter (ab 3 Zeichen) extrahieren.
            var woerter = q.ToLowerInvariant()
                .Split(new[] { ' ', ',', ';', '.', '!', '?', '\n', '\r', '\t', '-', '/' },
                       StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 3)
                .Distinct()
                .Take(8)
                .ToList();
            if (woerter.Count == 0)
                woerter.Add(q.Trim().ToLowerInvariant());

            // Die Wissensdatenbank ist klein -> veröffentlichte Artikel laden und
            // im Speicher bewerten (robuster als dynamische OR-Abfragen).
            var artikel = await _context.KnowledgeArticles
                .Where(a => a.IsPublished)
                .ToListAsync();

            var treffer = artikel
                .Select(a =>
                {
                    var heu = $"{a.Title} {a.Keywords} {a.Solution}".ToLowerInvariant();
                    var score = woerter.Count(w => heu.Contains(w));
                    return new { a, score };
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.score)
                .ThenByDescending(x => x.a.UpdatedAt)
                .Take(5)
                .Select(x => new KnowledgeSuggestionDto
                {
                    Id = x.a.Id,
                    Title = x.a.Title,
                    Solution = x.a.Solution
                })
                .ToList();

            return Ok(treffer);
        }

        // GET /api/knowledge  -> Alle Artikel (Verwaltung, nur Staff).
        [HttpGet]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<IEnumerable<KnowledgeArticleDto>>> GetAll()
        {
            var list = await (from a in _context.KnowledgeArticles
                              join d in _context.Departments on a.DepartmentId equals d.Id into dj
                              from d in dj.DefaultIfEmpty()
                              orderby a.UpdatedAt descending
                              select new KnowledgeArticleDto
                              {
                                  Id = a.Id,
                                  Title = a.Title,
                                  Keywords = a.Keywords,
                                  Solution = a.Solution,
                                  DepartmentId = a.DepartmentId,
                                  DepartmentName = d != null ? d.Name : null,
                                  IsPublished = a.IsPublished,
                                  CreatedAt = a.CreatedAt,
                                  UpdatedAt = a.UpdatedAt
                              }).ToListAsync();
            return Ok(list);
        }

        // GET /api/knowledge/{id}  -> Einzelner Artikel.
        [HttpGet("{id}")]
        public async Task<ActionResult<KnowledgeArticleDto>> Get(int id)
        {
            var a = await _context.KnowledgeArticles.FindAsync(id);
            if (a == null) return NotFound();
            // Unveröffentlichte Artikel nur für Staff sichtbar.
            if (!a.IsPublished && !IsStaff) return NotFound();

            string? depName = a.DepartmentId == null ? null : await _context.Departments
                .Where(d => d.Id == a.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync();

            return Ok(new KnowledgeArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Keywords = a.Keywords,
                Solution = a.Solution,
                DepartmentId = a.DepartmentId,
                DepartmentName = depName,
                IsPublished = a.IsPublished,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            });
        }

        // POST /api/knowledge  -> Artikel anlegen (Staff).
        [HttpPost]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<KnowledgeArticleDto>> Create(CreateKnowledgeArticleDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            int? depId = await ResolveDepartmentId(dto.DepartmentName);

            var a = new KnowledgeArticle
            {
                Title = dto.Title.Trim(),
                Keywords = dto.Keywords?.Trim() ?? string.Empty,
                Solution = dto.Solution.Trim(),
                DepartmentId = depId,
                IsPublished = dto.IsPublished,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.KnowledgeArticles.Add(a);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = a.Id }, new KnowledgeArticleDto
            {
                Id = a.Id,
                Title = a.Title,
                Keywords = a.Keywords,
                Solution = a.Solution,
                DepartmentId = a.DepartmentId,
                IsPublished = a.IsPublished,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt
            });
        }

        // PUT /api/knowledge/{id}  -> Artikel ändern (Staff, Felder optional).
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> Update(int id, UpdateKnowledgeArticleDto dto)
        {
            var a = await _context.KnowledgeArticles.FindAsync(id);
            if (a == null) return NotFound();

            if (dto.Title != null) a.Title = dto.Title.Trim();
            if (dto.Keywords != null) a.Keywords = dto.Keywords.Trim();
            if (dto.Solution != null) a.Solution = dto.Solution.Trim();
            if (dto.DepartmentName != null) a.DepartmentId = await ResolveDepartmentId(dto.DepartmentName);
            if (dto.IsPublished.HasValue) a.IsPublished = dto.IsPublished.Value;

            a.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/knowledge/{id}  -> Artikel löschen (Staff).
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> Delete(int id)
        {
            var a = await _context.KnowledgeArticles.FindAsync(id);
            if (a == null) return NotFound();
            _context.KnowledgeArticles.Remove(a);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private async Task<int?> ResolveDepartmentId(string? name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            return await _context.Departments
                .Where(d => d.Name == name)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
