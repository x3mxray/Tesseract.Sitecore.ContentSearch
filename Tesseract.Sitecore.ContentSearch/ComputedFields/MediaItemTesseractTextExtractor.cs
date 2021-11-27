using Sitecore.ContentSearch;
using Sitecore.ContentSearch.ComputedFields;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Resources.Media;
using System;
using System.IO;
using Sitecore.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using BitMiracle.Docotic.Pdf;
using System.Text;

namespace Tesseract.Sitecore.ContentSearch.ComputedFields
{
    public class MediaItemTesseractTextExtractor : AbstractComputedIndexField
    {
        public override object ComputeFieldValue(IIndexable indexable)
        {
            Item item = indexable as SitecoreIndexableItem;
            if (item == null)
            {
                Log.Warn("Item to index is null", this);
                return null;
            }
            var media = MediaManager.GetMedia(item);

            if (media == null)
            {
                Log.Warn("Media to index is null", this);
                return null;
            }

            if (item.Fields["Extension"] == null)
            {
                Log.Warn("Field Extension is null", this);
                return null;
            }

            var mediaIndexingFolder = ContentSearchConfigurationSettingsWrapper.MediaIndexingFolder;
            var fileName = $"{Guid.NewGuid()}-{item.Name}.{item.Fields["Extension"].Value}";

            var tempFilePath = FileUtil.MakePath(mediaIndexingFolder, fileName);
            try
            {
                if (!Directory.Exists(mediaIndexingFolder))
                {
                    Directory.CreateDirectory(mediaIndexingFolder);
                }

                using (Stream file = File.OpenWrite(tempFilePath))
                {
                    using (var mediaStream = media.GetStream())
                    {
                        if (mediaStream == null)
                        {
                            Log.Warn("mediaStream is null", this);
                            return null;
                        }
                        CopyStream(mediaStream.Stream, file);
                    }
                }

                try
                {

                    var lang = "eng";
                    var itemLang = item.Language.ToString().ToLower();
                    if (!itemLang.StartsWith("en"))
                    {
                        var languageMapping = Factory.GetConfigNode("tesseract");
                        if (languageMapping != null)
                        {
                            var langs = Transform<XmlNode>(languageMapping.ChildNodes);
                            var l = langs.FirstOrDefault(x => x.Attributes["name"].Value == item.Language.ToString().ToLower());
                            if (l != null)
                                lang = l.InnerText;
                        }
                    }
                   

                    var basepath = AppDomain.CurrentDomain.BaseDirectory;
                    var tessdata = Settings.GetSetting("TessdataFolder");
                    var path = Path.Combine(basepath, tessdata);
                    var extention = item.Fields["Extension"].Value;

                    if(extention == "pdf")
                    {
                        var documentText = new StringBuilder();
                        using (var pdf = new PdfDocument(tempFilePath))
                        {
                            using (var engine = new TesseractEngine(path, lang, EngineMode.Default))
                            {
                                for (int i = 0; i < pdf.PageCount; ++i)
                                {
                                    if (documentText.Length > 0)
                                        documentText.Append("\r\n\r\n");

                                    PdfPage page = pdf.Pages[i];
                                    string searchableText = page.GetText();

                                    if (!string.IsNullOrEmpty(searchableText.Trim()))
                                    {
                                        documentText.Append(searchableText);
                                        continue;
                                    }

                                    // This page is not searchable.
                                    // Save the page as a high-resolution image
                                    PdfDrawOptions options = PdfDrawOptions.Create();
                                    options.BackgroundColor = new PdfRgbColor(255, 255, 255);
                                    options.HorizontalResolution = 300;
                                    options.VerticalResolution = 300;

                                    var tempfileName = $"{Guid.NewGuid()}-{item.Name}-page_{i}.{item.Fields["Extension"].Value}";
                                    string pageImage = FileUtil.MakePath(mediaIndexingFolder, tempfileName);
                                    page.Save(pageImage, options);

                                    using (var img = Pix.LoadFromFile(pageImage))
                                    {
                                        using (var recognizedPage = engine.Process(img))
                                        {
                                            var text = recognizedPage.GetText();
                                            Console.WriteLine("Mean confidence: {0}", recognizedPage.GetMeanConfidence());

                                            Console.WriteLine("Text (GetText): \r\n{0}", text);
                                            documentText.Append(text);

                                        }
                                    }
                                }

                                Console.WriteLine(documentText.ToString());
                            }
                        }

                        return documentText.ToString();
                    }


                    using (var engine = new TesseractEngine(path, lang, EngineMode.Default))
                    {
                        using (var img = Pix.LoadFromFile(tempFilePath))
                        {
                            using (var page = engine.Process(img))
                            {
                                var text = page.GetText();
                                Console.WriteLine("Mean confidence: {0}", page.GetMeanConfidence());

                                Console.WriteLine("Text (GetText): \r\n{0}", text);

                                Log.Warn("YYY extractedText=" + text, this);
                                return text;
                            }
                        }
                    }
                        //var textExtractor = new TextExtractor();
                        //var text = textExtractor.Extract(tempFilePath).Text;
                    return null;
                }
                catch (Exception e)
                {
                    Log.Warn(e.Message, this);
                    return null;
                }
            }
            catch (Exception exception)
            {
                Log.Warn(exception.Message, this);
                return null;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }

        public static IEnumerable<T> Transform<T>(System.Collections.IEnumerable enumerable)
        {
            return enumerable.Cast<object>().Where(current => (current as XmlElement) != null).Cast<T>();
        }

        private static void CopyStream(Stream input, Stream output)
        {
            var buffer = new byte[8 * 1024];
            int len;

            while ((len = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, len);
            }
        }
    }
}