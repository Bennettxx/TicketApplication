using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TicketApplication.Data;
using TicketApplication.DTOs;
using TicketApplication.Functions;
using TicketApplication.Models;

namespace TicketApplication.Controllers
{
    // Datei-Anhänge eines Tickets.
    // Route: api/ticket/{ticketId}/attachments
    //
    // Speicherung:
    //   - privat  -> AES-GCM-verschlüsselt in der DB
    //   - öffentl. -> als Datei im Projekt-Unterordner "AttachmentStorage"
    [Route("api/ticket/{ticketId}/attachments")]
    [ApiController]
    [Authorize]
    public class AttachmentController : ControllerBase
    {
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB
        private const string StorageFolder = "AttachmentStorage";

        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AttachmentController(ApplicationDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _config = config;
            _env = env;
        }

        private int CurrentUserId =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private bool IsStaff =>
            User.IsInRole("Admin") || User.IsInRole("Support");

        private byte[] EncryptionKey =>
            Convert.FromBase64String(_config["Attachments:Key"]!);

        // Prüft, ob der aktuelle User dieses Ticket sehen darf.
        private async Task<bool> DarfTicketSehen(int ticketId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return false;
            return IsStaff || ticket.CreatedByUserId == CurrentUserId;
        }

        // GET -> Liste der Anhänge (nur Metadaten).
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttachmentResponseDto>>> Get(int ticketId)
        {
            if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
                return NotFound("Ticket nicht gefunden.");
            if (!await DarfTicketSehen(ticketId))
                return Forbid();

            var list = await (from a in _context.TicketAttachments
                              where a.TicketId == ticketId
                              join u in _context.Users on a.UploadedByUserId equals u.Id into uj
                              from u in uj.DefaultIfEmpty()
                              orderby a.CreatedAt descending
                              select new AttachmentResponseDto
                              {
                                  Id = a.Id,
                                  TicketId = a.TicketId,
                                  DataName = a.DataName,
                                  ContentType = a.ContentType,
                                  FileSize = a.FileSize,
                                  ContainsPrivateData = a.ContainsPrivateData,
                                  UploadedByUserId = a.UploadedByUserId,
                                  UploadedByEmail = u != null ? u.Email : string.Empty,
                                  CreatedAt = a.CreatedAt
                              }).ToListAsync();

            return Ok(list);
        }

        // POST (multipart/form-data) -> Datei hochladen.
        // Felder: file (Datei), isPrivate (bool).
        [HttpPost]
        [RequestSizeLimit(MaxBytes + 4096)] // etwas Puffer für Multipart-Overhead
        public async Task<ActionResult<AttachmentResponseDto>> Upload(
            int ticketId, [FromForm] IFormFile? file, [FromForm] bool isPrivate)
        {
            if (!await _context.Tickets.AnyAsync(t => t.Id == ticketId))
                return NotFound("Ticket nicht gefunden.");
            if (!await DarfTicketSehen(ticketId))
                return Forbid();

            // --- Eingangsprüfung (dies ist das Eintrittstor) ---
            if (file == null || file.Length == 0)
                return BadRequest("Keine Datei übergeben.");
            if (file.Length > MaxBytes)
                return BadRequest($"Datei zu groß. Maximal {MaxBytes / (1024 * 1024)} MB erlaubt.");

            var originalName = Path.GetFileName(file.FileName);
            if (string.IsNullOrWhiteSpace(originalName) || originalName.Length > 255)
                return BadRequest("Ungültiger Dateiname.");

            // Datei in den Speicher lesen.
            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                bytes = ms.ToArray();
            }

            var attachment = new TicketAttachments
            {
                TicketId = ticketId,
                UploadedByUserId = CurrentUserId,
                ContainsPrivateData = isPrivate,
                DataName = originalName,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream" : file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            if (isPrivate)
            {
                // Privat: verschlüsselt in der DB ablegen.
                attachment.DatenBase64 = FileCrypto.Encrypt(bytes, EncryptionKey);
                attachment.DirectoryPath = string.Empty;
            }
            else
            {
                // Öffentlich: als Datei im Projekt-Unterordner speichern.
                var relDir = Path.Combine(StorageFolder, ticketId.ToString());
                var absDir = Path.Combine(_env.ContentRootPath, relDir);
                Directory.CreateDirectory(absDir);

                var safeName = $"{Guid.NewGuid():N}_{originalName}";
                var relPath = Path.Combine(relDir, safeName);
                var absPath = Path.Combine(_env.ContentRootPath, relPath);
                await System.IO.File.WriteAllBytesAsync(absPath, bytes);

                attachment.DirectoryPath = relPath;
                attachment.DatenBase64 = string.Empty;
            }

            _context.TicketAttachments.Add(attachment);
            await _context.SaveChangesAsync();

            var email = await _context.Users
                .Where(u => u.Id == CurrentUserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync() ?? string.Empty;

            return Ok(new AttachmentResponseDto
            {
                Id = attachment.Id,
                TicketId = attachment.TicketId,
                DataName = attachment.DataName,
                ContentType = attachment.ContentType,
                FileSize = attachment.FileSize,
                ContainsPrivateData = attachment.ContainsPrivateData,
                UploadedByUserId = attachment.UploadedByUserId,
                UploadedByEmail = email,
                CreatedAt = attachment.CreatedAt
            });
        }

        // GET {attachmentId}/download -> Datei herunterladen.
        [HttpGet("{attachmentId}/download")]
        public async Task<IActionResult> Download(int ticketId, int attachmentId)
        {
            if (!await DarfTicketSehen(ticketId))
                return Forbid();

            var att = await _context.TicketAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId);
            if (att == null) return NotFound();

            byte[] bytes;
            if (att.ContainsPrivateData)
            {
                // Entschlüsseln.
                bytes = FileCrypto.Decrypt(att.DatenBase64, EncryptionKey);
            }
            else
            {
                var absPath = Path.Combine(_env.ContentRootPath, att.DirectoryPath);
                if (!System.IO.File.Exists(absPath))
                    return NotFound("Datei nicht mehr vorhanden.");
                bytes = await System.IO.File.ReadAllBytesAsync(absPath);
            }

            return File(bytes, att.ContentType, att.DataName);
        }

        // DELETE {attachmentId} -> Anhang löschen.
        // Staff darf alle, sonst nur der Hochlader.
        [HttpDelete("{attachmentId}")]
        public async Task<IActionResult> Delete(int ticketId, int attachmentId)
        {
            var att = await _context.TicketAttachments
                .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TicketId == ticketId);
            if (att == null) return NotFound();

            if (!IsStaff && att.UploadedByUserId != CurrentUserId)
                return Forbid();

            // Bei öffentlicher Datei auch von der Platte entfernen.
            if (!att.ContainsPrivateData && !string.IsNullOrEmpty(att.DirectoryPath))
            {
                var absPath = Path.Combine(_env.ContentRootPath, att.DirectoryPath);
                if (System.IO.File.Exists(absPath))
                    System.IO.File.Delete(absPath);
            }

            _context.TicketAttachments.Remove(att);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
