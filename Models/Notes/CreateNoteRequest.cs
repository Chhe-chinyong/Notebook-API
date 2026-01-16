using System.ComponentModel.DataAnnotations;

namespace NotebookApi.Models.Notes;

public class CreateNoteRequest
{
    [Required]
    [MinLength(1)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }
}
