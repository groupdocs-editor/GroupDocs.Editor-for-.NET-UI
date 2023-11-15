using System.Reflection;

namespace GroupDocs.Editor.UI.Api.Test.Helpers;

public abstract class PathEnumeration : IComparable
{
    public string Name { get; }

    public PathEnumeration? Parent { get; }

    protected PathEnumeration(PathEnumeration? parent, string name) => (Parent, Name) = (parent, name);

    public IEnumerable<string> ParentPathList()
    {
        var parent = Parent;
        while (parent != null)
        {
            var name = parent.Name;
            parent = parent.Parent;
            yield return name;
        }
        yield return Name;
    }

    public string CreatePath()
    {
        var lists = ParentPathList().ToArray();
        return Path.Combine(lists);
    }

    public override string ToString() => Name;

    public static IEnumerable<T> GetAll<T>() where T : PathEnumeration =>
        typeof(T).GetFields(BindingFlags.Public |
                            BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
            .Select(f => f.GetValue(null))
            .Cast<T>();

    public override bool Equals(object? obj)
    {
        if (obj is not PathEnumeration otherValue)
        {
            return false;
        }

        var typeMatches = GetType() == obj.GetType();
        var valueMatches = Name.Equals(otherValue.Name);
        var parentMatches = Parent != null && otherValue.Parent != null && Parent.Equals(otherValue.Parent);

        return typeMatches && valueMatches && parentMatches;
    }

    protected bool Equals(PathEnumeration other)
    {
        return other.Parent != null && Parent != null && Name == other.Name && Parent.Equals(other.Parent);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Parent);
    }

    public int CompareTo(object other) => string.Compare(Name, ((PathEnumeration)other).Name, StringComparison.Ordinal);

    // Other utility methods ...
}