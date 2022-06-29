using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Core.Languages.Interfaces;
using Newtonsoft.Json;

namespace BreakVideoLoop.Domain
{
    public class RekognizeVoice : IRekognizeVoice
    {
        private readonly IVoskRecognizer _voskRecognizer;
        public readonly SemaphoreStrategy semaphoreStrategy;
        public RekognizeVoice(IVoskRecognizer voskRecognizerBase)
        {
            _voskRecognizer = voskRecognizerBase;
            semaphoreStrategy = new SemaphoreStrategy();
        }

        public string GetText(string path)
        {
            try
            {
                string result = string.Empty;

                var file = File.ReadAllBytes(path);

                semaphoreStrategy.ExecSemaphore(() =>
                {
                    _voskRecognizer.Reset();
                    var recognizer = _voskRecognizer.AcceptWaveform(file, file.Length);

                    if (recognizer)
                        result = _voskRecognizer.Result();
                    else
                        result = _voskRecognizer.PartialResult();
                });

                if (!string.IsNullOrEmpty(result))
                {
                    var voskResult = JsonConvert.DeserializeObject<VoskResult>(result);

                    return voskResult.Text ?? voskResult.Partial;
                }
            }
            catch { }

            return string.Empty;
        }

        public void Dispose()
        {
            _voskRecognizer.Dispose();
        }
    }

    public class VoskResult
    {
        public string? Text { get; set; }
        public string? Partial { get; set; }
    }
}
