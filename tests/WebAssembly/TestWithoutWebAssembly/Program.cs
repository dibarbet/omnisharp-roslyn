using OmniSharp.Services;

namespace TestWebAssemblyDriver;

internal class Program
{
    static void Main(string[] args)
    {
        var compilerLogBytes = File.ReadAllBytes(@"C:\Users\dabarbet\Downloads\msbuild.compilerlog");
        var response = OmniSharp.WebAssembly.Program.InitializeAsync(compilerLogBytes, Console.In, new SharedTextWriter(Console.Out)).Result;
        Console.WriteLine(response);
    }
}
