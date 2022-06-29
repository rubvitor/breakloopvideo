using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Core.Languages.Interfaces;
using BreakVideoLoop.Domain.Core.Models;
using BreakVideoLoop.Domain.DI;
using MediaToolkit;
using MediaToolkit.Model;
using PdfSharpCore.Drawing;
using PdfSharpCore.Drawing.Layout;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.IO.Abstractions;
using System.Net;
using VideoLibrary;

namespace BreakVideoLoop.Domain
{
    public class Mp4ToWav : IMp4ToWav
    {
        private readonly Engine engine;
        private readonly string source;
        private readonly string dateTime;
        private readonly string textPath;
        private readonly string pdfPath;
        private readonly IRekognizeVoice _rekognizeVoice;
        private readonly ISearchTools _searchTools;
        private readonly SemaphoreStrategy semaphoreStrategy;
        private readonly List<string> alreadyRead;
        private long StillCreatingPdf;

        public Mp4ToWav(IServiceProvider serviceProvider, IVoskRecognizer voskRecognizerBase)
        {
            var fileSystem = (IFileSystem)serviceProvider.GetService(typeof(IFileSystem));
            var searchTools = (ISearchTools)serviceProvider.GetService(typeof(ISearchTools));
            var rekognizeVoice = RecognizeVoiceFactory.GetFactory(voskRecognizerBase);

            alreadyRead = new List<string>();

            StillCreatingPdf = 0;

            semaphoreStrategy = new SemaphoreStrategy();

            dateTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            source = Path.Combine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"ffmpeg"), dateTime);

            var piecePath = Path.Combine(source, "pieces");

