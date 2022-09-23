using System.ComponentModel.DataAnnotations;

namespace TestWithWebAssembly.Client;

public class InputJsonRequest
{
    [Required]
    public string? RequestJson { get; set; }
}
