namespace GroupDocs.Editor.UI.Api.Test.Helpers;

public class TestFile : PathEnumeration
{
    public string Extension { get; }

    public TestFile(TestFolder? parent, string name, string extension) : base(parent, $"{name}.{extension}")
    {
        Extension = extension;
    }

    public string GetFileName() => Path.GetFileName(Name);

    /// <summary>
    /// Changes the extension of file name.
    /// </summary>
    /// <param name="ext">The ext.</param>
    /// <returns>Replaced file name, with new extension.</returns>
    public string ChangeExtension(string ext)
    {
        return $"{Path.GetFileNameWithoutExtension(Name)}.{ext}";
    }

    public virtual Stream OpenFile()
    {
        var fileCopy = Path.GetTempFileName();
        var path = CreatePath();
        if (!File.Exists(path))
        {
            return Stream.Null;
        }
        File.Copy(path, fileCopy, true);
        return File.Open(fileCopy, FileMode.Open);
    }

    public static TestFile WordProcessing = new(TestFolder.TestFiles, nameof(WordProcessing), "docx");
    public static TestFile Spreadsheet = new(TestFolder.TestFiles, nameof(Spreadsheet), "xlsx");
    public static TestFile Presentation = new(TestFolder.TestFiles, nameof(Presentation), "pptx");
    public static TestFile Pdf = new(TestFolder.TestFiles, nameof(Pdf), "pdf");
}