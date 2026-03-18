namespace StudyApp.Api.Extraction;

public record PageContent(int PageNumber, string FileName, string Text, bool NeedsVision);

public interface IPdfExtractor
{
    IEnumerable<PageContent> Extract(Stream pdfStream, string fileName);
}
