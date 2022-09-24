using System;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using OmniSharp.Eventing;
using OmniSharp.Services;
using System.Threading.Tasks;
using System.Linq;
using DotNetJS;
using OmniSharp.Stdio.Logging;

namespace OmniSharp.WebAssembly.Driver
{
    public partial class Program
    {
        private static readonly Stream _inputStream = new MemoryStream();
        private static readonly OutputWriter _outputWriter = new OutputWriter();
        private static readonly ILoggerProvider _loggerProvider = new StdioLoggerProvider(_outputWriter);

        public static void Main(string[] args)
        {
        }

        [JSInvokable]
        public static void HandleRequest(string request)
        {
            _ = Task.Run(() => WebAssembly.Program.InvokeRequestAsync(request));
        }

        [JSInvokable]
        public static async Task<string> InitializeAsync(byte[] compilerLogBytes, string workspaceBasePath)
        {
            return await WebAssembly.Program.InitializeAsync(compilerLogBytes, workspaceBasePath, new StreamReader(_inputStream), _outputWriter, _loggerProvider);
        }

        class OutputWriter : ISharedTextWriter
        {
            public void WriteLine(object value)
            {
                Program.OnWriteLine(value.ToString());
            }
        }

        [JSEvent]
        public static partial void OnWriteLine(string line);
    }
}
