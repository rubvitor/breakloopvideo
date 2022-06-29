using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Core.Languages.Interfaces;

namespace BreakVideoLoop.Domain.DI
{
    public class RecognizeVoiceFactory : IDisposable
    {
        private static RekognizeVoice rekognizeVoice;
        public static IRekognizeVoice GetFactory(IVoskRecognizer voskRecognizerBase)
        {
            rekognizeVoice = new RekognizeVoice(voskRecognizerBase);
            return rekognizeVoice;
        }

        public void Dispose()
        {
            rekognizeVoice.Dispose();
        }
    }
}
