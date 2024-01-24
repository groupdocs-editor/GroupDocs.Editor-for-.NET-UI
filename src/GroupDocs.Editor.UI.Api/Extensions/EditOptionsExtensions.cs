using GroupDocs.Editor.Options;

namespace GroupDocs.Editor.UI.Api.Extensions;

public static class EditOptionsExtensions
{
    public static bool IsOptionsEquals(this PresentationEditOptions? target, PresentationEditOptions? presentationEditOptions)
    {
        return (presentationEditOptions == null && target == null) ||
               presentationEditOptions != null && target != null &&
               target.ShowHiddenSlides == presentationEditOptions.ShowHiddenSlides &&
               target.SlideNumber == presentationEditOptions.SlideNumber;
    }

    public static bool IsOptionsEquals(this WordProcessingEditOptions? target, WordProcessingEditOptions? editOptions)
    {
        return (editOptions == null && target == null) ||
               editOptions != null && target != null &&
               target.EnableLanguageInformation == editOptions.EnableLanguageInformation &&
               target.EnablePagination == editOptions.EnablePagination &&
               target.ExtractOnlyUsedFont == editOptions.ExtractOnlyUsedFont &&
               target.UseInlineStyles == editOptions.UseInlineStyles &&
               target.FontExtraction == editOptions.FontExtraction &&
               (target.InputControlsClassName == null || target.InputControlsClassName.Equals(editOptions.InputControlsClassName, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool IsOptionsEquals(this PdfEditOptions? target, PdfEditOptions? editOptions)
    {
        return (editOptions == null && target == null) ||
               editOptions != null && target != null &&
               target.EnablePagination == editOptions.EnablePagination &&
               target.Pages.Equals(editOptions.Pages) &&
               target.SkipImages == editOptions.SkipImages;
    }
    public static bool IsOptionsEquals(this SpreadsheetEditOptions? target, SpreadsheetEditOptions? editOptions)
    {
        return (editOptions == null && target == null) ||
               editOptions != null && target != null &&
               target.WorksheetIndex == editOptions.WorksheetIndex &&
               target.ExcludeHiddenWorksheets == editOptions.ExcludeHiddenWorksheets;
    }
}