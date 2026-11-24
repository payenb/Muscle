using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

// Throwaway generator for Muscle 2.0's Grasshopper component icons.
// Draws every icon on a 24x24 logical grid (rendered at 8x then downsampled for clean
// anti-aliasing, matching how the real Bitmaps will render on the GH canvas), using the
// two brand colors from the Muscle logo. Run with `dotnet run` from this folder; output goes
// to src/Muscle/Resources/Icons/ (individual 24x24 PNGs, embedded as resources by the plugin)
// and a single contact-sheet PNG for quick review without opening Grasshopper.

string outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
    "src", "Muscle", "Resources", "Icons"));
Directory.CreateDirectory(outDir);

Color Teal = Color.FromArgb(255, 21, 71, 89);      // dark petrol teal, from "USCLE"/bicep
Color Red = Color.FromArgb(255, 196, 28, 38);       // crimson, from the "M"
Color Black = Color.FromArgb(255, 30, 30, 30);
Color White = Color.White;

const float S = 24f;      // logical canvas size
const int SCALE = 8;      // supersampling factor before downsample
const float SW = 2.1f;    // default stroke width, in logical units

Pen TealPen() => new Pen(Teal, SW) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
Pen RedPen() => new Pen(Red, SW) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
Pen BlackPen(float w = SW) => new Pen(Black, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

Bitmap NewCanvas(out Graphics g)
{
    var bmp = new Bitmap((int)(S * SCALE), (int)(S * SCALE), PixelFormat.Format32bppArgb);
    g = Graphics.FromImage(bmp);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.ScaleTransform(SCALE, SCALE);
    return bmp;
}

Bitmap Downsample(Bitmap big)
{
    var small = new Bitmap(24, 24, PixelFormat.Format32bppArgb);
    using var g = Graphics.FromImage(small);
    g.SmoothingMode = SmoothingMode.AntiAlias;
    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
    g.CompositingQuality = CompositingQuality.HighQuality;
    g.DrawImage(big, 0, 0, 24, 24);
    return small;
}

// ---- drawing primitives (all in 0..24 logical coordinates) ----

void Node(Graphics g, float cx, float cy, float r, Pen pen, Brush? fill = null)
{
    if (fill != null) g.FillEllipse(fill, cx - r, cy - r, 2 * r, 2 * r);
    g.DrawEllipse(pen, cx - r, cy - r, 2 * r, 2 * r);
}

void Bar(Graphics g, float x1, float y1, float x2, float y2, Pen pen, float nodeR = 1.8f, Brush? nodeFill = null)
{
    g.DrawLine(pen, x1, y1, x2, y2);
    Node(g, x1, y1, nodeR, pen, nodeFill ?? new SolidBrush(Color.White));
    Node(g, x2, y2, nodeR, pen, nodeFill ?? new SolidBrush(Color.White));
}

void Arrow(Graphics g, float x1, float y1, float x2, float y2, Pen pen, float headLen = 3f)
{
    g.DrawLine(pen, x1, y1, x2, y2);
    float ang = MathF.Atan2(y2 - y1, x2 - x1);
    float a1 = ang + MathF.PI - 0.5f, a2 = ang + MathF.PI + 0.5f;
    g.DrawLine(pen, x2, y2, x2 + headLen * MathF.Cos(a1), y2 + headLen * MathF.Sin(a1));
    g.DrawLine(pen, x2, y2, x2 + headLen * MathF.Cos(a2), y2 + headLen * MathF.Sin(a2));
}

void SupportHatch(Graphics g, float cx, float top, Pen pen, float halfWidth = 5f)
{
    // Pinned-support triangle on a single ground line - standard structural symbol,
    // deliberately without the finer hatch ticks: at 24px those rendered as a blurry smudge.
    g.DrawLine(pen, cx - halfWidth, top + 7f, cx, top);
    g.DrawLine(pen, cx + halfWidth, top + 7f, cx, top);
    g.DrawLine(pen, cx - halfWidth - 1.5f, top + 7f, cx + halfWidth + 1.5f, top + 7f);
}

void Hexagon(Graphics g, float cx, float cy, float r)
{
    // Karamba-style parameter badge: a filled hexagon with a white glyph on top.
    var pts = new PointF[6];
    for (int i = 0; i < 6; i++)
    {
        float ang = (float)(Math.PI / 180.0 * (60 * i - 90));
        pts[i] = new PointF(cx + r * MathF.Cos(ang), cy + r * MathF.Sin(ang));
    }
    g.FillPolygon(new SolidBrush(Teal), pts);
    g.DrawPolygon(new Pen(Color.FromArgb(255, 10, 38, 48), 1f), pts);
}

Pen WhitePen(float w = SW) => new Pen(White, w) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };

