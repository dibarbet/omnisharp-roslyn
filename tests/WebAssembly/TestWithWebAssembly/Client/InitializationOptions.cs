using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Components.Forms;

namespace TestWithWebAssembly.Client;

public class InitializationOptions
{
    [Required]
    public string? WorkspacePath { get; set; }

    [Required]
    public IBrowserFile? CompilerLog { get; set; }
}
