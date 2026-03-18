using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using StudyApp.Api.Extraction;
using A = DocumentFormat.OpenXml.Drawing;

namespace StudyApp.Worker.Extraction;

public class PptxExtractor : IPptxExtractor
{
    public IEnumerable<SlideContent> Extract(Stream pptxStream, string fileName)
    {
        // Copy to MemoryStream to avoid ObjectDisposedException when the source stream closes
        var ms = new MemoryStream();
        pptxStream.CopyTo(ms);
        ms.Position = 0;

        using var ppt = PresentationDocument.Open(ms, isEditable: false);
        var presentationPart = ppt.PresentationPart!;
        var slideIds = presentationPart.Presentation.SlideIdList!.ChildElements;

        for (int i = 0; i < slideIds.Count; i++)
        {
            var relId = ((SlideId)slideIds[i]).RelationshipId!;
            var slidePart = (SlidePart)presentationPart.GetPartById(relId);

            var bodyText = string.Concat(
                slidePart.Slide.Descendants<A.Text>().Select(t => t.Text));

            var notesText = "";
            if (slidePart.NotesSlidePart is { } notesPart)
            {
                notesText = string.Concat(
                    notesPart.NotesSlide.Descendants<A.Text>().Select(t => t.Text));
            }

            yield return new SlideContent(i + 1, fileName, bodyText, notesText);
        }
    }
}
