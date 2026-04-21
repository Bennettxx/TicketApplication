using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // Dieser Controller ist für die Verwaltung der User zuständig
    // Er bietet Endpunkte für CRUD-Operationen (Create, Read, Update, Delete) auf User-Objekten
    // Alle Endpunkte in diesem Controller haben die Basis-URL "api/user"
    
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UserController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet(Name = "GetUsers")]
        //[Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<IEnumerable<User>>> Get()
        {
            // Wir fragen die DB asynchron ab und geben die Liste zurück
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

            
            return Ok(users);
        }

        [HttpGet("{id}")]
        //[Authorize(Roles = "Admin, Support")]
        public async Task<ActionResult<User>> Get(int id)
        {
            // Wir fragen die DB asynchron ab und geben die Liste zurück
            var user = await _context.Users.FindAsync(id);
            if (user == null || !user.IsActive)
            {
                // NotFound() = 404 Statuscode
                return NotFound();
            }
            
            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<User>> Post(User user)
        {
            bool emailExists = await _context.Users
            .AnyAsync(u => u.Email == user.Email && u.IsActive == true);

            if (emailExists)
            {
                // Augabe Fehler 400
                return BadRequest("Diese Email-Adresse wird bereits verwendet.");
            }

            // Primary Key wird inkrementell von SQL DB vergeben und wirft Fehler wenn wir das manuell machen wollen
            user.Id = 0;
            // geht das so?
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.PasswordHash = passwordHash;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Put(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            // Hier nur Änderungen durch den Admin gemeint, am eigenen Profil kann der User selbst Änderungen vornehmen, siehe AccountController

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        [HttpDelete("{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}