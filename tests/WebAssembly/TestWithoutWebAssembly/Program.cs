﻿using OmniSharp.Services;
using OmniSharp.WebAssembly;

namespace TestWebAssemblyDriver;

internal class Program
{
    static async Task Main(string[] args)
    {
        var result = CompilerLoggerPathRemapper.Remap(@"c:\workspace\a\b\test.txt", @"C:\workspace\", @"/home/workspace");

        var compilerLogBytes = File.ReadAllBytes(@"/home/fred/Downloads/msbuild.compilerlog");
        var response = await OmniSharp.WebAssembly.Program.InitializeAsync(compilerLogBytes, "/home/fred/git/wasm-test", Console.In, new SharedTextWriter(Console.Out));
        Console.WriteLine(response);

        await OmniSharp.WebAssembly.Program.InvokeRequestAsync("""
{
  "Type": "request",
  "Seq": 73,
  "Command": "/quickinfo",
  "Arguments": {
    "FileName": "/home/fred/git/wasm-test/Program.cs",
    "Line": 1,
    "Column": 32
  }
}
""");

    }
}
