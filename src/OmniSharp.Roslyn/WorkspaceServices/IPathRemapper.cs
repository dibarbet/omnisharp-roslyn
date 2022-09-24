using System.Composition;

namespace OmniSharp;

public interface IPathRemapper
{
    string Remap(string path);
}

internal class EmptyRemapper : IPathRemapper
{
    public string Remap(string path) => path;
}
