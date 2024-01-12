using GroupDocs.Editor.HtmlCss.Resources;
using GroupDocs.Editor.HtmlCss.Resources.Images;
using GroupDocs.Editor.HtmlCss.Resources.Textual;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Interfaces;

namespace GroupDocs.Editor.UI.Api.Services.Options;

internal sealed class StorageCallback<TEditOptions> : IHtmlSavingCallback where TEditOptions : IEditOptions
{
    private readonly IStorage _storage;
    private readonly StorageSubFile<TEditOptions> _metaFile;


    public StorageCallback(IStorage storage, StorageSubFile<TEditOptions> metaFile)
    {
        _storage = storage;
        _metaFile = metaFile;
    }

    public string SaveOneResource(IHtmlResource resource)
    {
        ResourceType type;
        switch (resource)
        {
            case IImageResource _:
                type = ResourceType.Image;
                break;
            case HtmlCss.Resources.Audio.Mp3Audio _:
                type = ResourceType.Audio;
                break;
            case HtmlCss.Resources.Fonts.FontResourceBase _:
                type = ResourceType.Font;
                break;
            case CssText _:
                type = ResourceType.Stylesheet;
                break;
            default:
                type = ResourceType.Image;
                break;
        }
        var response = _storage
            .SaveFile(new[] { new FileContent { FileName = resource.FilenameWithExtension, ResourceStream = resource.ByteContent, ResourceType = type } },
                _metaFile.DocumentCode, _metaFile.SubCode).Result.FirstOrDefault();
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return string.Empty;
        }
        _metaFile.Resources.Add(response.Response.FileName, response.Response);
        return response.Response.FileLink;
    }
}