void FanDots(Graphics g, float hubX, float hubY, float spread = 6.5f)
{
    // "Explosion" motif for Deconstruct-family icons, matching native GH's convention of a
    // shape fanning out into separate, differently-colored pieces (one per output).
    var dotColors = new[] { Red, Teal, Black };
    for (int i = 0; i < 3; i++)
    {
        float y = hubY + (i - 1) * spread;
        var p = new PointF(20.3f, y);
        g.DrawLine(new Pen(Color.FromArgb(150, 130, 130, 130), 1.1f), new PointF(hubX, hubY), p);
        g.FillEllipse(new SolidBrush(dotColors[i]), p.X - 2f, p.Y - 2f, 4f, 4f);
        g.DrawEllipse(new Pen(White, 0.6f), p.X - 2f, p.Y - 2f, 4f, 4f);
    }
}

void FanDotsIn(Graphics g, float hubX, float hubY, float spread = 6.5f)
{
    // Mirror of FanDots for Construct-family icons: the same 3 colored pieces, but merging
    // INTO the whole (on the right) instead of exploding out of it - so a Construct/
    // Deconstruct pair reads as inverse operations of each other, per Basile's feedback
    // that Construct icons should echo "the same idea" as their Deconstruct counterpart.
    var dotColors = new[] { Red, Teal, Black };
    for (int i = 0; i < 3; i++)
    {
        float y = hubY + (i - 1) * spread;
        var p = new PointF(3.7f, y);
        g.DrawLine(new Pen(Color.FromArgb(150, 130, 130, 130), 1.1f), p, new PointF(hubX, hubY));
        g.FillEllipse(new SolidBrush(dotColors[i]), p.X - 2f, p.Y - 2f, 4f, 4f);
        g.DrawEllipse(new Pen(White, 0.6f), p.X - 2f, p.Y - 2f, 4f, 4f);
    }
}

void Wave(Graphics g, float x0, float y, float w, float amp, Pen pen, float decay = 1f, int n = 60)
{
    var pts = new PointF[n];
    for (int i = 0; i < n; i++)
    {
        float t = i / (float)(n - 1);
        float x = x0 + t * w;
        float env = MathF.Exp(-decay * t * 3f);
        float yy = y - amp * env * MathF.Sin(t * MathF.PI * 3.2f);
        pts[i] = new PointF(x, yy);
    }
    g.DrawLines(pen, pts);
}

void Grid(Graphics g, float x, float y, float w, float h, int rows, int cols, Pen pen)
{
    g.DrawRectangle(pen, x, y, w, h);
    for (int i = 1; i < cols; i++) { float xx = x + w * i / cols; g.DrawLine(pen, xx, y, xx, y + h); }
    for (int i = 1; i < rows; i++) { float yy = y + h * i / rows; g.DrawLine(pen, x, yy, x + w, yy); }
}

void BeamBadge(Graphics g)
{
    // Small filled red corner badge marking a Beam-FE-Model-only component - mirrors how
    // Beam-specific inputs/tooltips are flagged everywhere else in this plugin's code.
    g.FillEllipse(new SolidBrush(Red), 17.3f, 0.7f, 6f, 6f);
    g.DrawEllipse(new Pen(White, 0.8f), 17.3f, 0.7f, 6f, 6f);
}

// ---- icon library: name -> draw action ----

var icons = new Dictionary<string, Action<Graphics>>();

// 0. Python init
icons["PythonInit"] = g =>
{
    g.DrawEllipse(TealPen(), 4, 4, 16, 16);
    var p = TealPen();
    var pts = new[] { new PointF(10.3f, 8.5f), new PointF(16.5f, 12f), new PointF(10.3f, 15.5f) };
    g.FillPolygon(new SolidBrush(Teal), pts);
};

// 1. Parameters
icons["Support"] = g => SupportHatch(g, 12, 6.5f, TealPen());

