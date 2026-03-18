using StudyApp.Api.Extraction;
using StudyApp.Worker.Extraction;

namespace StudyApp.Api.Tests.Extraction;

public class PdfExtractorTests
{
    private readonly IPdfExtractor _extractor = new PdfExtractor();

    // Minimal valid 1-page blank PDF (~300 bytes, no content stream = no text layer)
    private static byte[] BlankPdfBytes() => """
        %PDF-1.4
        1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
        2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
        3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj
        xref
        0 4
        0000000000 65535 f
        0000000009 00000 n
        0000000058 00000 n
        0000000115 00000 n
        trailer<</Size 4/Root 1 0 R>>
        startxref
        190
        %%EOF
        """u8.ToArray();

    // Minimal valid 1-page PDF with a text layer (BT...ET content stream)
    private static byte[] TextPdfBytes()
    {
        // Build a PDF with a content stream containing a text object
        const string content = "BT /F1 12 Tf 100 700 Td (Hello PDF) Tj ET";
        var contentBytes = System.Text.Encoding.Latin1.GetBytes(content);
        var contentLength = contentBytes.Length;

        var header = "%PDF-1.4\n";
        var obj1 = "1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n";
        var obj2 = "2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj\n";
        var obj3 = $"3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R/Contents 4 0 R/Resources<</Font<</F1<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>>>>>>>>endobj\n";
        var obj4Header = $"4 0 obj<</Length {contentLength}>>\nstream\n";
        var obj4Footer = "\nendstream\nendobj\n";

        // Calculate offsets
        var off1 = header.Length;
        var off2 = off1 + obj1.Length;
        var off3 = off2 + obj2.Length;
        var off4 = off3 + obj3.Length;

        var xref = $"xref\n0 5\n0000000000 65535 f \n{off1:D10} 00000 n \n{off2:D10} 00000 n \n{off3:D10} 00000 n \n{off4:D10} 00000 n \n";
        var startxref = off4 + obj4Header.Length + contentLength + obj4Footer.Length;
        var trailer = $"trailer<</Size 5/Root 1 0 R>>\nstartxref\n{startxref}\n%%EOF";

        var parts = new List<byte[]>
        {
            System.Text.Encoding.Latin1.GetBytes(header),
            System.Text.Encoding.Latin1.GetBytes(obj1),
            System.Text.Encoding.Latin1.GetBytes(obj2),
            System.Text.Encoding.Latin1.GetBytes(obj3),
            System.Text.Encoding.Latin1.GetBytes(obj4Header),
            contentBytes,
            System.Text.Encoding.Latin1.GetBytes(obj4Footer),
            System.Text.Encoding.Latin1.GetBytes(xref),
            System.Text.Encoding.Latin1.GetBytes(trailer),
        };

        return parts.SelectMany(b => b).ToArray();
    }

    private static MemoryStream ToStream(byte[] bytes)
    {
        var ms = new MemoryStream(bytes);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void ExtractPages_ReturnsTextFromTextLayerPage()
    {
        using var stream = ToStream(TextPdfBytes());

        var pages = _extractor.Extract(stream, "test.pdf").ToList();

        Assert.Single(pages);
        Assert.False(string.IsNullOrWhiteSpace(pages[0].Text));
        Assert.False(pages[0].NeedsVision);
    }

    [Fact]
    public void ExtractPages_FlagsBlankPageForVision()
    {
        using var stream = ToStream(BlankPdfBytes());

        var pages = _extractor.Extract(stream, "test.pdf").ToList();

        Assert.Single(pages);
        Assert.Equal(string.Empty, pages[0].Text);
        Assert.True(pages[0].NeedsVision);
    }

    [Fact]
    public void ExtractPages_SetsCorrectPageNumbers()
    {
        // Use two blank pages — simplest way to get multi-page PDF
        var twoPagePdf = BuildTwoBlankPagePdf();
        using var stream = ToStream(twoPagePdf);

        var pages = _extractor.Extract(stream, "test.pdf").ToList();

        Assert.Equal(2, pages.Count);
        Assert.Equal(1, pages[0].PageNumber);
        Assert.Equal(2, pages[1].PageNumber);
    }

    [Fact]
    public void ExtractPages_ReturnsFileName()
    {
        using var stream = ToStream(BlankPdfBytes());

        var pages = _extractor.Extract(stream, "lecture.pdf").ToList();

        Assert.All(pages, p => Assert.Equal("lecture.pdf", p.FileName));
    }

    private static byte[] BuildTwoBlankPagePdf()
    {
        var raw = """
            %PDF-1.4
            1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
            2 0 obj<</Type/Pages/Kids[3 0 R 4 0 R]/Count 2>>endobj
            3 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj
            4 0 obj<</Type/Page/MediaBox[0 0 612 792]/Parent 2 0 R>>endobj
            xref
            0 5
            0000000000 65535 f
            0000000009 00000 n
            0000000058 00000 n
            0000000115 00000 n
            0000000178 00000 n
            trailer<</Size 5/Root 1 0 R>>
            startxref
            241
            %%EOF
            """;
        return System.Text.Encoding.Latin1.GetBytes(raw);
    }
}
