using BreakVideoLoop.Domain.Core.Languages.Interfaces;
using Vosk;

namespace BreakVideoLoop.Domain.Core.Languages
{
    public class VoskRecognizerPT : VoskRecognizer, IVoskRecognizerPT
    {
        public VoskRecognizerPT(Model model, float sample_rate) : base(model, sample_rate)
        {
        }

        public VoskRecognizerPT(Model model, float sample_rate, SpkModel spk_model) : base(model, sample_rate, spk_model)
        {
        }

        public VoskRecognizerPT(Model model, float sample_rate, string grammar) : base(model, sample_rate, grammar)
        {
        }
    }
}

