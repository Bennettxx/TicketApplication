using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketApplication.Data;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
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
        public async Task<ActionResult<IEnumerable<User>>> Get()
        {
            // Wir fragen die DB asynchron ab und geben die Liste zurück
            var users = await _context.Users.Where(u => u.IsActive).ToListAsync();

            
            return Ok(users);
        }

        [HttpGet("{id}")]
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

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, User user)
        {
            if (id != user.Id)
            {
                return BadRequest();
            }

            // wir müssen später noch einbauen, dass man sein pw erst eingeben muss bevor man eine änderung eingeben kann

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
        [Authorize(Roles = "Admin")]
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