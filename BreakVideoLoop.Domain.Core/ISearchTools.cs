using BreakVideoLoop.Domain.Core.Models;

namespace BreakVideoLoop.Domain.Core
{
    public interface ISearchTools
    {
        Task<ImageModel> GetImageArchive(string sentence);
        Task<DictionaryModel> GetMeaning(string sentence);
    }
}
