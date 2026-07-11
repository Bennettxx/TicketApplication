using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // problemmeldungen: erstellen für jeden (auch anonym), verwaltung nur admin
    [Route("api/[controller]")]
    [ApiController]
    public class ProblemController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProblemController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST api/problem — neue meldung, login optional
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ProblemResponseDto>> Create(CreateProblemDto dto)
        {
            // bei gültigem token den ersteller festhalten
            int? createdBy = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(idClaim, out var uid)) createdBy = uid;
            }

            var problem = new Problem
            {
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                Priority = dto.Priority,
                Status = TicketStatus.Open,
                ContactEmail = dto.ContactEmail.Trim(),
                CreatedByUserId = createdBy,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Problems.Add(problem);
            await _context.SaveChangesAsync();

            return Ok(ToDto(problem));
        }

        // GET api/problem — liste mit filtern (q, status, priority), nur admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ProblemResponseDto>>> GetAll(
            [FromQuery] string? q, [FromQuery] int? status, [FromQuery] int? priority)
        {
            IQueryable<Problem> query = _context.Problems;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(p =>
                    p.Title.Contains(term) || p.Description.Contains(term) || p.ContactEmail.Contains(term));
            }
            if (status is >= 0 and <= 2)
                query = query.Where(p => (int)p.Status == status);
            if (priority is >= 0 and <= 2)
                query = query.Where(p => (int)p.Priority == priority);

            var problems = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(problems.Select(ToDto));
        }

        // GET api/problem/{id} — einzelnes problem, nur admin
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProblemResponseDto>> Get(int id)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null) return NotFound();
            return Ok(ToDto(problem));
        }

        // PATCH api/problem/{id}/status — status setzen, nur admin
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateProblemStatusDto dto)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null) return NotFound();

            problem.Status = dto.Status;
            problem.UpdatedAt = DateTime.UtcNow;
            problem.ClosedAt = dto.Status == TicketStatus.Closed ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE api/problem/{id} — löschen, nur admin
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var problem = await _context.Problems.FindAsync(id);
            if (problem == null) return NotFound();
            _context.Problems.Remove(problem);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // entity -> response-dto
        private static ProblemResponseDto ToDto(Problem p) => new()
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Priority = p.Priority.ToString(),
            PriorityCode = (int)p.Priority,
            Status = p.Status.ToString(),
            StatusCode = (int)p.Status,
            ContactEmail = p.ContactEmail,
            CreatedByUserId = p.CreatedByUserId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            ClosedAt = p.ClosedAt
        };
    }
}
