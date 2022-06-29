using BreakVideoLoop.Domain.Core.Languages;
using BreakVideoLoop.Domain.Core.Languages.Interfaces;
using BreakVideoLoop.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace BreakVideoLoop.Domain.DI
{
    public static class VoskInjection
    {
        private static readonly int SampleRate = 100000;
        public static IServiceCollection AddVoskInjection(this IServiceCollection services)
        {
            Vosk.Vosk.SetLogLevel(0);
            List<Task> injectionsTask = new List<Task>();

            var languageClasses = from t in Assembly.Load("BreakVideoLoop.Domain.Core").GetTypes()
                                  where t.IsClass
                                  && t.Namespace.Equals("BreakVideoLoop.Domain.Core.Languages")
                                  select t;

            foreach (var languageClass in languageClasses)
            {
                var languageInit = languageClass.Name.Split("VoskRecognizer").Last();
                var voskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"vosk-model-{languageInit.ToLower()}");

                if (Directory.Exists(voskPath))
                {
                    injectionsTask.Add(Task.Run(() =>
                    {
                        GetInstance(services, languageInit, voskPath);
                    }));
                }
            }

            Task.WaitAll(injectionsTask.ToArray());

            return services;
        }

        private static void GetInstance(IServiceCollection services, string init, string path)
        {
            EnumLanguage enumLan = (EnumLanguage)Enum.Parse(typeof(EnumLanguage), init);
            switch (enumLan)
            {
                case EnumLanguage.FR:
                    services.AddScoped<IVoskRecognizerFR>(x => new VoskRecognizerFR(new(path), SampleRate));
                    break;
                case EnumLanguage.IT:
                    services.AddScoped<IVoskRecognizerIT>(x => new VoskRecognizerIT(new(path), SampleRate));
                    break;
                case EnumLanguage.DE:
                    services.AddScoped<IVoskRecognizerDE>(x => new VoskRecognizerDE(new(path), SampleRate));
                    break;
                case EnumLanguage.PT:
                    services.AddScoped<IVoskRecognizerPT>(x => new VoskRecognizerPT(new(path), SampleRate));
                    break;
                case EnumLanguage.ES:
                    services.AddScoped<IVoskRecognizerES>(x => new VoskRecognizerES(new(path), SampleRate));
                    break;
                default:
                    services.AddScoped<IVoskRecognizerEN>(x => new VoskRecognizerEN(new(path), SampleRate));
                    break;
            }
        }
    }
}
