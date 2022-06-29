namespace BreakVideoLoop.Domain.Core.Languages.Interfaces
{
    public interface IVoskRecognizer : IDisposable
    {
        void SetMaxAlternatives(int max_alternatives);
        void SetWords(bool words);
        bool AcceptWaveform(byte[] data, int len);
        bool AcceptWaveform(short[] sdata, int len);
        bool AcceptWaveform(float[] fdata, int len);
        string Result();
        string PartialResult();
        string FinalResult();
        void Reset();
    }
}