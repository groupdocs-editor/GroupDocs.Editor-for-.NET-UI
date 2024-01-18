using FluentAssertions;
using GroupDocs.Editor.Options;
using GroupDocs.Editor.UI.Api.Extensions;

namespace GroupDocs.Editor.UI.Api.Test.Extensions;

public class EditOptionsExtensionsTests
{
    [Fact]
    public void PresentationEditOptions()
    {
        // Act
        var result = new PresentationEditOptions().IsOptionsEquals(null);
        result.Should().BeFalse();
        result = (null as PresentationEditOptions).IsOptionsEquals(null);
        result.Should().BeTrue();
        result = new PresentationEditOptions { ShowHiddenSlides = false, SlideNumber = 1 }.IsOptionsEquals(new PresentationEditOptions { ShowHiddenSlides = false, SlideNumber = 1 });
        result.Should().BeTrue();
        result = new PresentationEditOptions { ShowHiddenSlides = true, SlideNumber = 1 }.IsOptionsEquals(new PresentationEditOptions { ShowHiddenSlides = false, SlideNumber = 1 });
        result.Should().BeFalse();
    }
}