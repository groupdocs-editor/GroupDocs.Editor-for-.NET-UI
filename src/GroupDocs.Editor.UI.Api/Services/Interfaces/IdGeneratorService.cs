namespace GroupDocs.Editor.UI.Api.Services.Interfaces;

public class IdGeneratorService : IIdGeneratorService
{
    public Guid GenerateDocumentCode() => Guid.NewGuid();

    public Guid GenerateEmptyDocumentCode() => Guid.Empty;
}