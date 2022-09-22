namespace TestWebAssemblyDriver;

internal class Program
{
    static void Main(string[] args)
    {
        var compilerLogBytes = File.ReadAllBytes(@"C:\Users\dabarbet\source\repos\ConsoleApp8\msbuild.compilerlog");
        var response = OmniSharp.WebAssembly.Driver.Program.InitializeAsync(compilerLogBytes).Result;
        Console.WriteLine(response);
    }
}
