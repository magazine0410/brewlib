using SkiaSharp;
using System.IO;

namespace BrewLib.Util
{
    public static class BitmapLoader
    {
        /// <summary>
        /// Loads an image as BGRA pixels that aren't premultiplied, like System.Drawing did.
        /// Returns null if the file isn't a supported image.
        /// </summary>
        public static SKBitmap Load(string path)
        {
            using (var stream = File.OpenRead(path))
                return Decode(stream);
        }

        /// <summary>
        /// Decodes an image as BGRA pixels that aren't premultiplied, like System.Drawing did.
        /// Returns null if the stream isn't a supported image.
        /// </summary>
        public static SKBitmap Decode(Stream stream)
        {
            using (var data = SKData.Create(stream))
            using (var codec = data != null ? SKCodec.Create(data) : null)
            {
                if (codec == null)
                    return null;

                var info = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
                var bitmap = new SKBitmap(info);

                var result = codec.GetPixels(info, bitmap.GetPixels());
                if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput)
                {
                    bitmap.Dispose();
                    return null;
                }
                return bitmap;
            }
        }
    }
}
