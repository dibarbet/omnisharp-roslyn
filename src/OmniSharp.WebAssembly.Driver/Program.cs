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

namespace OmniSharp.WebAssembly.Driver
{
    public class Program
    {

        public static void Main(string[] args)
        {
        }

        [JSInvokable]
        public static async Task<string> InitializeAsync(byte[] compilerLogBytes)
        {
            return await Host.InitializeAsync(compilerLogBytes);
        }
    }
}