            engine = new Engine(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"ffmpeg\ffmpeg.exe"), fileSystem);

            if (Directory.Exists(source))
                Directory.Delete(source, true);

            Directory.CreateDirectory(source);


            if (Directory.Exists(piecePath))
                Directory.Delete(piecePath, true);

            Directory.CreateDirectory(piecePath);


            textPath = Path.Combine(source, "Text.txt");
            if (File.Exists(textPath))
                File.Delete(textPath);

            File.Create(textPath).Dispose();

            pdfPath = Path.Combine(source, "Dictionary.pdf");
            if (File.Exists(pdfPath))
                File.Delete(pdfPath);

            _rekognizeVoice = rekognizeVoice;

            _searchTools = searchTools;
        }

        public async Task<bool> TransformVideo(string url)
        {
            try
            {
                alreadyRead.Clear();

                var media = DownloadAudioQuick(url);
                if (media is not null)
                {
                    _ = BreakInPieces(media);
                    if (StillCreatingPdf > 0)
                        Thread.Sleep(1);

                    CreateFinalPdf();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task CreatePdfImagesText(string found, long position)
        {
            var words = found.Split(" ");
            if (words is not null && words.Any())
            {
                StillCreatingPdf++;
                try
                {
                    for (int i = 0; i < words.Count(); i++)
                        AddWordDictionary(ref i, words, position);
                }
                catch { }
                StillCreatingPdf--;
            }
        }

        private async Task CreateFinalPdf()
        {
            var dictionaryPages = Directory.GetFiles(source, "Dictionary_*", SearchOption.TopDirectoryOnly);
            if (dictionaryPages is not null && dictionaryPages.Any())
            {
                dictionaryPages = dictionaryPages.OrderBy(x => x)?.ToArray();

                using (var pdfDocument = new PdfDocument())
                {
                    foreach (var dictionaryPage in dictionaryPages)
                    {
                        using (var stream = new MemoryStream(File.ReadAllBytes(dictionaryPage)))
                        {
                            using (var currentDoc = PdfReader.Open(stream, PdfDocumentOpenMode.Import))
                            {
                                try
                                {
                                    pdfDocument.AddPage(currentDoc.Pages[0]);
                                }
                                catch { }
                            }
                        }


                        File.Delete(dictionaryPage);
                    }

                    pdfDocument.Save(pdfPath);
                }
            }
        }

        private void AddWordDictionary(ref int i, string[] words, long position)
        {
            var word = words[i];
            if (word.ToLower().Equals("figure"))
            {
                var a = "";
            }

            try
            {
                if (string.IsNullOrWhiteSpace(word) || alreadyRead.Contains(word))
                    return;

                var meaningResult = GetMeaning(word).Result;

                if (meaningResult is not null)
                {
                    try
                    {
                        if (meaningResult.Meanings is null || !meaningResult.Meanings.Any())
                            return;

                        int advances = 0;
                        using (var pdfDocument = new PdfDocument())
                        {
                            var page = pdfDocument.AddPage();

                            if (meaningResult.Meanings.Any(x => x.PartOfSpeech.Equals("verb")) && i + 1 < words.Length)
                            {
                                var nextWord = words[i + 1];
                                var meaningNextResult = GetMeaning(nextWord).Result;
                                if (meaningNextResult is not null && meaningNextResult.Meanings is not null && meaningNextResult.Meanings.Any())
                                {
                                    var nextResultFirst = meaningNextResult.Meanings.FirstOrDefault();

                                    if (nextResultFirst is not null)
                                    {
                                        if (nextResultFirst.PartOfSpeech.Equals("verb"))
                                        {
                                            word += $" {nextWord}";
                                            i++;
                                            advances = 1;
                                        }
                                        else if (i + 2 < words.Length && nextResultFirst.PartOfSpeech.Contains("noun"))
                                        {
                                            var secondNextWord = words[i + 2];
                                            var secondMeaningNextResult = GetMeaning(secondNextWord).Result;
                                            if (secondMeaningNextResult is not null && secondMeaningNextResult.Meanings is not null && secondMeaningNextResult.Meanings.Any() && secondMeaningNextResult.Meanings.FirstOrDefault(x => x.PartOfSpeech.Equals("verb")) != null)
                                            {
                                                word += $" {nextWord} {secondNextWord}";
                                                i += 2;
                                                advances = 2;
                                            }
                                        }
                                    }
                                }
                            }

                            if (advances > 0)
                            {
                                var meaningGathered = GetMeaning(word).Result;
                                if (meaningGathered is not null && meaningGathered.Meanings is not null && meaningGathered.Meanings.Any())
                                    meaningResult = meaningGathered;
                                else
                                {
                                    i -= advances;
                                    word = words[i];
                                }
                            }

                            var font = new XFont("Verdana", 12, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));

                            using (XGraphics graph = XGraphics.FromPdfPage(page))
                            {
                                XTextFormatter textFor = new XTextFormatter(graph);
                                textFor.Font = font;

                                double height = 220;

                                int x = 5;
                                int y = 5;

                                textFor.DrawString(word, new XFont("Verdana", 20, XFontStyle.Bold, new XPdfFontOptions(PdfFontEncoding.Unicode)), XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);

                                var phonetics = meaningResult.Phonetics?.Where(p => !string.IsNullOrWhiteSpace(p.Audio) && !string.IsNullOrWhiteSpace(p.Text));

                                if (phonetics is not null && phonetics.Any())
                                {
                                    y += 40;

                                    textFor.DrawString($"Phonetics: ", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);

                                    y += 15;

                                    textFor.DrawString($"Text: {phonetics.FirstOrDefault().Text}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);
                                    foreach (var phonetic in phonetics)
                                    {
                                        y += 15;
                                        textFor.DrawString($"Audio: {phonetic.Audio}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);
                                    }
                                }

                                foreach (var meaningOne in meaningResult.Meanings)
                                {
                                    y += 25;

                                    textFor.DrawString($"MEANING {meaningOne.PartOfSpeech}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);

                                    var definition = meaningOne.Definitions?.FirstOrDefault();
                                    if (definition is not null)
                                    {
                                        y += 20;

                                        if (!string.IsNullOrWhiteSpace(definition.DefinitionText))
                                        {
                                            textFor.DrawString($"Definition: {definition.DefinitionText}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);
                                            var calc = definition.DefinitionText.Length / 80;
                                            if (calc == 0)
                                                calc = 1;

                                            y += calc * 30;
                                        }

                                        if (!string.IsNullOrWhiteSpace(definition.Example))
                                        {
                                            textFor.DrawString($"Example: {definition.Example}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);

                                            var calc = definition.Example.Length / 80;
                                            if (calc == 0)
                                                calc = 1;

                                            y += calc * 30;
                                        }

                                        var syn = definition.Synonyms;
                                        if (syn is not null && syn.Any())
                                        {
                                            y += 20;
                                            textFor.DrawString($"Synonym: {syn.FirstOrDefault()}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);
                                        }

                                        var ant = definition.Antonyms;
                                        if (ant is not null && ant.Any())
                                        {
                                            y += 20;
                                            textFor.DrawString($"Antonym: {ant.FirstOrDefault()}", font, XBrushes.Black, new XRect(x, y, page.Width, height), XStringFormats.TopLeft);
                                        }
                                    }
                                }

                                var imageResult = SearchImages(word).Result;

                                if (imageResult is not null)
                                {
                                    y += 10;
                                    int j = 1;
                                    if (imageResult.Count > 4)
                                        imageResult = imageResult.Take(4).ToList();

                                    var middle = ((int)page.Width.Value / 2) - 200;
                                    foreach (var image in imageResult)
                                    {
                                        try
                                        {
                                            if (j == 3)
                                            {
                                                j = 1;
                                                y += 100;
                                            }

                                            var imageFunc = GetImageFromUrl(image.Urls.Thumb);
                                            using (var imageX = XImage.FromStream(imageFunc))
                                                graph.DrawImage(imageX, (j * 100) + middle, y, 100, 100);

                                            imageFunc().Dispose();

                                            j++;
                                        }
                                        catch { }
                                    }
                                }
                            }

                            if (pdfDocument.PageCount > 0)
                                pdfDocument.Save(pdfPath.Replace("Dictionary", $"Dictionary_{position}_{i}"));
                        }
                    }
                    catch { }
                }
            }
            catch { }

            alreadyRead.Add(word);
        }

        private Func<Stream> GetImageFromUrl(string url)
        {
            using (var webClient = new WebClient())
            {
                var data = webClient.DownloadData(url);

                return new Func<MemoryStream>(() => new MemoryStream(data));
            }
        }

        private async Task<List<Result>> SearchImages(string word)
        {
            return (await _searchTools.GetImageArchive(word)).Results;
        }

        private async Task<DictionaryModel> GetMeaning(string word)
        {
            return await _searchTools.GetMeaning(word);
        }

        private List<Tuple<long, double, double>> CalcPieces(MediaFile media)
        {
            List<Tuple<long, double, double>> tuples = new List<Tuple<long, double, double>>();
            double i = 0;
            long count = 1;
            bool final = false;
            double seconds = 5;

            while (i <= media.Metadata.Duration.TotalSeconds && !final)
            {
                try
                {

                    final = i + 5 > media.Metadata.Duration.TotalSeconds;
                    if (final)
                        seconds = Convert.ToInt32(media.Metadata.Duration.TotalSeconds - i);

                    tuples.Add(new Tuple<long, double, double>(count, i, seconds));

                    i += 5;
                    count++;
                }
                catch { }
            }

            return tuples;
        }

        private bool BreakInPieces(MediaFile media)
        {
            Dictionary<long, string> textLines = new Dictionary<long, string>();

            var pieces = CalcPieces(media);
            Parallel.ForEach(pieces, new ParallelOptions { MaxDegreeOfParallelism = 10 }, (item, state, position) =>
            {
                try
                {
                    var destinationFile = $@"{media.Filename.Replace(".mp4", "").Replace(dateTime, $@"{dateTime}\pieces")}_{item.Item1}_base.wav";

                    var command = $@"-ss {item.Item2} -i ""{media.Filename.Replace("mp4", "wav")}"" -c copy -t {item.Item3} ""{destinationFile}""";

                    semaphoreStrategy.ExecSemaphore(() => engine.CustomCommand(command));

                    var text = _rekognizeVoice.GetText(destinationFile);
                    if (!string.IsNullOrEmpty(text))
                    {
                        textLines.Add(item.Item1, text);

                        Task.Run(() => CreatePdfImagesText(text, item.Item1));

                        var repeatCommand = $@"-stream_loop 30 -i ""{destinationFile}"" ""{destinationFile.Replace("_base", "").Replace("wav", "mp3")}""";

                        semaphoreStrategy.ExecSemaphore(() => engine.CustomCommand(repeatCommand));
                    }

                    File.Delete(destinationFile);
                }
                catch { }
            });

            if (textLines is not null)
            {
                foreach (var text in textLines.OrderBy(x => x.Key))
                {
                    File.AppendAllText(textPath, text.Value);
                    File.AppendAllText(textPath, Environment.NewLine);
                }
            }

            return true;
        }

        private MediaFile? DownloadAudioQuick(string url)
        {
            var youtube = YouTube.Default;
            var vid = youtube.GetVideo(url);

            var fullName = vid.FullName.Trim().ToLower().Replace(" ", string.Empty);

            var path = $@"{source}\{fullName}";

            File.WriteAllBytes(path, vid.GetBytes());

            var inputFile = new MediaFile { Filename = Path.Combine(source, fullName) };
            var outputFile = new MediaFile { Filename = Path.Combine(source, fullName.Replace("mp4", "wav")) };

            engine.GetMetadata(inputFile);
            engine.Convert(inputFile, outputFile);

            return inputFile;
        }

        public void Dispose()
        {
            engine.Dispose();
        }
    }
}