// Karamba-style hexagon badges for Muscle's own persistent GH_Param types (the small
// standalone chips shown when a Node/Element/... parameter is placed on the canvas, not
// just used as a wire) - a filled teal hexagon with a simplified white glyph on top.
icons["ParamNode"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    g.FillEllipse(new SolidBrush(White), 9f, 9f, 6f, 6f);
};
icons["ParamElement"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    var p = WhitePen(1.8f);
    g.DrawLine(p, 7.5f, 15.5f, 16.5f, 8.5f);
    g.FillEllipse(new SolidBrush(White), 6f, 14f, 3f, 3f);
    g.FillEllipse(new SolidBrush(White), 15f, 7f, 3f, 3f);
};
icons["ParamStructure"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    var p = WhitePen(1.6f);
    g.DrawLine(p, 7f, 16f, 17f, 16f);
    g.DrawLine(p, 7f, 16f, 12f, 8f);
    g.DrawLine(p, 17f, 16f, 12f, 8f);
};
icons["ParamMaterial"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    var p = WhitePen(1.4f);
    g.DrawLine(p, 8f, 16.5f, 8f, 7.5f);
    g.DrawLine(p, 8f, 16.5f, 17f, 16.5f);
    g.DrawLine(WhitePen(1.8f), 9f, 15f, 15.5f, 9f);
};
icons["ParamCrossSection"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    g.DrawEllipse(WhitePen(2.4f), 7f, 7f, 10f, 10f);
};
icons["ParamSupport"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    var p = WhitePen(1.6f);
    g.DrawLine(p, 7.5f, 16f, 12f, 9f);
    g.DrawLine(p, 16.5f, 16f, 12f, 9f);
    g.DrawLine(p, 6.5f, 16f, 17.5f, 16f);
};
icons["ParamPointLoad"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    g.FillEllipse(new SolidBrush(White), 10.3f, 15.5f, 3.4f, 3.4f);
    Arrow(g, 12, 6.5f, 12, 15f, WhitePen(1.9f), 2.6f);
};
icons["ParamPrestress"] = g =>
{
    Hexagon(g, 12, 12, 10.2f);
    var p = WhitePen(1.6f);
    g.DrawLine(p, 8f, 14.5f, 16f, 9.5f);
    Arrow(g, 12.4f, 12.6f, 8.5f, 15f, WhitePen(1.6f), 2.4f);
    Arrow(g, 11.6f, 11.4f, 15.5f, 9f, WhitePen(1.6f), 2.4f);
};

