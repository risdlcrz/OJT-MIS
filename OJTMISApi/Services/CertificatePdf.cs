using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace OJTMISApi.Services
{
    /// <summary>
    /// Everything the printable certificate sheet needs, mapped from an intern
    /// record plus the options chosen on the Certificate of Completion page.
    /// </summary>
    public sealed record CertificatePdfModel
    {
        public string Honorific { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string School { get; init; } = string.Empty;
        public string Office { get; init; } = string.Empty;
        public double HoursRendered { get; init; }
        public int RequiredHours { get; init; }
        public DateTime? DateStarted { get; init; }
        public DateTime? DateCompleted { get; init; }
        public DateTime? DateIssued { get; init; }
        public string SignatoryName { get; init; } = string.Empty;
        public string SignatoryPosition { get; init; } = string.Empty;
        public string SignatoryOffice { get; init; } = string.Empty;
        public bool A4Paper { get; init; }
    }

    /// <summary>
    /// Renders the Certificate of Completion as a one-page landscape PDF
    /// (Letter or A4) that mirrors the on-screen <c>coc-sheet</c> design:
    /// agency header with the COA seal, underlined title, justified body with
    /// bold fill-ins, the issuance line, and the centered signatory block.
    ///
    /// The PDF is written by hand (no third-party library): base-14 Times
    /// fonts with standard Adobe AFM metrics for exact centering/justification,
    /// and the seal PNG is decoded and embedded as a FlateDecode image.
    /// </summary>
    public static class CertificatePdf
    {
        private const double Mm = 2.834645669; // 1 mm in PDF points
        private const double TitleSize = 18.0;
        private const double BodySize = 13.5;
        private const double BodyLeading = 21.0; // 13.5 pt * 1.55 line height

        // Base-14 Times metrics, units per 1000 em, straight from the Adobe AFMs.
        private static readonly Dictionary<char, double> RomanWidths = ParseWidths(
            "sp:250 !:333 \":408 #:500 $:500 %:833 &:778 ':180 (:333 ):333 *:500 +:564 ,:250 -:333 .:250 /:278 " +
            "0:500 1:500 2:500 3:500 4:500 5:500 6:500 7:500 8:500 9:500 " +
            "::278 ;:278 <:564 =:564 >:564 ?:444 @:921 " +
            "A:722 B:667 C:667 D:722 E:611 F:556 G:722 H:722 I:333 J:389 K:722 L:611 M:889 N:722 O:722 P:556 Q:722 R:667 S:556 T:611 U:722 V:722 W:944 X:722 Y:722 Z:611 " +
            "[:333 \\:278 ]:333 ^:469 _:500 `:333 " +
            "a:444 b:500 c:444 d:500 e:444 f:333 g:500 h:500 i:278 j:278 k:500 l:278 m:778 n:500 o:500 p:500 q:500 r:333 s:389 t:278 u:500 v:500 w:722 x:500 y:500 z:500 " +
            "{:480 |:200 }:480 ~:541");

        private static readonly Dictionary<char, double> BoldWidths = ParseWidths(
            "sp:250 !:333 \":555 #:500 $:500 %:1000 &:833 ':278 (:333 ):333 *:500 +:570 ,:250 -:333 .:250 /:278 " +
            "0:500 1:500 2:500 3:500 4:500 5:500 6:500 7:500 8:500 9:500 " +
            "::333 ;:333 <:570 =:570 >:570 ?:500 @:930 " +
            "A:722 B:667 C:722 D:722 E:667 F:611 G:778 H:778 I:389 J:500 K:778 L:667 M:944 N:722 O:778 P:611 Q:778 R:722 S:556 T:667 U:722 V:722 W:1000 X:722 Y:722 Z:667 " +
            "[:333 \\:278 ]:333 ^:581 _:500 `:333 " +
            "a:500 b:556 c:444 d:556 e:444 f:333 g:500 h:556 i:278 j:333 k:556 l:278 m:833 n:556 o:500 p:556 q:556 r:444 s:389 t:333 u:556 v:500 w:722 x:500 y:500 z:500 " +
            "{:480 |:200 }:480 ~:541");

        /// <summary>Renders the certificate. When the logo cannot be decoded, the sheet is drawn without the seal.</summary>
        public static byte[] Render(CertificatePdfModel model, byte[]? logoPng)
        {
            ArgumentNullException.ThrowIfNull(model);

            var pageW = model.A4Paper ? 841.89 : 792.0;
            var pageH = model.A4Paper ? 595.28 : 612.0;
            var centerX = pageW / 2.0;

            // Same geometry as the on-screen sheet: thin border, 16 mm side
            // padding, 12 mm top and 10 mm bottom padding.
            var border = 8.0;
            var x0 = border + 16 * Mm;
            var x1 = pageW - border - 16 * Mm;

            var c = new StringBuilder();
            c.Append("0.96 w\n");
            c.Append(Formattable($"{border} {border} {pageW - 2 * border} {pageH - 2 * border} re S\n"));

            var logo = TryDecodePng(logoPng);
            var y = pageH - border - 12 * Mm; // top of the content area

            // ---- Agency header: seal on the left, centered letterhead ----
            var header = new (string Text, double Size, bool Bold, double Lead)[]
            {
                ("Republic of the Philippines", 12, false, 15.0),
                ("COMMISSION ON AUDIT", 14, true, 17.5),
                ("HUMAN RESOURCE MANAGEMENT OFFICE", 12, false, 15.0),
                ("Commonwealth Avenue, Quezon City", 12, false, 15.0)
            };
            var headerHeight = header.Sum(line => line.Lead);

            if (logo is not null)
            {
                // 22 mm square, vertically centered against the letterhead.
                var box = 22 * Mm;
                var scale = box / Math.Max(logo.Width, logo.Height);
                var w = logo.Width * scale;
                var h = logo.Height * scale;
                var top = y - (headerHeight - h) / 2.0;
                c.Append(Formattable($"q {w} 0 0 {h} {x0} {top - h} cm /Logo Do Q\n"));
            }

            var hy = y;
            foreach (var line in header)
            {
                hy -= line.Lead;
                DrawText(c, centerX - TextWidth(line.Text, line.Size, line.Bold) / 2.0, hy, line.Text, line.Size, line.Bold);
            }
            y -= headerHeight;

            // ---- Underlined title ----
            y -= 6 * Mm;
            var title = "CERTIFICATE OF COMPLETION";
            var titleW = TextWidth(title, TitleSize, true);
            var titleBaseline = y - TitleSize;
            DrawText(c, centerX - titleW / 2.0, titleBaseline, title, TitleSize, true);
            Line(c, centerX - titleW / 2.0, titleBaseline - 4.5, centerX + titleW / 2.0, titleBaseline - 4.5, 0.9);
            y = titleBaseline - 5 * Mm;

            // ---- Justified body with bold + underlined fill-ins ----
            var name = string.IsNullOrWhiteSpace(model.Honorific)
                ? model.FullName
                : $"{model.Honorific} {model.FullName}".Trim();
            var hours = FormatHours(model.HoursRendered);
            var start = LongDate(model.DateStarted);
            var end = LongDate(model.DateCompleted);

            var body = new List<(string Text, bool Bold)>
            {
                ("This is to certify that ", false),
                (name, true),
                (", of the ", false),
                (string.IsNullOrWhiteSpace(model.School) ? "school indicated above" : model.School, true),
                (", has satisfactorily completed ", false),
                (hours, true),
                (" hours of office training at the ", false),
                (string.IsNullOrWhiteSpace(model.Office) ? "assigned office" : model.Office, true),
                (", Commission on Audit. The training covered the period from ", false),
                (string.IsNullOrWhiteSpace(start) ? "the start of the training" : start, true),
                (" to ", false),
                (string.IsNullOrWhiteSpace(end) ? "the end of the training" : end, true),
                (".", false)
            };
            y = DrawJustified(c, body, x0, x1, y, 12 * Mm);

            // ---- Issued line ----
            var issued = model.DateIssued ?? DateTime.Today;
            var issuedText =
                $"Issued this {Ordinal(issued.Day)} day of {issued.ToString("MMMM", CultureInfo.InvariantCulture)}, " +
                $"{issued.ToString("yyyy", CultureInfo.InvariantCulture)} at the Commission on Audit, Quezon City, Philippines.";

            y -= 6 * Mm;
            y = DrawJustified(c, new List<(string, bool)> { (issuedText, false) }, x0, x1, y, 12 * Mm);

            // ---- Signatory block, centered ----
            y -= 8 * Mm;
            var signName = string.IsNullOrWhiteSpace(model.SignatoryName) ? "SIGNATORY NAME" : model.SignatoryName;
            var signPosition = string.IsNullOrWhiteSpace(model.SignatoryPosition) ? "Position" : model.SignatoryPosition;
            var signOffice = string.IsNullOrWhiteSpace(model.SignatoryOffice) ? "Division" : model.SignatoryOffice;

            var nameBaseline = y - 12 * Mm - BodySize; // 12 mm wet-signature space
            DrawText(c, centerX - TextWidth(signName, BodySize, true) / 2.0, nameBaseline, signName, BodySize, true);
            var sy = nameBaseline;
            foreach (var (text, size, bold) in new[]
                     {
                         (signPosition, 12.5, false),
                         (signOffice, 12.5, false),
                         ("Human Resource Management Office", 12.5, false)
                     })
            {
                sy -= 16.9;
                DrawText(c, centerX - TextWidth(text, size, bold) / 2.0, sy, text, size, bold);
            }

            // ---- Assemble the PDF objects and cross-reference table ----
            var content = Encoding.Latin1.GetBytes(c.ToString());
            var hasLogo = logo is not null;
            var resources = hasLogo
                ? "<< /Font << /F1 5 0 R /F2 6 0 R >> /XObject << /Logo 7 0 R >> >>"
                : "<< /Font << /F1 5 0 R /F2 6 0 R >> >>";

            var objects = new List<(string Dict, byte[]? Stream)>
            {
                ("<< /Type /Catalog /Pages 2 0 R >>", null),
                ("<< /Type /Pages /Kids [3 0 R] /Count 1 >>", null),
                ($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Fmt(pageW)} {Fmt(pageH)}] /Resources {resources} /Contents 4 0 R >>", null),
                ($"<< /Length {content.Length} >>", content)
            };

            objects.Add(("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Roman /Encoding /WinAnsiEncoding >>", null));
            objects.Add(("<< /Type /Font /Subtype /Type1 /BaseFont /Times-Bold /Encoding /WinAnsiEncoding >>", null));

            if (hasLogo)
            {
                var image = ZlibCompress(logo!.Rgb);
                objects.Add((
                    $"<< /Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} " +
                    $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode /Length {image.Length} >>",
                    image));
            }

            using var pdf = new MemoryStream();
            Write(pdf, Encoding.Latin1.GetBytes("%PDF-1.4\n%\u00E2\u00E3\u00CF\u00D3\n"));

            var offsets = new List<long>();
            for (var number = 1; number <= objects.Count; number++)
            {
                var (dict, stream) = objects[number - 1];
                offsets.Add(pdf.Position);
                Write(pdf, Encoding.Latin1.GetBytes($"{number} 0 obj\n{dict}\n"));
                if (stream is not null)
                {
                    Write(pdf, Encoding.Latin1.GetBytes("stream\n"));
                    Write(pdf, stream);
                    Write(pdf, Encoding.Latin1.GetBytes("\nendstream\n"));
                }
                Write(pdf, Encoding.Latin1.GetBytes("endobj\n"));
            }

            var xrefOffset = pdf.Position;
            var count = objects.Count + 1;
            var xref = new StringBuilder();
            xref.Append($"xref\n0 {count}\n");
            xref.Append("0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                xref.Append(CultureInfo.InvariantCulture, $"{offset:d10} 00000 n \n");
            }
            xref.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {count} /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF");
            Write(pdf, Encoding.Latin1.GetBytes(xref.ToString()));

            return pdf.ToArray();
        }

        // ====================================================================
        // Content-stream primitives
        // ====================================================================

        private static void DrawText(StringBuilder c, double x, double baseline, string text, double size, bool bold)
        {
            var font = bold ? "/F2" : "/F1";
            c.Append(Formattable($"BT {font} {Fmt(size)} Tf {Fmt(x)} {Fmt(baseline)} Td ({Escape(text)}) Tj ET\n"));
        }

        private static void Line(StringBuilder c, double x1, double y1, double x2, double y2, double width)
        {
            c.Append(Formattable($"{Fmt(width)} w {Fmt(x1)} {Fmt(y1)} m {Fmt(x2)} {Fmt(y2)} l S\n"));
        }

        /// <summary>
        /// Draws a justified paragraph with a first-line indent. Bold words are
        /// underlined like the fill-ins on the printed sheet. Every line except
        /// the last is stretched to end exactly at <paramref name="x1"/>.
        /// Returns the y position below the paragraph.
        /// </summary>
        private static double DrawJustified(
            StringBuilder c,
            List<(string Text, bool Bold)> segments,
            double x0,
            double x1,
            double top,
            double firstIndent)
        {
            var words = new List<(string Word, bool Bold, double W)>();
            foreach (var (text, bold) in segments)
            {
                foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    words.Add((word, bold, TextWidth(word, BodySize, bold)));
                }
            }

            var spaceW = TextWidth(" ", BodySize, false);

            // ---- Break the words into lines (CSS-like greedy wrapping) ----
            var lines = new List<(List<(string Word, bool Bold, double W)> Words, double Start)>();
            var current = new List<(string Word, bool Bold, double W)>();
            var start = x0 + firstIndent;

            foreach (var word in words)
            {
                var width = current.Sum(w => w.W) + Math.Max(0, current.Count - 1) * spaceW;
                var needed = current.Count == 0 ? word.W : width + spaceW + word.W;

                if (current.Count > 0 && start + needed > x1)
                {
                    lines.Add((current, start));
                    current = new List<(string Word, bool Bold, double W)>();
                    start = x0;
                }

                current.Add(word);
            }

            if (current.Count > 0)
            {
                lines.Add((current, start));
            }

            // ---- Draw the lines, stretching every line but the last ----
            var baseline = top - BodySize;
            for (var i = 0; i < lines.Count; i++)
            {
                var (line, lineStart) = lines[i];
                var natural = line.Sum(w => w.W) + Math.Max(0, line.Count - 1) * spaceW;
                var isLast = i == lines.Count - 1;
                var extra = !isLast && line.Count > 1
                    ? Math.Max(0, (x1 - lineStart - natural) / (line.Count - 1))
                    : 0;

                var x = lineStart;
                foreach (var (word, bold, w) in line)
                {
                    DrawText(c, x, baseline, word, BodySize, bold);
                    if (bold)
                    {
                        // Match the .coc-fill padding (0.5 mm each side).
                        var pad = 0.5 * Mm;
                        Line(c, x - pad, baseline - 2.2, x + w + pad, baseline - 2.2, 0.6);
                    }
                    x += w + spaceW + extra;
                }

                baseline -= BodyLeading;
            }

            return baseline + BodyLeading;
        }

        // ====================================================================
        // Text measurement and encoding
        // ====================================================================

        private static double TextWidth(string text, double size, bool bold)
        {
            var widths = bold ? BoldWidths : RomanWidths;
            var fallback = bold ? 556.0 : 500.0;
            var total = 0.0;
            foreach (var ch in ToWinAnsi(text))
            {
                total += widths.GetValueOrDefault(ch, fallback);
            }
            return total / 1000.0 * size;
        }

        /// <summary>Escapes PDF string delimiters. The stream is encoded Latin-1, so bytes map 1:1.</summary>
        private static string Escape(string text)
        {
            var safe = ToWinAnsi(text)
                .Replace("\\", "\\\\")
                .Replace("(", "\\(")
                .Replace(")", "\\)");
            return safe;
        }

        /// <summary>
        /// Maps text into the WinAnsi range: typographic quotes become ASCII,
        /// Latin-1 letters (for example ñ) are kept, and anything above Latin-1
        /// is decomposed and its accent dropped.
        /// </summary>
        private static string ToWinAnsi(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var replaced = text
                .Replace('\u2013', '-').Replace('\u2014', '-')
                .Replace('\u2018', '\'').Replace('\u2019', '\'')
                .Replace('\u201C', '"').Replace('\u201D', '"')
                .Replace("\u2026", "...")
                .Replace('\u00A0', ' ')
                .Replace('\r', ' ').Replace('\n', ' ');

            var sb = new StringBuilder(replaced.Length);
            foreach (var ch in replaced)
            {
                if (ch <= '\u00FF')
                {
                    sb.Append(ch);
                    continue;
                }

                foreach (var part in ch.ToString().Normalize(NormalizationForm.FormD))
                {
                    if (part <= '\u00FF' &&
                        char.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)
                    {
                        sb.Append(part);
                    }
                }
            }

            return sb.ToString();
        }

        internal static string FormatHours(double hours)
        {
            var rounded = Math.Round(hours, 2);
            return rounded == Math.Floor(rounded)
                ? ((int)rounded).ToString(CultureInfo.InvariantCulture)
                : rounded.ToString("0.##", CultureInfo.InvariantCulture);
        }

        internal static string LongDate(DateTime? date) =>
            date?.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) ?? string.Empty;

        internal static string Ordinal(int day)
        {
            var teen = day % 100;
            if (teen is >= 11 and <= 13) return $"{day}th";
            return (day % 10) switch
            {
                1 => $"{day}st",
                2 => $"{day}nd",
                3 => $"{day}rd",
                _ => $"{day}th"
            };
        }

        private static string Fmt(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>Formats an interpolated string with the invariant culture.</summary>
        private static string Formattable(FormattableString formattable) =>
            formattable.ToString(CultureInfo.InvariantCulture);

        private static Dictionary<char, double> ParseWidths(string spec)
        {
            var map = new Dictionary<char, double>();
            foreach (var pair in spec.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                // Split on the last colon so pairs like "::278" (the colon glyph)
                // and "\\:278" (the backslash glyph) survive intact.
                var split = pair.LastIndexOf(':');
                if (split <= 0) continue;

                var glyph = pair[..split];
                var ch = glyph == "sp" ? ' ' : glyph[0];
                map[ch] = double.Parse(pair[(split + 1)..], CultureInfo.InvariantCulture);
            }
            return map;
        }

        // ====================================================================
        // PNG decoding (8-bit, non-interlaced) and zlib helpers
        // ====================================================================

        private sealed record DecodedPng(int Width, int Height, byte[] Rgb);

        private static DecodedPng? TryDecodePng(byte[]? png)
        {
            try
            {
                if (png is null || png.Length < 16) return null;

                ReadOnlySpan<byte> signature = stackalloc byte[8] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                if (!png.AsSpan(0, 8).SequenceEqual(signature)) return null;

                int width = 0, height = 0, bitDepth = 0, colorType = 0, interlace = 0;
                byte[]? palette = null, trns = null;
                using var idat = new MemoryStream();

                var pos = 8;
                while (pos + 12 <= png.Length)
                {
                    var length = BeInt(png, pos);
                    var type = Encoding.ASCII.GetString(png, pos + 4, 4);
                    var dataStart = pos + 8;
                    if (length < 0 || dataStart + length + 4 > png.Length) break;

                    if (type == "IHDR")
                    {
                        width = BeInt(png, dataStart);
                        height = BeInt(png, dataStart + 4);
                        bitDepth = png[dataStart + 8];
                        colorType = png[dataStart + 9];
                        interlace = png[dataStart + 12];
                    }
                    else if (type == "PLTE")
                    {
                        palette = png[dataStart..(dataStart + length)];
                    }
                    else if (type == "tRNS")
                    {
                        trns = png[dataStart..(dataStart + length)];
                    }
                    else if (type == "IDAT")
                    {
                        idat.Write(png, dataStart, length);
                    }
                    else if (type == "IEND")
                    {
                        break;
                    }

                    pos = dataStart + length + 4; // skip the CRC
                }

                if (width <= 0 || height <= 0 || width * height > 40_000_000) return null;
                if (bitDepth != 8 || interlace != 0) return null;
                if (colorType is not (0 or 2 or 3 or 4 or 6)) return null;

                var channels = colorType switch
                {
                    0 => 1, // grayscale
                    2 => 3, // RGB
                    3 => 1, // palette index
                    4 => 2, // grayscale + alpha
                    _ => 4  // RGBA
                };

                byte[] raw;
                idat.Position = 0;
                using (var zlib = new ZLibStream(idat, CompressionMode.Decompress))
                using (var output = new MemoryStream())
                {
                    zlib.CopyTo(output);
                    raw = output.ToArray();
                }

                var stride = width * channels;
                if (raw.Length < (stride + 1) * height) return null;

                var pixels = new byte[stride * height];
                var prev = new byte[stride];
                var current = new byte[stride];
                for (var row = 0; row < height; row++)
                {
                    var rowStart = row * (stride + 1);
                    var filter = raw[rowStart];
                    Array.Copy(raw, rowStart + 1, current, 0, stride);

                    switch (filter)
                    {
                        case 1: // Sub
                            for (var i = channels; i < stride; i++) current[i] += current[i - channels];
                            break;
                        case 2: // Up
                            for (var i = 0; i < stride; i++) current[i] += prev[i];
                            break;
                        case 3: // Average
                            for (var i = 0; i < stride; i++)
                            {
                                var left = i >= channels ? current[i - channels] : 0;
                                current[i] += (byte)((left + prev[i]) / 2);
                            }
                            break;
                        case 4: // Paeth
                            for (var i = 0; i < stride; i++)
                            {
                                var a = i >= channels ? current[i - channels] : 0;
                                var b = prev[i];
                                var c = i >= channels ? prev[i - channels] : 0;
                                var p = a + b - c;
                                var pa = Math.Abs(p - a);
                                var pb = Math.Abs(p - b);
                                var pc = Math.Abs(p - c);
                                var predictor = pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
                                current[i] += (byte)predictor;
                            }
                            break;
                    }

                    Array.Copy(current, 0, pixels, row * stride, stride);
                    (prev, current) = (current, prev);
                }

                // Composite onto white so the seal looks right on white paper.
                var rgb = new byte[width * height * 3];
                for (var p = 0; p < width * height; p++)
                {
                    var si = p * channels;
                    var di = p * 3;
                    int r, g, b, a = 255;

                    switch (colorType)
                    {
                        case 6:
                            r = pixels[si]; g = pixels[si + 1]; b = pixels[si + 2]; a = pixels[si + 3];
                            break;
                        case 2:
                            r = pixels[si]; g = pixels[si + 1]; b = pixels[si + 2];
                            break;
                        case 4:
                            r = g = b = pixels[si]; a = pixels[si + 1];
                            break;
                        case 0:
                            r = g = b = pixels[si];
                            break;
                        default: // palette
                            var index = pixels[si];
                            if (palette is null || index * 3 + 2 >= palette.Length)
                            {
                                r = g = b = 0;
                            }
                            else
                            {
                                r = palette[index * 3];
                                g = palette[index * 3 + 1];
                                b = palette[index * 3 + 2];
                            }
                            if (trns is not null && index < trns.Length) a = trns[index];
                            break;
                    }

                    rgb[di] = (byte)((r * a + 255 * (255 - a)) / 255);
                    rgb[di + 1] = (byte)((g * a + 255 * (255 - a)) / 255);
                    rgb[di + 2] = (byte)((b * a + 255 * (255 - a)) / 255);
                }

                return new DecodedPng(width, height, rgb);
            }
            catch
            {
                // A bad logo must never block the certificate itself.
                return null;
            }
        }

        private static byte[] ZlibCompress(byte[] data)
        {
            using var output = new MemoryStream();
            using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        private static int BeInt(byte[] bytes, int offset) =>
            (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];

        private static void Write(Stream stream, byte[] bytes) => stream.Write(bytes, 0, bytes.Length);
    }
}
