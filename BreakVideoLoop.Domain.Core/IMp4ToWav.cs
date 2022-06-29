namespace BreakVideoLoop.Domain.Core
{
    public interface IMp4ToWav : IDisposable
    {
        Task<bool> TransformVideo(string url);
    }
}