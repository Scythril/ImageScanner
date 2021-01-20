using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Tesseract;

namespace ImageScanner
{
    class Parser
    {
        private static bool _debug { get; set; }
        private static string _screenshotFolder { get; set; }
        private static string _idRegex { get; set; }

        public Parser(bool debug, string screenshotFolder, string idRegex)
        {
            _debug = debug;
            _screenshotFolder = screenshotFolder;
            _idRegex = idRegex;
        }

        public string GetIdFromImage(string filename)
        {
            try
            {
                var id = string.Empty;
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(filename))
                    {
                        img.XRes = img.YRes = 600;
                        if (_debug)
                        {
                            var pixPath = Path.Combine(_screenshotFolder, "pix");

                            // This is the image after Pix has processed the image (after scaling, grayscale)
                            img.Save(Path.Combine(pixPath, Path.GetFileName(filename)));
                        }

                        id = ParseImageForId(engine, img, filename);
                    }
                }

                Console.WriteLine($"ID is {id}");
                if (!string.IsNullOrWhiteSpace(id) && Path.GetFileName(filename).StartsWith($"{id.Replace(":", "COLON")}"))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("WE GOT A MATCH!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    Console.ResetColor();
                }

                return id;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }

            return null;
        }

        private string ParseImageForId(TesseractEngine engine, Pix image, string filename)
        {
            using (var page = engine.Process(image))
            {
                return FindIdFromPage(page, filename);
            }
        }

        private string FindIdFromPage(Page page, string filename)
        {
            if (_debug)
            {
                var ocrPath = Path.Combine(_screenshotFolder, "ocr");

                // This is the image that Tesseract is using
                page.GetThresholdedImage().Save(Path.Combine(ocrPath, Path.GetFileName(filename)));

                var text = page.GetText();
                Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());
                Console.WriteLine("Text (GetText): \r\n{0}", text);
            }

            using (var iter = page.GetIterator())
            {
                iter.Begin();

                do
                {
                    do
                    {
                        do
                        {
                            var foundString = iter.GetText(PageIteratorLevel.TextLine).Trim();
                            if (Regex.IsMatch(foundString, _idRegex))
                            {
                                return FixMisinterpretations(foundString);
                            }
                        }
                        while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    }
                    while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                }
                while (iter.Next(PageIteratorLevel.Block));
            }

            return null;
        }

        private string FixMisinterpretations(string value)
        {
            // Massage some common misinterpretations
            var fixedValue = value.Replace(" ", "").Replace("£", "E");
            if (fixedValue.StartsWith("E1"))
            {
                var chars = fixedValue.ToCharArray();
                chars[1] = 'I';
                fixedValue = new string(chars);
            }

            if (fixedValue.StartsWith("6:"))
            {
                var chars = fixedValue.ToCharArray();
                chars[0] = 'G';
                fixedValue = new string(chars);
            }

            return fixedValue;
        }
    }
}
