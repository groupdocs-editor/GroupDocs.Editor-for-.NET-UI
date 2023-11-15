using GroupDocs.Editor.HtmlCss.Resources;
using GroupDocs.Editor.HtmlCss.Resources.Images;
using GroupDocs.Editor.HtmlCss.Resources.Textual;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Models.DocumentConvertor;
using GroupDocs.Editor.UI.Api.Models.Storage;
using GroupDocs.Editor.UI.Api.Services.Interfaces;

namespace GroupDocs.Editor.UI.Api.Services.Options;

internal sealed class StorageCallback : IHtmlSavingCallback
{
    private readonly IStorage _storage;
    private readonly StorageSubFile _metaFile;


    public StorageCallback(IStorage storage, StorageSubFile metaFile)
    {
        _storage = storage;
        _metaFile = metaFile;
    }

    public string SaveOneResource(IHtmlResource resource)
    {
        var response = _storage
            .SaveFile(new[] { new FileContent { FileName = resource.FilenameWithExtension, ResourceStream = resource.ByteContent } },
                _metaFile.DocumentCode, _metaFile.SubCode.ToString()).Result.FirstOrDefault();
        if (response is not { IsSuccess: true } || response.Response == null)
        {
            return string.Empty;
        }
        switch (resource)
        {
            case IImageResource _:
                _metaFile.Images.Add(response.Response);
                break;
            case HtmlCss.Resources.Audio.Mp3Audio _:
                _metaFile.Audios.Add(response.Response);
                break;
            case HtmlCss.Resources.Fonts.FontResourceBase _:
                _metaFile.Fonts.Add(response.Response);
                break;
            case CssText _:
                _metaFile.Stylesheets.Add(response.Response);
                break;
            case null:
                throw new ArgumentNullException(nameof(resource));
        }

        return response.Response.FileLink;
    }
}