// 2. Construct FE Model - mirrors the Deconstruct family's fan motif (pieces merging INTO
// the whole here, on the right, instead of exploding out of it) so a Construct/Deconstruct
// pair reads as inverse operations of each other.
icons["Element"] = g =>
{
    FanDotsIn(g, 15f, 12f);
    Bar(g, 21.5f, 17f, 15f, 8f, BlackPen(1.7f), 1.7f);
};
icons["CrossSection"] = g =>
{
    FanDotsIn(g, 14f, 12f);
    g.DrawEllipse(BlackPen(1.8f), 12.5f, 6f, 9f, 9f);
    g.DrawEllipse(new Pen(White, 1.1f), 14.7f, 8.2f, 4.6f, 4.6f);
};
icons["CrossSectionSquare"] = g =>
{
    FanDotsIn(g, 14f, 12f);
    g.DrawRectangle(BlackPen(1.8f), 12.5f, 6f, 9f, 9f);
    g.DrawRectangle(new Pen(White, 1.1f), 14.7f, 8.2f, 4.6f, 4.6f);
};
icons["MaterialList"] = g =>
{
    for (int i = 0; i < 3; i++)
    {
        float y = 6f + i * 6f;
        g.DrawLine(BlackPen(1.4f), 4f, y, 15f, y);
        g.FillEllipse(new SolidBrush(i == 1 ? Red : Teal), 17f, y - 1.6f, 3.2f, 3.2f);
    }
};
Action<Graphics> SupportAxisIcon(string axis) => g =>
{
    // Small support triangle top-left (family resemblance to "Support"), a bold axis
    // letter dominates - legible at 24px is the priority, per Basile's feedback that the
    // first pass's tiny sub-caption letter was unreadable.
    var p = TealPen(); p.Width = 1.7f;
    g.DrawLine(p, 3.5f, 9f, 7f, 3.5f);
    g.DrawLine(p, 10.5f, 9f, 7f, 3.5f);
    g.DrawLine(p, 3.5f, 9f, 10.5f, 9f);
    using var f = new Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
    var sz = g.MeasureString(axis, f);
    g.DrawString(axis, f, new SolidBrush(Red), 24 - sz.Width - 1.5f, 24 - sz.Height - 1f);
};
icons["SupportX"] = SupportAxisIcon("X");
icons["SupportY"] = SupportAxisIcon("Y");
icons["SupportZ"] = SupportAxisIcon("Z");
icons["PointLoad"] = g =>
{
    Node(g, 12, 20, 1.8f, TealPen(), new SolidBrush(White));
    Arrow(g, 12, 4, 12, 17.5f, RedPen(), 3f);
};
icons["SelfWeight"] = g =>
{
    var p = TealPen();
    g.DrawLine(p, 4.5f, 15f, 19.5f, 15f);
    Node(g, 4.5f, 15f, 1.6f, p, new SolidBrush(White));
    Node(g, 19.5f, 15f, 1.6f, p, new SolidBrush(White));
    var rp = RedPen(); rp.Width = 1.7f;
    Arrow(g, 8f, 15.8f, 8f, 21.5f, rp, 2.2f);
    Arrow(g, 16f, 15.8f, 16f, 21.5f, rp, 2.2f);
};
icons["PointMass"] = g =>
{
    g.FillEllipse(new SolidBrush(Teal), 7f, 7f, 10f, 10f);
    g.DrawEllipse(new Pen(White, 1.2f), 7f, 7f, 10f, 10f);
    g.DrawEllipse(RedPen(), 3.5f, 3.5f, 17f, 17f);
};
icons["Structure"] = g =>
{
    FanDotsIn(g, 15f, 12f);
    var p = BlackPen(1.6f);
    g.DrawLine(p, 21.5f, 15f, 21.5f, 6f);
    g.DrawLine(p, 21.5f, 6f, 15.5f, 10.5f);
    g.DrawLine(p, 21.5f, 15f, 15.5f, 10.5f);
};
void MaterialAxes(Graphics g)
{
    // Cross through the origin, both quadrants visible - tension (up-right) AND
    // compression (down-left), not just a single-quadrant corner graph. Kept thin and
    // light so the teal curve (drawn after, on top) is what actually reads at 24px.
    var axisPen = new Pen(Color.FromArgb(255, 150, 150, 150), 0.9f);
    g.DrawLine(axisPen, 12f, 20f, 12f, 4f);
    g.DrawLine(axisPen, 4f, 12f, 20f, 12f);
}
Pen CurvePen() => new Pen(Teal, 2.6f) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round };
icons["MaterialLinear"] = g =>
{
    MaterialAxes(g);
    // Same stiffness in tension and compression: one straight line through the origin.
    g.DrawLine(CurvePen(), 5f, 19f, 19f, 5f);
};
icons["MaterialBilinear"] = g =>
{
    MaterialAxes(g);
    // Steep (elastic) near the origin, shallow (yielding/plateau) further out - mirrored
    // symmetrically for both compression (left) and tension (right).
    g.DrawLines(CurvePen(), new[] {
        new PointF(5f, 16.5f), new PointF(10f, 15f), new PointF(12f, 12f),
        new PointF(14f, 9f), new PointF(19f, 7.5f)
    });
};
icons["MaterialTensionOnly"] = g =>
{
    MaterialAxes(g);
    // Zero stiffness in compression (flat along the axis, left of origin), proportional
    // in tension (rising to the right) - a cable's behavior.
    g.DrawLines(CurvePen(), new[] { new PointF(5f, 12f), new PointF(12f, 12f), new PointF(19f, 6.5f) });
};
icons["MaterialCompressionOnly"] = g =>
{
    MaterialAxes(g);
    // Mirror of tension-only: proportional in compression (down-left of origin), zero in
    // tension (flat along the axis, right of origin) - a strut with no tensile capacity.
    g.DrawLines(CurvePen(), new[] { new PointF(5f, 19f), new PointF(12f, 12f), new PointF(19f, 12f) });
};
icons["BeamElement"] = g =>
{
    FanDotsIn(g, 14f, 13.5f);
    var p = TealPen(); p.Width = 3.2f;
    g.DrawLine(p, 21f, 18.5f, 14.5f, 9.5f);
    for (float t = 0.2f; t < 1f; t += 0.3f)
    {
        float x = 21f + (14.5f - 21f) * t, y = 18.5f + (9.5f - 18.5f) * t;
        g.DrawLine(BlackPen(1f), x - 1.5f, y + 1.5f, x + 1.5f, y - 1.5f);
    }
    Node(g, 21f, 18.5f, 1.7f, TealPen(), new SolidBrush(White));
    Node(g, 14.5f, 9.5f, 1.7f, TealPen(), new SolidBrush(White));
    BeamBadge(g);
};
icons["BeamSupport"] = g => { SupportHatch(g, 10.5f, 5f, TealPen(), 4.3f); BeamBadge(g); };
icons["BeamStructure"] = g =>
{
    FanDotsIn(g, 12f, 13.5f);
    var p = TealPen(); p.Width = 3f;
    g.DrawLine(p, 13f, 19f, 13f, 8f);
    g.DrawLine(p, 13f, 8f, 21f, 8f);
    g.DrawLine(p, 21f, 8f, 21f, 19f);
    Node(g, 13, 19, 1.5f, TealPen(), new SolidBrush(White));
    Node(g, 21, 19, 1.5f, TealPen(), new SolidBrush(White));
    BeamBadge(g);
};

