namespace StudyApp.Worker.Extraction;

public record SlideContent(int SlideNumber, string FileName, string BodyText, string NotesText);

public interface IPptxExtractor
{
    IEnumerable<SlideContent> Extract(Stream pptxStream, string fileName);
}
