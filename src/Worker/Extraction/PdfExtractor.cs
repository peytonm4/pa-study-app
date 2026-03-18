using StudyApp.Api.Extraction;
using UglyToad.PdfPig;

namespace StudyApp.Worker.Extraction;

public class PdfExtractor : IPdfExtractor
{
    public IEnumerable<PageContent> Extract(Stream pdfStream, string fileName)
    {
        // Copy to byte array — PdfDocument.Open(stream) requires a seekable stream or byte[]
        byte[] bytes;
        using (var ms = new MemoryStream())
        {
            pdfStream.CopyTo(ms);
            bytes = ms.ToArray();
        }

        using var document = PdfDocument.Open(bytes);
        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            var text = words.Any()
                ? string.Join(" ", words.Select(w => w.Text))
                : string.Empty;

            yield return new PageContent(page.Number, fileName, text, !words.Any());
        }
    }
}
