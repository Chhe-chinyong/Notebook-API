using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotebookApi.Data;
using NotebookApi.Models;
using NotebookApi.Models.Notes;

namespace NotebookApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly INoteRepository _noteRepository;

    public NotesController(INoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    private int GetUserId()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? throw new UnauthorizedAccessException("User ID not found in token");
        
        if (!int.TryParse(userIdString, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user ID format");
        }
        
        return userId;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
    {
        try
        {
            var userId = GetUserId();
            var notes = await _noteRepository.GetByUserIdAsync(userId);
            return Ok(notes);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching notes" });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Note>> GetNote(string id)
    {
        try
        {
            var userId = GetUserId();
            var note = await _noteRepository.GetByIdAsync(id, userId);
            
            if (note == null)
            {
                return NotFound(new { message = "Note not found" });
            }

            return Ok(note);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while fetching the note" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<Note>> CreateNote([FromBody] CreateNoteRequest request)
    {
        try
        {
            var userId = GetUserId();
            var note = new Note
            {
                Id = Guid.NewGuid().ToString(),
                Title = request.Title,
                Content = request.Content ?? string.Empty,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _noteRepository.CreateAsync(note);
            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while creating the note" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Note>> UpdateNote(string id, [FromBody] UpdateNoteRequest request)
    {
        try
        {
            var userId = GetUserId();
            var existingNote = await _noteRepository.GetByIdAsync(id, userId);
            
            if (existingNote == null)
            {
                return NotFound(new { message = "Note not found" });
            }

            // Update only provided fields
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                existingNote.Title = request.Title;
            }
            if (request.Content != null)
            {
                existingNote.Content = request.Content;
            }
            existingNote.UpdatedAt = DateTime.UtcNow;

            var updated = await _noteRepository.UpdateAsync(existingNote);
            if (!updated)
            {
                return StatusCode(500, new { message = "Failed to update note" });
            }

            return Ok(existingNote);
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while updating the note" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(string id)
    {
        try
        {
            var userId = GetUserId();
            var deleted = await _noteRepository.DeleteAsync(id, userId);
            
            if (!deleted)
            {
                return NotFound(new { message = "Note not found" });
            }

            return NoContent();
        }
        catch (Exception)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the note" });
        }
    }
}
