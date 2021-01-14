using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Tesseract;

namespace ImageScanner
{
    class Program
    {
        private const bool DEBUG = true;
        private const string ID_REGEX = @"^[a-zA-Z][0-9]{20}$";

        static void Main(string[] args)
        {
            var id = string.Empty;
            var imagePath = "./Egg_Inc_user_id_screenshot.jpg";
            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(imagePath).Scale(3, 3).ConvertRGBToGray())
                    {
                        img.XRes = img.YRes = 300;
                        if (DEBUG)
                        {
                            // This is the image after Pix has processed the image (after scaling, grayscale)
                            img.Save("./pix_image.jpg");
                        }

                        // Look at bottom left quadrant where the ID should be located in a full-screen screenshot
                        var y = img.Height * 60 / 100;
                        var region = new Rect(0, y, img.Width / 2, img.Height - y);
                        id = ParseImageForId(engine, img, region);
                        
                        // If it can't be found, look at entire image in case it was cropped beforehand
                        if (string.IsNullOrEmpty(id))
                        {
                            Console.WriteLine("Couldn't find ID in region, scanning entire image.");
                            id = ParseImageForId(engine, img);
                        }
                    }
                }

                Console.WriteLine($"ID is {id}");
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Console.WriteLine("Unexpected Error: " + e.Message);
                Console.WriteLine("Details: ");
                Console.WriteLine(e.ToString());
            }
        }

        static string ParseImageForId(TesseractEngine engine, Pix image)
        {
            using (var page = engine.Process(image))
            {
                return FindIdInImage(page);
            }
        }

        static string ParseImageForId(TesseractEngine engine, Pix image, Rect reg)
        {
            using (var page = engine.Process(image, reg))
            {
                return FindIdInImage(page);
            }
        }

        static string FindIdInImage(Page page)
        {
            if (DEBUG)
            {
                // This is the image that Tesseract is using
                page.GetThresholdedImage().Save("./scanned_image.jpg");

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
                            do
                            {
                                var foundString = iter.GetText(PageIteratorLevel.Word).Trim();
                                if (Regex.IsMatch(foundString, ID_REGEX))
                                {
                                    return foundString;
                                }
                            }
                            while (iter.Next(PageIteratorLevel.TextLine, PageIteratorLevel.Word));
                        }
                        while (iter.Next(PageIteratorLevel.Para, PageIteratorLevel.TextLine));
                    }
                    while (iter.Next(PageIteratorLevel.Block, PageIteratorLevel.Para));
                }
                while (iter.Next(PageIteratorLevel.Block));
            }

            return null;
        }
    }
}
