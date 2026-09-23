using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace BrewLib.Graphics.Text
{
    /// <summary>
    /// Measures and draws a line of text, using system fonts for the characters missing from the font.
    /// </summary>
    public static class SkiaText
    {
        /// <summary>
        /// Creates a font the way storybrew renders text: antialiased and without hinting.
        /// The size is in points, at 96 dpi.
        /// </summary>
        public static SKFont CreateFont(SKTypeface typeface, float pointSize)
            => new SKFont(typeface, pointSize * 96f / 72f)
            {
                Edging = SKFontEdging.Antialias,
                Hinting = SKFontHinting.None,
                Subpixel = true,
            };

        public static float Measure(SKFont font, string text)
        {
            var width = 0f;
            foreach (var run in getRuns(font, text))
                using (run)
                    width += run.Font.MeasureText(run.Text);
            return width;
        }

        public static void Draw(SKCanvas canvas, SKFont font, string text, float x, float baseline, SKPaint paint)
        {
            foreach (var run in getRuns(font, text))
                using (run)
                {
                    canvas.DrawText(run.Text, x, baseline, SKTextAlign.Left, run.Font, paint);
                    x += run.Font.MeasureText(run.Text);
                }
        }

        private static List<Run> getRuns(SKFont font, string text)
        {
            var runs = new List<Run>();
            var builder = new StringBuilder();
            SKTypeface currentTypeface = null;

            foreach (var rune in text.EnumerateRunes())
            {
                var typeface = font.Typeface;
                if (!Rune.IsWhiteSpace(rune) && !Rune.IsControl(rune) && font.GetGlyph(rune.Value) == 0)
                    typeface = SKFontManager.Default.MatchCharacter(font.Typeface.FamilyName, font.Typeface.FontStyle, null, rune.Value) ?? font.Typeface;

                if (builder.Length > 0 && typeface.FamilyName != currentTypeface.FamilyName)
                {
                    runs.Add(new Run(font, currentTypeface, builder.ToString()));
                    builder.Clear();
                }
                if (builder.Length == 0)
                    currentTypeface = typeface;

                builder.Append(rune.ToString());
            }
            if (builder.Length > 0)
                runs.Add(new Run(font, currentTypeface, builder.ToString()));

            return runs;
        }

        private class Run : IDisposable
        {
            public readonly SKFont Font;
            public readonly string Text;
            private readonly bool ownsFont;

            public Run(SKFont font, SKTypeface typeface, string text)
            {
                Text = text;
                if (typeface == font.Typeface)
                {
                    Font = font;
                    return;
                }

                Font = new SKFont(typeface, font.Size, font.ScaleX, font.SkewX)
                {
                    Edging = font.Edging,
                    Hinting = font.Hinting,
                    Subpixel = font.Subpixel,
                    Embolden = font.Embolden,
                };
                ownsFont = true;
            }

            public void Dispose()
            {
                if (ownsFont)
                    Font.Dispose();
            }
        }
    }
}
