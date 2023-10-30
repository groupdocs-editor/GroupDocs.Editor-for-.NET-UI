namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public interface IIdGeneratorService
{
    public Guid GenerateDocumentCode();
    public Guid GenerateEmptyDocumentCode();
}