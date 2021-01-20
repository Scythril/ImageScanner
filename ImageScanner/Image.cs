using ImageMagick;

namespace ImageScanner
{
    class Image
    {
        private static readonly MagickColor _white = new MagickColor("#FFFFFF");
        private const double WHITE_THRESHOLD = 0.6;

        public static string CropToIdRegion(string filename)
        {
            using (var image = new MagickImage(filename))
            {
                var geometry = new MagickGeometry(0, 0, image.Width, image.Height);
                var pixels = image.GetPixels();
                for (var y = image.Height - 1; y > -1; y--)
                {
                    var whiteCounter = 0;
                    var firstWhite = 0;
                    var lastWhite = 0;
                    for (var x = 0; x < image.Width; x++)
                    {
                        if (pixels[x, y].ToColor().Equals(_white))
                        {
                            if (whiteCounter == 0)
                            {
                                firstWhite = x;
                            }

                            whiteCounter++;
                            lastWhite = x;
                        }
                    }

                    if (geometry.Height == image.Height && (double)whiteCounter / image.Width > WHITE_THRESHOLD)
                    {
                        geometry.Height = y;
                        geometry.X = firstWhite;
                        geometry.Width = lastWhite - firstWhite;
                    }
                    else if (geometry.Height != image.Height && (double)whiteCounter / image.Width < WHITE_THRESHOLD)
                    {
                        geometry.Y = y + 1;
                        geometry.Height -= geometry.Y;
                        break;
                    }
                }

                var baseFile = filename.Substring(0, filename.LastIndexOf('.'));
                var newFilename = $"{baseFile}_cropped.jpg";
                image.Crop(geometry);
                image.Format = MagickFormat.Jpeg;
                image.Grayscale();
                image.InterpolativeResize(2 * image.Width, 2 * image.Height, PixelInterpolateMethod.Bilinear);
                image.Write(newFilename);
                return newFilename;
            }
        }
    }
}
