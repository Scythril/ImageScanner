using System;
using System.IO;

namespace ImageScanner
{
    class Program
    {
        private const bool DEBUG = true;

        private const string ID_REGEX = @"^[E£][I1].+$";
        private static readonly string SCREENSHOT_FOLDER = $".{Path.DirectorySeparatorChar}screenshots{Path.DirectorySeparatorChar}ArtifactsUpdate";
        /*private const string ID_REGEX = @"^[A-Za-z0-9:-]{10,}$";
        private static readonly string SCREENSHOT_FOLDER = $".{Path.DirectorySeparatorChar}screenshots{Path.DirectorySeparatorChar}CurrentVersion";*/

        static void Main(string[] args)
        {
            var parser = new Parser(DEBUG, SCREENSHOT_FOLDER, ID_REGEX);

            if (DEBUG)
            {
                var dirs = new[] { "pix", "ocr" };
                foreach (var dir in dirs)
                {
                    var path = Path.Combine(SCREENSHOT_FOLDER, dir);
                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }

                    Directory.CreateDirectory(path);
                }
            }

            foreach (var filename in Directory.EnumerateFiles(SCREENSHOT_FOLDER))
            {
                Console.WriteLine("".PadLeft(80, '#'));
                Console.WriteLine($"Filename is {filename}");
                var newFile = Image.CropToIdRegion(filename);
                parser.GetIdFromImage(newFile);
            }
        }
    }
}
