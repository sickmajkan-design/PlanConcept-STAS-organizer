using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Construction.Application.Features.Branches;

/// <summary>
/// Cleans an SVG logo so it can be stored and served without being able to run anything: no scripts,
/// no event handlers, no outside references, no embedded pages. What stays is the drawing.
/// </summary>
/// <remarks>
/// The picture is drawn through an image tag in the panel, where a browser never runs an SVG's
/// scripts, but the file is also served by its own address, so it is cleaned the same way anyway.
/// It is an allow-list: an element or attribute that is not named here is dropped, so a feature
/// added to SVG later is not trusted by default.
/// </remarks>
public static class SvgLogoSanitizer
{
    private const string SvgNamespace = "http://www.w3.org/2000/svg";
    private const string XlinkNamespace = "http://www.w3.org/1999/xlink";

    private static readonly HashSet<string> AllowedElements = new(StringComparer.Ordinal)
    {
        "svg", "g", "defs", "symbol", "use", "title", "desc", "style",
        "path", "rect", "circle", "ellipse", "line", "polyline", "polygon",
        "text", "tspan", "textPath", "image",
        "linearGradient", "radialGradient", "stop", "pattern", "clipPath", "mask", "marker",
        "filter", "feBlend", "feColorMatrix", "feComposite", "feFlood", "feGaussianBlur", "feMerge",
        "feMergeNode", "feOffset", "feDropShadow", "feMorphology", "feComponentTransfer",
        "feFuncA", "feFuncR", "feFuncG", "feFuncB",
    };

    private static readonly Regex UnsafeCss = new(
        @"@import|javascript:|expression\s*\(|behavior\s*:|-moz-binding|url\(\s*['""]?\s*(?!#|data:image/(png|jpe?g|webp);)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex DataImage = new(
        @"^data:image/(png|jpe?g|webp);base64,[A-Za-z0-9+/=\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>Cleans the SVG; false when it is not an SVG document at all or cannot be read.</summary>
    public static bool TrySanitize(Stream input, out byte[] output)
    {
        output = [];

        XDocument document;

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Ignore,
                XmlResolver = null,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = 4 * 1024 * 1024,
            };

            using var reader = XmlReader.Create(input, settings);
            document = XDocument.Load(reader, LoadOptions.None);
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            return false;
        }

        var root = document.Root;

        if (root is null || root.Name != XName.Get("svg", SvgNamespace))
        {
            return false;
        }

        Clean(root);

        // Declarations of other namespaces (editor extras) go with their elements; the two used stay.
        var clean = new XDocument(new XElement(root));
        output = Encoding.UTF8.GetBytes(clean.ToString(SaveOptions.DisableFormatting));

        return true;
    }

    private static void Clean(XElement element)
    {
        foreach (var child in element.Elements().ToList())
        {
            var allowed = child.Name.Namespace == SvgNamespace
                && AllowedElements.Contains(child.Name.LocalName)
                && IsSafeElement(child);

            if (!allowed)
            {
                child.Remove();
                continue;
            }

            Clean(child);
        }

        // Comments and processing instructions carry nothing a logo needs.
        foreach (var node in element.Nodes().Where(n => n is XComment or XProcessingInstruction).ToList())
        {
            node.Remove();
        }

        foreach (var attribute in element.Attributes().ToList())
        {
            if (!IsSafeAttribute(element, attribute))
            {
                attribute.Remove();
            }
        }
    }

    private static bool IsSafeElement(XElement element)
    {
        // A stylesheet is kept only when it reaches nothing outside the drawing.
        return element.Name.LocalName != "style" || !UnsafeCss.IsMatch(element.Value);
    }

    private static bool IsSafeAttribute(XElement owner, XAttribute attribute)
    {
        if (attribute.IsNamespaceDeclaration)
        {
            return attribute.Value is SvgNamespace or XlinkNamespace;
        }

        var ns = attribute.Name.Namespace;
        var name = attribute.Name.LocalName;

        if (ns != XNamespace.None && ns != XNamespace.Xml && ns.NamespaceName != XlinkNamespace)
        {
            return false;
        }

        if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = attribute.Value.Trim();

        if (name == "href")
        {
            // Inside the drawing, or a picture embedded in it (for an image only).
            return value.StartsWith('#')
                || (owner.Name.LocalName == "image" && DataImage.IsMatch(value));
        }

        if (name is "style" or "fill" or "stroke" or "filter" or "clip-path" or "mask" or "marker-start"
            or "marker-mid" or "marker-end" or "cursor")
        {
            return !UnsafeCss.IsMatch(value);
        }

        return !value.Contains("javascript:", StringComparison.OrdinalIgnoreCase);
    }
}
