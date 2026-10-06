using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace PresalesGPT.Api.Services;

public class SlideStatContent
{
    public string Value { get; set; } = "";

    public string Label { get; set; } = "";
}

public class SlideColumnContent
{
    public string Heading { get; set; } = "";

    public List<string> Items { get; set; } = new();
}

public class SlideContent
{
    public string Layout { get; set; } = "Bullets";

    public string Title { get; set; } = "";

    public List<string> Bullets { get; set; } = new();

    public List<SlideStatContent> Stats { get; set; } = new();

    public List<SlideColumnContent> Columns { get; set; } = new();

    public string Callout { get; set; } = "";

    public string SpeakerNotes { get; set; } = "";

    public string SourceSection { get; set; } = "";
}

/// <summary>
/// Builds a .pptx from generated slide content.
///
/// Shapes are positioned explicitly rather than inheriting placeholders
/// from the slide layout. That keeps the master and layout minimal, at
/// the cost of doing the arithmetic here.
///
/// All measurements are English Metric Units: 914400 EMU to the inch.
/// </summary>
public class PowerPointBuilder
{
    private const long SlideWidth = 12192000;   // 16:9
    private const long SlideHeight = 6858000;

    private const long Margin = 838200;         // 0.92in
    private const long ContentWidth = SlideWidth - (Margin * 2);

    private const string Ink = "0F1E38";        // near-black navy
    private const string Accent = "3B6FF5";
    private const string Body = "37455C";
    private const string Muted = "8A97AB";
    private const string Paper = "FFFFFF";
    private const string Tint = "F2F6FD";
    private const string Rule = "DCE4F2";

    public byte[] Build(
        string deckTitle,
        string deckSubtitle,
        IReadOnlyList<SlideContent> slides)
    {
        using var stream = new MemoryStream();

        using (var document = PresentationDocument.Create(
                   stream,
                   PresentationDocumentType.Presentation))
        {
            var presentationPart = document.AddPresentationPart();
            presentationPart.Presentation = new Presentation();

            var masterPart = CreateSlideMasterPart(presentationPart);
            var layoutPart = CreateSlideLayoutPart(masterPart);

            var master = masterPart.SlideMaster;

            master.SlideLayoutIdList = new SlideLayoutIdList(
                new SlideLayoutId
                {
                    Id = 2147483649U,
                    RelationshipId =
                        masterPart.GetIdOfPart(layoutPart)
                });

            master.Save();

            var slideIdList = new SlideIdList();
            var slideId = 256U;

            AddSlide(
                presentationPart,
                layoutPart,
                slideIdList,
                ref slideId,
                BuildTitleSlide(deckTitle, deckSubtitle),
                notes: "");

            var lastSection = "";
            var number = 1;

            foreach (var slide in slides)
            {
                // A divider whenever the deck moves to a new part of the
                // proposal. Derived here rather than asked of the model:
                // it is a structural decision, not an editorial one.
                var section = slide.SourceSection ?? "";

                if (!string.IsNullOrWhiteSpace(section) &&
                    !string.Equals(
                        section,
                        lastSection,
                        StringComparison.OrdinalIgnoreCase))
                {
                    AddSlide(
                        presentationPart,
                        layoutPart,
                        slideIdList,
                        ref slideId,
                        BuildSectionSlide(section),
                        notes: "");

                    lastSection = section;
                }

                AddSlide(
                    presentationPart,
                    layoutPart,
                    slideIdList,
                    ref slideId,
                    BuildContentSlide(slide, number),
                    slide.SpeakerNotes);

                number++;
            }

            presentationPart.Presentation.SlideMasterIdList =
                new SlideMasterIdList(
                    new SlideMasterId
                    {
                        Id = 2147483648U,
                        RelationshipId =
                            presentationPart.GetIdOfPart(masterPart)
                    });

            presentationPart.Presentation.SlideIdList = slideIdList;

            presentationPart.Presentation.SlideSize =
                new SlideSize
                {
                    Cx = (int)SlideWidth,
                    Cy = (int)SlideHeight
                };

            presentationPart.Presentation.NotesSize =
                new NotesSize
                {
                    Cx = SlideHeight,
                    Cy = SlideWidth
                };

            presentationPart.Presentation.Save();
        }

        return stream.ToArray();
    }

