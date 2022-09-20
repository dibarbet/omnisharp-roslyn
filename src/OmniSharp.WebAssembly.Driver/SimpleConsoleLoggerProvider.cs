using System;

using Microsoft.Extensions.Logging;

namespace OmniSharp.WebAssembly.Driver
{
    /// <summary>
    /// The default console logger throws a platform not supported exception in wasm.
    /// This implements a super duper straightforward version that doesn't do anything tricky and works in wasm.
    /// </summary>
    class SimpleWasmConsoleLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName)
        {
            return new SimpleWasmConsoleLogger(categoryName);
        }

        public void Dispose()
        {
        }

        private class SimpleWasmConsoleLogger : ILogger
        {
            private readonly string _categoryName;
            public SimpleWasmConsoleLogger(string categoryName)
            {
                _categoryName = categoryName;
            }
            public IDisposable BeginScope<TState>(TState state)
            {
                return NullScope.Instance;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (eventId == default)
                {
                    Console.WriteLine($"[{logLevel}][{_categoryName}]{formatter(state, exception)}");
                }
                else
                {
                    Console.WriteLine($"[{logLevel}][{_categoryName}][{eventId}]{formatter(state, exception)}");
                }
            }

            private class NullScope : IDisposable
            {
                public static NullScope Instance => new();
                public void Dispose()
                {
                }
            }
        }
    }
}
