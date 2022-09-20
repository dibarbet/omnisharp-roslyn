using System;

using Microsoft.JSInterop;

namespace OmniSharp.WebAssembly.Driver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"hello from {typeof(Program).AssemblyQualifiedName}");
        }

        [JSInvokable] // The method is invoked from JavaScript.
        public static string GetName() => $"{nameof(GetName)} from Omnisharp";
    }
}
