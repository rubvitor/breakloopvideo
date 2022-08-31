using BreakVideoLoop.Domain.Core;
using BreakVideoLoop.Domain.Core.Models;
using BreakVideoLoop.Domain.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BreakVideoLoop.Domain
{
    public class SearchTools : ISearchTools
    {
        private readonly IOptions<ConfigurationBreakLoop> _config;

        public SearchTools(IOptions<ConfigurationBreakLoop> config)
        {
            _config = config;
        }

        public async Task<ImageModel> GetImageArchive(string sentence)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Client-ID {_config.Value.SearchAccessKey}");

                httpClient.BaseAddress = new Uri(_config.Value.SearchUrlBase);
                var result = await (await httpClient.GetAsync(string.Format(_config.Value.SearchUrl, sentence))).Content?.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(result))
                    return JsonConvert.DeserializeObject<ImageModel>(result);
            }

            return null;
        }

        public async Task<DictionaryModel> GetMeaning(string sentence)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_config.Value.DictionaryBase);
                var result = await httpClient.GetAsync(string.Format(_config.Value.Dictionary, sentence));

                if (result is not null && result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var stringResult = await result.Content?.ReadAsStringAsync();
                    var html = new HtmlAgilityPack.HtmlDocument();
                    html.LoadHtml(stringResult);

                    if (!string.IsNullOrEmpty(stringResult))
                        return JsonConvert.DeserializeObject<DictionaryModel[]>(stringResult)?.FirstOrDefault();
                }
            }

            return null;
        }
    }
}
