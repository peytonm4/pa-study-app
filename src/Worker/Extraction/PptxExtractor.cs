namespace StudyApp.Worker.Extraction;

public class PptxExtractor : IPptxExtractor
{
    public IEnumerable<SlideContent> Extract(Stream pptxStream, string fileName)
        => throw new NotImplementedException();
}
