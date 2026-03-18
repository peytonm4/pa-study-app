using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using StudyApp.Api.Extraction;
using StudyApp.Worker.Extraction;
using A = DocumentFormat.OpenXml.Drawing;

namespace StudyApp.Api.Tests.Extraction;

public class PptxExtractorTests
{
    private readonly IPptxExtractor _extractor = new PptxExtractor();

    // Creates a minimal in-memory PPTX with slides specified as (bodyText, notesText?) tuples
    private static MemoryStream CreatePptx(params (string body, string? notes)[] slides)
    {
        var ms = new MemoryStream();
        using (var ppt = PresentationDocument.Create(ms, PresentationDocumentType.Presentation, autoSave: true))
        {
            var pp = ppt.AddPresentationPart();
            pp.Presentation = new Presentation
            {
                SlideIdList = new SlideIdList(),
                SlideSize = new SlideSize { Cx = 9144000, Cy = 6858000 },
                NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 },
            };

            uint slideId = 256;
            foreach (var (body, notes) in slides)
            {
                var slidePart = pp.AddNewPart<SlidePart>();
                slidePart.Slide = BuildSlide(body);
                slidePart.Slide.Save();

                if (notes != null)
                {
                    var notesPart = slidePart.AddNewPart<NotesSlidePart>();
                    notesPart.NotesSlide = BuildNotesSlide(notes);
                    notesPart.NotesSlide.Save();
                }

                var relId = pp.GetIdOfPart(slidePart);
                pp.Presentation.SlideIdList!.AppendChild(
                    new SlideId { Id = slideId++, RelationshipId = relId });
            }

            pp.Presentation.Save();
        }

        ms.Position = 0;
        return ms;
    }

    private static Slide BuildSlide(string bodyText)
    {
        var nvGroupProps = new NonVisualGroupShapeProperties(
            new NonVisualDrawingProperties { Id = 1U, Name = "" },
            new NonVisualGroupShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties());

        var groupShapeProps = new GroupShapeProperties(new A.TransformGroup());

        var nvShapeProps = new NonVisualShapeProperties(
            new NonVisualDrawingProperties { Id = 2U, Name = "Title" },
            new NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties(new PlaceholderShape()));

        var textBody = new TextBody(
            new A.BodyProperties(),
            new A.ListStyle(),
            new A.Paragraph(new A.Run(new A.Text(bodyText))));

        var shape = new Shape(nvShapeProps, new ShapeProperties(), textBody);
        var shapeTree = new ShapeTree(nvGroupProps, groupShapeProps, shape);
        var commonData = new CommonSlideData(shapeTree);

        return new Slide(commonData, new ColorMapOverride(new A.MasterColorMapping()));
    }

    private static NotesSlide BuildNotesSlide(string notesText)
    {
        var nvGroupProps = new NonVisualGroupShapeProperties(
            new NonVisualDrawingProperties { Id = 1U, Name = "" },
            new NonVisualGroupShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties());

        var groupShapeProps = new GroupShapeProperties(new A.TransformGroup());

        var nvShapeProps = new NonVisualShapeProperties(
            new NonVisualDrawingProperties { Id = 2U, Name = "Notes" },
            new NonVisualShapeDrawingProperties(),
            new ApplicationNonVisualDrawingProperties(
                new PlaceholderShape { Type = PlaceholderValues.Body }));

        var textBody = new TextBody(
            new A.BodyProperties(),
            new A.ListStyle(),
            new A.Paragraph(new A.Run(new A.Text(notesText))));

        var shape = new Shape(nvShapeProps, new ShapeProperties(), textBody);
        var shapeTree = new ShapeTree(nvGroupProps, groupShapeProps, shape);
        var commonData = new CommonSlideData(shapeTree);

        return new NotesSlide(commonData, new ColorMapOverride(new A.MasterColorMapping()));
    }

    [Fact]
    public void ExtractSlides_ReturnsSlideBodyText()
    {
        using var stream = CreatePptx(("Hello World", null));

        var slides = _extractor.Extract(stream, "test.pptx").ToList();

        Assert.Single(slides);
        Assert.Contains("Hello World", slides[0].BodyText);
    }

    [Fact]
    public void ExtractSlides_ReturnsSpeakerNotes()
    {
        using var stream = CreatePptx(("Slide Title", "These are notes"));

        var slides = _extractor.Extract(stream, "test.pptx").ToList();

        Assert.Single(slides);
        Assert.Contains("These are notes", slides[0].NotesText);
    }

    [Fact]
    public void ExtractSlides_SetsCorrectSlideNumbers()
    {
        using var stream = CreatePptx(("Slide One", null), ("Slide Two", null));

        var slides = _extractor.Extract(stream, "test.pptx").ToList();

        Assert.Equal(2, slides.Count);
        Assert.Equal(1, slides[0].SlideNumber);
        Assert.Equal(2, slides[1].SlideNumber);
    }

    [Fact]
    public void ExtractSlides_ReturnsFileName()
    {
        using var stream = CreatePptx(("Content", null));

        var slides = _extractor.Extract(stream, "lecture.pptx").ToList();

        Assert.All(slides, s => Assert.Equal("lecture.pptx", s.FileName));
    }
}