// 3. Static Loading
icons["Prestress"] = g =>
{
    var p = TealPen();
    Bar(g, 5f, 16.5f, 19f, 7.5f, p);
    var rp = RedPen(); rp.Width = 1.9f;
    Arrow(g, 1.5f, 18.6f, 5.7f, 16.2f, rp, 3.2f);
    Arrow(g, 22.5f, 5.4f, 18.3f, 7.8f, rp, 3.2f);
};
icons["SelfStress"] = g =>
{
    var p = TealPen();
    Bar(g, 6f, 16f, 18f, 8f, p);
    var rp = RedPen(); rp.Width = 1.9f;
    g.DrawArc(rp, 2.5f, 2.5f, 19f, 19f, 200, 250);
    float endAngRad = (200f + 250f) * MathF.PI / 180f;
    float cx = 2.5f + 19f / 2f, cy = 2.5f + 19f / 2f, r = 19f / 2f;
    var tip = new PointF(cx + r * MathF.Cos(endAngRad), cy + r * MathF.Sin(endAngRad));
    float tangent = endAngRad + MathF.PI / 2f;
    var h1 = new PointF(tip.X - 3f * MathF.Cos(tangent - 0.5f), tip.Y - 3f * MathF.Sin(tangent - 0.5f));
    var h2 = new PointF(tip.X - 3f * MathF.Cos(tangent + 0.5f), tip.Y - 3f * MathF.Sin(tangent + 0.5f));
    g.DrawLine(rp, tip.X, tip.Y, h1.X, h1.Y);
    g.DrawLine(rp, tip.X, tip.Y, h2.X, h2.Y);
};
icons["LocalizeSelfStress"] = g =>
{
    var p = TealPen();
    Bar(g, 5f, 17f, 15f, 11f, p, 1.5f);
    g.DrawEllipse(RedPen(), 12f, 3.5f, 8f, 8f);
    g.DrawLine(RedPen(), 17.6f, 9.6f, 20.5f, 12.5f);
};

