using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Core.Languages.Interfaces;
using BreakVideoLoop.Domain.Enums;
using Vosk;

namespace BreakVideoLoop.Domain.DI
{
    public class Mp4ToWavFactory : IDisposable
    {
        private static Mp4ToWav mp4ToWav;
        public static IMp4ToWav GetFactory(EnumLanguage enumLanguage, IServiceProvider serviceProvider)
        {
            mp4ToWav = new Mp4ToWav(serviceProvider, GetVoskInterface(enumLanguage, serviceProvider));
            return mp4ToWav;
        }

        public static IVoskRecognizer GetVoskInterface(EnumLanguage enumLanguage, IServiceProvider serviceProvider)
        {
            switch (enumLanguage)
            {
                case EnumLanguage.DE:
                    return (IVoskRecognizerDE)serviceProvider.GetService(typeof(IVoskRecognizerDE));
                case EnumLanguage.ES:
                    return (IVoskRecognizerES)serviceProvider.GetService(typeof(IVoskRecognizerES));
                case EnumLanguage.PT:
                    return (IVoskRecognizerPT)serviceProvider.GetService(typeof(IVoskRecognizerPT));
                case EnumLanguage.FR:
                    return (IVoskRecognizerFR)serviceProvider.GetService(typeof(IVoskRecognizerFR));
                case EnumLanguage.IT:
                    return (IVoskRecognizerIT)serviceProvider.GetService(typeof(IVoskRecognizerIT));
                default:
                    return (IVoskRecognizerEN)serviceProvider.GetService(typeof(IVoskRecognizerEN));
            }
        }

        public void Dispose()
        {
            mp4ToWav.Dispose();
        }
    }
}
