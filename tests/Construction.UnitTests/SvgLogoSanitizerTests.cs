using System.Text;
using Construction.Application.Features.Branches;

namespace Construction.UnitTests;

/// <summary>An SVG logo is stored and served only after everything that could run or reach outside is removed.</summary>
public class SvgLogoSanitizerTests
{
    private static string Clean(string svg)
    {
        Assert.True(SvgLogoSanitizer.TrySanitize(new MemoryStream(Encoding.UTF8.GetBytes(svg)), out var output));

        return Encoding.UTF8.GetString(output);
    }

    private const string Open = "<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" viewBox=\"0 0 10 10\">";

    [Fact]
    public void A_plain_drawing_is_kept()
    {
        var result = Clean(Open + "<g><rect x=\"1\" y=\"1\" width=\"8\" height=\"8\" fill=\"#3457d5\"/><path d=\"M0 0L5 5\" stroke=\"#fff\"/></g></svg>");

        Assert.Contains("<rect", result);
        Assert.Contains("<path", result);
        Assert.Contains("#3457d5", result);
    }

    [Fact]
    public void Scripts_and_event_handlers_are_removed()
    {
        var result = Clean(Open + "<script>alert(1)</script><rect width=\"5\" height=\"5\" onclick=\"alert(1)\" onload=\"x()\"/></svg>");

        Assert.DoesNotContain("script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<rect", result);
    }

    [Fact]
    public void Foreign_objects_and_other_namespaces_are_removed()
    {
        var result = Clean(Open + "<foreignObject><div xmlns=\"http://www.w3.org/1999/xhtml\">x</div></foreignObject><rect width=\"5\" height=\"5\"/></svg>");

        Assert.DoesNotContain("foreignObject", result);
        Assert.DoesNotContain("xhtml", result);
    }

    [Fact]
    public void References_outside_the_drawing_are_removed_but_internal_ones_stay()
    {
        var result = Clean(Open
            + "<defs><linearGradient id=\"g\"><stop offset=\"0\" stop-color=\"#000\"/></linearGradient></defs>"
            + "<rect width=\"5\" height=\"5\" fill=\"url(#g)\"/>"
            + "<use xlink:href=\"https://evil.example/x.svg#a\"/>"
            + "<use href=\"#g\"/>"
            + "<image href=\"https://evil.example/p.png\"/>"
            + "<a href=\"javascript:alert(1)\"><rect width=\"1\" height=\"1\"/></a></svg>");

        Assert.Contains("url(#g)", result);
        Assert.Contains("href=\"#g\"", result);
        Assert.DoesNotContain("evil.example", result);
        Assert.DoesNotContain("javascript", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_stylesheet_that_reaches_outside_is_removed_and_a_harmless_one_stays()
    {
        var harmful = Clean(Open + "<style>@import url(https://evil.example/a.css); .a{fill:red}</style><rect class=\"a\" width=\"5\" height=\"5\"/></svg>");
        var harmless = Clean(Open + "<style>.a{fill:#3457d5}</style><rect class=\"a\" width=\"5\" height=\"5\"/></svg>");

        Assert.DoesNotContain("evil.example", harmful);
        Assert.DoesNotContain("<style", harmful);
        Assert.Contains("<style", harmless);
    }

    [Fact]
    public void An_embedded_picture_stays_only_when_it_is_a_raster_image()
    {
        var ok = Clean(Open + "<image href=\"data:image/png;base64,iVBORw0KGgo=\" width=\"4\" height=\"4\"/></svg>");
        var bad = Clean(Open + "<image href=\"data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=\" width=\"4\" height=\"4\"/></svg>");

        Assert.Contains("data:image/png", ok);
        Assert.DoesNotContain("svg+xml", bad);
    }

    [Fact]
    public void A_document_type_with_entities_does_not_expand()
    {
        var svg = "<?xml version=\"1.0\"?><!DOCTYPE svg [<!ENTITY a \"aaaaaaaaaa\">]>" + Open + "<rect width=\"5\" height=\"5\"/></svg>";

        Assert.True(SvgLogoSanitizer.TrySanitize(new MemoryStream(Encoding.UTF8.GetBytes(svg)), out var output));
        Assert.DoesNotContain("aaaaaaaaaa", Encoding.UTF8.GetString(output));
    }

    [Theory]
    [InlineData("not xml at all")]
    [InlineData("<html><body/></html>")]
    [InlineData("")]
    public void Something_that_is_not_an_svg_is_refused(string text)
    {
        Assert.False(SvgLogoSanitizer.TrySanitize(new MemoryStream(Encoding.UTF8.GetBytes(text)), out _));
    }
}