// 4. Static Solvers
icons["LinearDM"] = g =>
{
    g.DrawLines(BlackPen(1.4f), new[] { new PointF(4, 20), new PointF(4, 4), new PointF(20, 4) });
    g.DrawLine(TealPen(), 5f, 18f, 18f, 7f);
};
icons["NonlinearDM"] = g =>
{
    g.DrawLines(BlackPen(1.4f), new[] { new PointF(4, 20), new PointF(4, 4), new PointF(20, 4) });
    var pts = new PointF[30];
    for (int i = 0; i < 30; i++)
    {
        float t = i / 29f;
        pts[i] = new PointF(5f + t * 13f, 18f - 12f * MathF.Pow(t, 1.6f));
    }
    g.DrawLines(TealPen(), pts);
};
icons["SVD"] = g => Grid(g, 5, 5, 14, 14, 3, 3, TealPen());
icons["DR"] = g => Wave(g, 3.5f, 12f, 17f, 7f, TealPen(), 1.1f);
icons["SRDR"] = g =>
{
    Wave(g, 3.5f, 10f, 17f, 6f, TealPen(), 1.1f);
    Arrow(g, 20f, 6f, 20f, 15f, RedPen(), 2.4f);
};
icons["BeamFEM"] = g =>
{
    var p = TealPen(); p.Width = 3.2f;
    g.DrawLine(p, 4.5f, 19f, 19.5f, 19f);
    Node(g, 4.5f, 19f, 1.7f, TealPen(), new SolidBrush(White));
    Node(g, 19.5f, 19f, 1.7f, TealPen(), new SolidBrush(White));
    Arrow(g, 12f, 5f, 12f, 15.5f, RedPen(), 2.6f);
    BeamBadge(g);
};
icons["BeamDR"] = g =>
{
    Wave(g, 3.5f, 15f, 17f, 6f, TealPen(), 1.1f, 50);
    BeamBadge(g);
};

// 5. Deconstruct FE Model - "explosion" motif: the whole (small, dark) glyph on the left,
// fanning out into separately-colored pieces on the right, matching native GH's own
// convention for Deconstruct-family icons (a shape breaking into its distinct outputs).
icons["DeconstructStructure"] = g =>
{
    var p = BlackPen(1.6f);
    g.DrawLine(p, 2.5f, 15f, 2.5f, 6f);
    g.DrawLine(p, 2.5f, 6f, 8.5f, 10.5f);
    g.DrawLine(p, 2.5f, 15f, 8.5f, 10.5f);
    FanDots(g, 9f, 12f);
};
icons["DeconstructNode"] = g =>
{
    Node(g, 6f, 12f, 2.4f, BlackPen(1.6f), new SolidBrush(White));
    FanDots(g, 8.4f, 12f);
};
icons["DeconstructElement"] = g =>
{
    Bar(g, 2.5f, 17f, 9f, 8f, BlackPen(1.6f), 1.6f);
    FanDots(g, 9f, 12f);
};
icons["DeconstructCrossSection"] = g =>
{
    var p = BlackPen(1.6f);
    g.DrawEllipse(p, 1.5f, 7f, 10f, 10f);
    g.DrawEllipse(new Pen(White, 1.1f), 4f, 9.5f, 5f, 5f);
    FanDots(g, 11.5f, 12f);
};
icons["DeconstructMaterial"] = g =>
{
    var p = new Pen(Color.FromArgb(255, 150, 150, 150), 0.9f);
    g.DrawLine(p, 1.5f, 20f, 1.5f, 4f);
    g.DrawLine(p, 1.5f, 20f, 10f, 20f);
    g.DrawLines(CurvePen(), new[] { new PointF(2.5f, 18f), new PointF(6f, 12f), new PointF(9.5f, 6f) });
    FanDots(g, 10f, 12f);
};
icons["DeconstructBeamStructure"] = g =>
{
    Bar(g, 4f, 19f, 4f, 9f, RedPen(), 1.5f);
    Bar(g, 12.5f, 19f, 12.5f, 5f, TealPen(), 1.5f);
    BeamBadge(g);
};
icons["DeconstructBeamNode"] = g =>
{
    Node(g, 12, 12, 3f, TealPen(), new SolidBrush(White));
    g.DrawLine(RedPen(), 12, 4, 12, 8.3f);
    g.DrawLine(TealPen(), 12, 15.7f, 12, 20);
    g.DrawLine(RedPen(), 4, 12, 8.3f, 12);
    g.DrawLine(TealPen(), 15.7f, 12, 20, 12);
    BeamBadge(g);
};
icons["DeconstructBeamElement"] = g =>
{
    Bar(g, 4f, 17f, 9.5f, 12.5f, RedPen(), 1.7f);
    Bar(g, 13.5f, 9.5f, 19f, 5f, TealPen(), 1.7f);
    BeamBadge(g);
};

// 6. Dynamic
icons["Dynamic"] = g => Wave(g, 3.5f, 12f, 17f, 8f, TealPen(), 0f, 60);

