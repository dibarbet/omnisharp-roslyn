namespace TestWebAssemblyDriver;

internal class Program
{
    static void Main(string[] args)
    {
        OmniSharp.WebAssembly.Driver.Program.InitializeAsync().Wait();
    }
}
