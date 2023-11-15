namespace GroupDocs.Editor.UI.Api.Test.Helpers;

public class TestFolder : PathEnumeration
{
    public static TestFolder TestFiles = new(null, nameof(TestFiles));

    public TestFolder(TestFolder? parent, string name) : base(parent, name)
    {
    }
}