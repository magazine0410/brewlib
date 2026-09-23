using BrewLib.Data;
using OpenTK;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace BrewLib.Graphics.Text
{
    public class TextGenerator : IDisposable
    {
        private const bool debugFont = false;
        private static int debugSeed = 0;

        private static readonly SKColor textColor = new SKColor(255, 255, 255, 255);
        private static readonly SKColor shadowColor = new SKColor(0, 0, 0, 220);

        private Dictionary<string, SKTypeface> typefaces = new Dictionary<string, SKTypeface>();

        private ResourceContainer resourceContainer;

        public TextGenerator(ResourceContainer resourceContainer)
        {
            this.resourceContainer = resourceContainer;
        }

        /// <summary>
        /// Renders white text with a shadow, one line per line of text, horizontally centered.
        /// Returns null when only measuring.
        /// </summary>
        public SKBitmap CreateBitmap(string text, string fontName, float fontSize, Vector2 padding, out Vector2 textureSize, bool measureOnly)
        {
            if (string.IsNullOrEmpty(text)) text = " ";

            using (var font = SkiaText.CreateFont(getTypeface(fontName), fontSize))
            {
                var lines = text.Split('\n');
                var lineWidths = lines.Select(line => SkiaText.Measure(font, line)).ToArray();
                var textWidth = lineWidths.Max();

                var width = (int)(textWidth + padding.X * 2 + 1);
                var height = (int)(font.Spacing * lines.Length + padding.Y * 2 + 1);

                textureSize = new Vector2(width, height);
                if (measureOnly) return null;

                var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul));
                try
                {
                    using (var canvas = new SKCanvas(bitmap))
                    using (var shadowPaint = new SKPaint() { Color = shadowColor, IsAntialias = true })
                    using (var textPaint = new SKPaint() { Color = textColor, IsAntialias = true })
                    {
                        if (debugFont)
                        {
                            var r = new Random(debugSeed++);
                            canvas.Clear(new SKColor((byte)r.Next(100, 255), (byte)r.Next(100, 255), (byte)r.Next(100, 255)));
                        }
                        else canvas.Clear(SKColors.Transparent);

                        var baseline = padding.Y - font.Metrics.Ascent;
                        for (var i = 0; i < lines.Length; i++)
                        {
                            var x = padding.X + (textWidth - lineWidths[i]) * 0.5f;
                            SkiaText.Draw(canvas, font, lines[i], x + 1, baseline + 1, shadowPaint);
                            SkiaText.Draw(canvas, font, lines[i], x, baseline, textPaint);
                            baseline += font.Spacing;
                        }
                    }
                }
                catch (Exception)
                {
                    bitmap.Dispose();
                    throw;
                }
                return bitmap;
            }
        }

        private SKTypeface getTypeface(string name)
        {
            if (typefaces.TryGetValue(name, out SKTypeface typeface))
                return typeface;

            var bytes = resourceContainer.GetBytes(name, ResourceSource.Embedded);
            if (bytes != null)
            {
                using (var data = SKData.CreateCopy(bytes))
                    typeface = SKTypeface.FromData(data);

                if (typeface != null)
                    Trace.WriteLine($"Loaded font {typeface.FamilyName} for {name}");
                else Trace.WriteLine($"Failed to load font {name}");
            }

            if (typeface == null)
            {
                typeface = SKTypeface.FromFamilyName(name);
                Trace.WriteLine($"Using system font {typeface.FamilyName} for {name}");
            }

            typefaces.Add(name, typeface);
            return typeface;
        }

        #region IDisposable Support

        private bool disposedValue = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var typeface in typefaces.Values)
                        typeface.Dispose();
                }
                typefaces = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }
}
