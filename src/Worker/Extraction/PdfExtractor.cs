namespace StudyApp.Worker.Extraction;

public class PdfExtractor : IPdfExtractor
{
    public IEnumerable<PageContent> Extract(Stream pdfStream, string fileName)
        => throw new NotImplementedException();
}
