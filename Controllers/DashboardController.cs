using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // Liefert die Kennzahlen für das Dashboard.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> Get()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "User";
            var isStaff = role == "Admin" || role == "Support";

            // Sichtbarer Grundbestand: Staff alle, User nur eigene.
            IQueryable<Ticket> basis = _context.Tickets;
            if (!isStaff)
                basis = basis.Where(t => t.CreatedByUserId == userId);

            var dto = new DashboardDto
            {
                Role = role,
                IsStaff = isStaff,
                OpenCount = await basis.CountAsync(t => t.Status == TicketStatus.Open),
                InProgressCount = await basis.CountAsync(t => t.Status == TicketStatus.InProgress),
                ClosedCount = await basis.CountAsync(t => t.Status == TicketStatus.Closed),
                TotalCount = await basis.CountAsync()
            };

            if (isStaff)
            {
                dto.MyAssignedOpenCount = await _context.Tickets
                    .CountAsync(t => t.AssignedToId == userId && t.Status != TicketStatus.Closed);
                dto.UnassignedOpenCount = await _context.Tickets
                    .CountAsync(t => t.AssignedToId == null && t.Status != TicketStatus.Closed);
            }

            dto.Recent = await basis
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new DashboardTicketDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    StatusCode = (int)t.Status,
                    PriorityCode = (int)t.Priority,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(dto);
        }
    }
}