// 7. Display
icons["ShowForces"] = g =>
{
    var p = TealPen();
    g.DrawLine(p, 4.5f, 18f, 19.5f, 6f);
    for (float t = 0.15f; t <= 0.85f; t += 0.2f)
    {
        float x = 4.5f + (19.5f - 4.5f) * t, y = 18f + (6f - 18f) * t;
        Arrow(g, x - 1.6f, y + 1.4f, x + 1.6f, y - 1.4f, RedPen(), 1.8f);
    }
};
icons["ShowBending"] = g =>
{
    g.DrawLine(BlackPen(1.2f), 4.5f, 18f, 19.5f, 6f);
    var pts = new PointF[30];
    for (int i = 0; i < 30; i++)
    {
        float t = i / 29f;
        float bx = 4.5f + (19.5f - 4.5f) * t, by = 18f + (6f - 18f) * t;
        float bulge = 4.5f * MathF.Sin(t * MathF.PI);
        pts[i] = new PointF(bx + bulge * 0.55f, by + bulge * 0.83f);
    }
    g.DrawLines(RedPen(), pts);
};
icons["DisplaySettings"] = g =>
{
    for (int i = 0; i < 3; i++)
    {
        float y = 6f + i * 6f;
        g.DrawLine(BlackPen(1.6f), 4f, y, 20f, y);
        float knobX = i == 1 ? 15f : 9f;
        g.FillEllipse(new SolidBrush(Teal), knobX - 2f, y - 2f, 4f, 4f);
        g.DrawEllipse(new Pen(White, 0.6f), knobX - 2f, y - 2f, 4f, 4f);
    }
};

// 8. Util
icons["ToMatrix"] = g => Grid(g, 4, 7, 16, 10, 2, 4, TealPen());
icons["BoolGate"] = g =>
{
    g.DrawEllipse(TealPen(), 4, 8, 8, 8);
    g.DrawEllipse(RedPen(), 12, 8, 8, 8);
    g.DrawLine(BlackPen(1.4f), 12, 12, 20, 12);
};
icons["Count"] = g =>
{
    using var f = new Font("Segoe UI", 13f, System.Drawing.FontStyle.Bold);
    var sz = g.MeasureString("#", f);
    g.DrawString("#", f, new SolidBrush(Teal), (24 - sz.Width) / 2f, (24 - sz.Height) / 2f - 1f);
};

// Plugin-level icon (MuscleInfo) - a compact version of the M mark, red.
icons["MusclePlugin"] = g =>
{
    var p = RedPen(); p.Width = 2.6f;
    g.DrawLines(p, new[] { new PointF(4, 20), new PointF(4, 4), new PointF(12, 13), new PointF(20, 4), new PointF(20, 20) });
};

// ---- render everything ----

foreach (var (name, draw) in icons)
{
    using var big = NewCanvas(out var g);
    draw(g);
    g.Dispose();
    using var small = Downsample(big);
    small.Save(Path.Combine(outDir, name + ".png"), ImageFormat.Png);
}

Console.WriteLine($"Wrote {icons.Count} icons to {outDir}");

// ---- contact sheet ----

int cols = 6;
int rows = (int)Math.Ceiling(icons.Count / (double)cols);
int cellW = 110, cellH = 130, iconPx = 72;
using (var sheet = new Bitmap(cols * cellW, rows * cellH))
using (var sg = Graphics.FromImage(sheet))
{
    sg.Clear(Color.White);
    sg.SmoothingMode = SmoothingMode.AntiAlias;
    sg.InterpolationMode = InterpolationMode.NearestNeighbor;
    using var font = new Font("Segoe UI", 8f);
    int i = 0;
    foreach (var (name, _) in icons)
    {
        int cx = i % cols, cy = i / cols;
        int x = cx * cellW, y = cy * cellH;
        using var icon = new Bitmap(Path.Combine(outDir, name + ".png"));
        sg.DrawImage(icon, x + (cellW - iconPx) / 2, y + 10, iconPx, iconPx);
        var sz = sg.MeasureString(name, font);
        sg.DrawString(name, font, Brushes.Black, x + (cellW - sz.Width) / 2, y + iconPx + 16);
        sg.DrawRectangle(Pens.LightGray, x, y, cellW - 1, cellH - 1);
        i++;
    }
    sheet.Save(Path.Combine(outDir, "_contact_sheet.png"), ImageFormat.Png);
}

Console.WriteLine("Wrote contact sheet.");