    private void AddSlide(
        PresentationPart presentationPart,
        SlideLayoutPart layoutPart,
        SlideIdList slideIdList,
        ref uint slideId,
        Slide slide,
        string notes)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();

        slidePart.Slide = slide;
        slidePart.AddPart(layoutPart);
        slidePart.Slide.Save();

        if (!string.IsNullOrWhiteSpace(notes))
        {
            AddSpeakerNotes(slidePart, notes);
        }

        slideIdList.Append(new SlideId
        {
            Id = slideId,
            RelationshipId =
                presentationPart.GetIdOfPart(slidePart)
        });

        slideId++;
    }

    // ---------------------------------------------------------------
    // Slide types
    // ---------------------------------------------------------------

    private Slide BuildTitleSlide(string title, string subtitle)
    {
        var tree = NewShapeTree();
        uint id = 2;

        tree.Append(Block(id++, 0, 0, SlideWidth, SlideHeight, Ink));

        tree.Append(Block(
            id++,
            Margin,
            2150000,
            520000,
            72000,
            Accent));

        tree.Append(Text(
            id++,
            Margin,
            2380000,
            ContentWidth,
            1500000,
            new[] { Line(title, 4000, true, Paper) }));

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            tree.Append(Text(
                id++,
                Margin,
                3980000,
                ContentWidth,
                520000,
                new[] { Line(subtitle, 1500, false, "9FB2D4") }));
        }

        return Finish(tree);
    }

    private Slide BuildSectionSlide(string section)
    {
        var tree = NewShapeTree();
        uint id = 2;

        tree.Append(Block(id++, 0, 0, SlideWidth, SlideHeight, Ink));

        tree.Append(Block(
            id++,
            0,
            0,
            96000,
            SlideHeight,
            Accent));

        tree.Append(Text(
            id++,
            Margin,
            2900000,
            ContentWidth,
            1000000,
            new[] { Line(section, 3200, true, Paper) }));

        return Finish(tree);
    }

    private Slide BuildContentSlide(SlideContent slide, int number)
    {
        var tree = NewShapeTree();
        uint id = 2;

        tree.Append(Block(
            id++,
            0,
            0,
            SlideWidth,
            SlideHeight,
            Paper));

        tree.Append(Text(
            id++,
            Margin,
            620000,
            ContentWidth,
            900000,
            new[] { Line(slide.Title, 2600, true, Ink) }));

        tree.Append(Block(
            id++,
            Margin,
            1620000,
            460000,
            50000,
            Accent));

        switch (slide.Layout)
        {
            case "Stats":
                AppendStats(tree, ref id, slide.Stats);
                break;

            case "TwoColumn":
                AppendColumns(tree, ref id, slide.Columns);
                break;

            case "Callout":
                AppendCallout(tree, ref id, slide.Callout);
                break;

            default:
                AppendBullets(tree, ref id, slide.Bullets);
                break;
        }

        tree.Append(Block(
            id++,
            Margin,
            SlideHeight - 620000,
            ContentWidth,
            12700,
            Rule));

        tree.Append(Text(
            id++,
            Margin,
            SlideHeight - 540000,
            ContentWidth,
            320000,
            new[] { Line(number.ToString(), 1000, false, Muted) }));

        return Finish(tree);
    }

    // ---------------------------------------------------------------
    // Layout bodies
    // ---------------------------------------------------------------

    private void AppendBullets(
        ShapeTree tree,
        ref uint id,
        List<string> bullets)
    {
        var lines = bullets
            .Where(bullet => !string.IsNullOrWhiteSpace(bullet))
            .Select(bullet => Line(bullet, 1700, false, Body))
            .ToArray();

        if (lines.Length == 0)
        {
            return;
        }

        tree.Append(Text(
            id++,
            Margin,
            1980000,
            ContentWidth,
            SlideHeight - 2700000,
            lines,
            bulleted: true));
    }

    private void AppendStats(
        ShapeTree tree,
        ref uint id,
        List<SlideStatContent> stats)
    {
        var visible = stats.Take(4).ToList();

        if (visible.Count == 0)
        {
            return;
        }

        const long gap = 200000;
        const long top = 2150000;
        const long height = 1900000;

        var width =
            (ContentWidth - (gap * (visible.Count - 1)))
            / visible.Count;

        for (var index = 0; index < visible.Count; index++)
        {
            var x = Margin + ((width + gap) * index);

            tree.Append(Block(
                id++,
                x,
                top,
                width,
                height,
                Tint));

            tree.Append(Block(
                id++,
                x,
                top,
                width,
                40000,
                Accent));

            tree.Append(Text(
                id++,
                x + 180000,
                top + 340000,
                width - 360000,
                720000,
                new[] { Line(visible[index].Value, 3600, true, Ink) }));

            tree.Append(Text(
                id++,
                x + 180000,
                top + 1140000,
                width - 360000,
                600000,
                new[] { Line(visible[index].Label, 1250, false, Body) }));
        }
    }

    private void AppendColumns(
        ShapeTree tree,
        ref uint id,
        List<SlideColumnContent> columns)
    {
        var visible = columns.Take(2).ToList();

        if (visible.Count == 0)
        {
            return;
        }

        const long gap = 300000;
        const long top = 2050000;

        var height = SlideHeight - 2800000;

        var width =
            (ContentWidth - (gap * (visible.Count - 1)))
            / visible.Count;

        for (var index = 0; index < visible.Count; index++)
        {
            var x = Margin + ((width + gap) * index);

            tree.Append(Block(id++, x, top, width, height, Tint));

            tree.Append(Text(
                id++,
                x + 220000,
                top + 200000,
                width - 440000,
                420000,
                new[]
                {
                    Line(visible[index].Heading, 1500, true, Accent)
                }));

            var items = visible[index].Items
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => Line(item, 1400, false, Body))
                .ToArray();

            if (items.Length == 0)
            {
                continue;
            }

            tree.Append(Text(
                id++,
                x + 220000,
                top + 700000,
                width - 440000,
                height - 900000,
                items,
                bulleted: true));
        }
    }

    private void AppendCallout(
        ShapeTree tree,
        ref uint id,
        string callout)
    {
        if (string.IsNullOrWhiteSpace(callout))
        {
            return;
        }

        const long top = 2300000;
        const long height = 1800000;

        tree.Append(Block(
            id++,
            Margin,
            top,
            ContentWidth,
            height,
            Tint));

        tree.Append(Block(
            id++,
            Margin,
            top,
            60000,
            height,
            Accent));

        tree.Append(Text(
            id++,
            Margin + 420000,
            top + 420000,
            ContentWidth - 720000,
            height - 700000,
            new[] { Line(callout, 2200, false, Ink) }));
    }

    // ---------------------------------------------------------------
    // Shape helpers
    // ---------------------------------------------------------------

    private ShapeTree NewShapeTree()
    {
        return new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties
                {
                    Id = 1U,
                    Name = ""
                },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new D.TransformGroup()));
    }

    private Slide Finish(ShapeTree tree)
    {
        return new Slide(
            new CommonSlideData(tree),
            new ColorMapOverride(new D.MasterColorMapping()));
    }

    private P.Shape Block(
        uint id,
        long x,
        long y,
        long width,
        long height,
        string colourHex)
    {
        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties
                {
                    Id = id,
                    Name = $"Block{id}"
                },
                new P.NonVisualShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = x, Y = y },
                    new D.Extents { Cx = width, Cy = height }),
                new D.PresetGeometry(new D.AdjustValueList())
                {
                    Preset = D.ShapeTypeValues.Rectangle
                },
                new D.SolidFill(
                    new D.RgbColorModelHex { Val = colourHex })),
            new P.TextBody(
                new D.BodyProperties(),
                new D.ListStyle(),
                new D.Paragraph()));
    }

    private P.Shape Text(
        uint id,
        long x,
        long y,
        long width,
        long height,
        TextLine[] lines,
        bool bulleted = false)
    {
        var textBody = new P.TextBody(
            new D.BodyProperties
            {
                Wrap = D.TextWrappingValues.Square,
                LeftInset = 0,
                RightInset = 0,
                TopInset = 0,
                BottomInset = 0
            },
            new D.ListStyle());

        foreach (var line in lines)
        {
            var paragraphProperties = new D.ParagraphProperties();

            if (bulleted)
            {
                paragraphProperties.LeftMargin = 228600;
                paragraphProperties.Indent = -228600;

                paragraphProperties.Append(
                    new D.BulletColor(
                        new D.RgbColorModelHex { Val = Accent }));

                paragraphProperties.Append(
                    new D.CharacterBullet { Char = "\u25AA" });
            }
            else
            {
                paragraphProperties.Append(new D.NoBullet());
            }

            paragraphProperties.Append(
                new D.SpaceAfter(
                    new D.SpacingPoints
                    {
                        Val = bulleted ? 1000 : 400
                    }));

            var runProperties = new D.RunProperties
            {
                Language = "en-GB",
                FontSize = line.SizeHundredths,
                Bold = line.Bold,
                Dirty = false
            };

            runProperties.Append(
                new D.SolidFill(
                    new D.RgbColorModelHex { Val = line.ColourHex }));

            textBody.Append(
                new D.Paragraph(
                    paragraphProperties,
                    new D.Run(
                        runProperties,
                        new D.Text(line.Text))));
        }

        return new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties
                {
                    Id = id,
                    Name = $"Text{id}"
                },
                new P.NonVisualShapeDrawingProperties(
                    new D.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new D.Transform2D(
                    new D.Offset { X = x, Y = y },
                    new D.Extents { Cx = width, Cy = height }),
                new D.PresetGeometry(new D.AdjustValueList())
                {
                    Preset = D.ShapeTypeValues.Rectangle
                },
                new D.NoFill()),
            textBody);
    }

    private void AddSpeakerNotes(SlidePart slidePart, string notes)
    {
        var notesPart = slidePart.AddNewPart<NotesSlidePart>();

        notesPart.NotesSlide = new NotesSlide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties
                        {
                            Id = 1U,
                            Name = ""
                        },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(
                        new D.TransformGroup()),
                    new P.Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties
                            {
                                Id = 2U,
                                Name = "Notes Placeholder"
                            },
                            new P.NonVisualShapeDrawingProperties(
                                new D.ShapeLocks
                                {
                                    NoGrouping = true
                                }),
                            new ApplicationNonVisualDrawingProperties(
                                new PlaceholderShape
                                {
                                    Type = PlaceholderValues.Body
                                })),
                        new P.ShapeProperties(),
                        new P.TextBody(
                            new D.BodyProperties(),
                            new D.ListStyle(),
                            new D.Paragraph(
                                new D.Run(
                                    new D.RunProperties
                                    {
                                        Language = "en-GB",
                                        Dirty = false
                                    },
                                    new D.Text(notes))))))),
            new ColorMapOverride(new D.MasterColorMapping()));

        notesPart.NotesSlide.Save();
    }

    // ---------------------------------------------------------------
    // Master, layout and theme
    // ---------------------------------------------------------------

    private SlideMasterPart CreateSlideMasterPart(
        PresentationPart presentationPart)
    {
        var part = presentationPart.AddNewPart<SlideMasterPart>();

        part.SlideMaster = new SlideMaster(
            new CommonSlideData(NewShapeTree()),
            new P.ColorMap
            {
                Background1 = D.ColorSchemeIndexValues.Light1,
                Text1 = D.ColorSchemeIndexValues.Dark1,
                Background2 = D.ColorSchemeIndexValues.Light2,
                Text2 = D.ColorSchemeIndexValues.Dark2,
                Accent1 = D.ColorSchemeIndexValues.Accent1,
                Accent2 = D.ColorSchemeIndexValues.Accent2,
                Accent3 = D.ColorSchemeIndexValues.Accent3,
                Accent4 = D.ColorSchemeIndexValues.Accent4,
                Accent5 = D.ColorSchemeIndexValues.Accent5,
                Accent6 = D.ColorSchemeIndexValues.Accent6,
                Hyperlink = D.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink =
                    D.ColorSchemeIndexValues.FollowedHyperlink
            });

        var themePart = part.AddNewPart<ThemePart>();
        themePart.Theme = BuildTheme();
        themePart.Theme.Save();

        return part;
    }

    private SlideLayoutPart CreateSlideLayoutPart(
        SlideMasterPart slideMasterPart)
    {
        var part = slideMasterPart.AddNewPart<SlideLayoutPart>();

        part.SlideLayout = new SlideLayout(
            new CommonSlideData(NewShapeTree()),
            new ColorMapOverride(new D.MasterColorMapping()))
        {
            Type = SlideLayoutValues.Blank
        };

        part.SlideLayout.Save();

        return part;
    }

    private D.Theme BuildTheme()
    {
        return new D.Theme(
            new D.ThemeElements(
                new D.ColorScheme(
                    new D.Dark1Color(
                        new D.SystemColor
                        {
                            Val = D.SystemColorValues.WindowText
                        }),
                    new D.Light1Color(
                        new D.SystemColor
                        {
                            Val = D.SystemColorValues.Window
                        }),
                    new D.Dark2Color(
                        new D.RgbColorModelHex { Val = Ink }),
                    new D.Light2Color(
                        new D.RgbColorModelHex { Val = Tint }),
                    new D.Accent1Color(
                        new D.RgbColorModelHex { Val = Accent }),
                    new D.Accent2Color(
                        new D.RgbColorModelHex { Val = "067647" }),
                    new D.Accent3Color(
                        new D.RgbColorModelHex { Val = "B54708" }),
                    new D.Accent4Color(
                        new D.RgbColorModelHex { Val = "6941C6" }),
                    new D.Accent5Color(
                        new D.RgbColorModelHex { Val = "0E7490" }),
                    new D.Accent6Color(
                        new D.RgbColorModelHex { Val = "B42318" }),
                    new D.Hyperlink(
                        new D.RgbColorModelHex { Val = Accent }),
                    new D.FollowedHyperlinkColor(
                        new D.RgbColorModelHex { Val = "6941C6" }))
                {
                    Name = "PresalesGPT"
                },
                new D.FontScheme(
                    new D.MajorFont(
                        new D.LatinFont { Typeface = "Segoe UI" },
                        new D.EastAsianFont { Typeface = "" },
                        new D.ComplexScriptFont { Typeface = "" }),
                    new D.MinorFont(
                        new D.LatinFont { Typeface = "Segoe UI" },
                        new D.EastAsianFont { Typeface = "" },
                        new D.ComplexScriptFont { Typeface = "" }))
                {
                    Name = "PresalesGPT"
                },
                new D.FormatScheme(
                    new D.FillStyleList(
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            }),
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            }),
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            })),
                    new D.LineStyleList(
                        new D.Outline(
                            new D.SolidFill(
                                new D.SchemeColor
                                {
                                    Val = D.SchemeColorValues.PhColor
                                }))
                        {
                            Width = 9525
                        },
                        new D.Outline(
                            new D.SolidFill(
                                new D.SchemeColor
                                {
                                    Val = D.SchemeColorValues.PhColor
                                }))
                        {
                            Width = 15875
                        },
                        new D.Outline(
                            new D.SolidFill(
                                new D.SchemeColor
                                {
                                    Val = D.SchemeColorValues.PhColor
                                }))
                        {
                            Width = 25400
                        }),
                    new D.EffectStyleList(
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList()),
                        new D.EffectStyle(new D.EffectList())),
                    new D.BackgroundFillStyleList(
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            }),
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            }),
                        new D.SolidFill(
                            new D.SchemeColor
                            {
                                Val = D.SchemeColorValues.PhColor
                            })))
                {
                    Name = "PresalesGPT"
                }))
        {
            Name = "PresalesGPT"
        };
    }

    private static TextLine Line(
        string text,
        int sizeHundredths,
        bool bold,
        string colourHex)
    {
        return new TextLine(text, sizeHundredths, bold, colourHex);
    }

    private record TextLine(
        string Text,
        int SizeHundredths,
        bool Bold,
        string ColourHex);
}
