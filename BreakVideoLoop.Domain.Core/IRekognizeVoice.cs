namespace BreakVideoLoop.Domain.Core
{
    public interface IRekognizeVoice : IDisposable
    {
        string GetText(string path);
    }
}
