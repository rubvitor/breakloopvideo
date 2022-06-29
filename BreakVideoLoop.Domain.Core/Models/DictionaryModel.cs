using Newtonsoft.Json;

namespace BreakVideoLoop.Domain.Core.Models
{
    public class Definition
    {
        [JsonProperty("Definition")]
        public string DefinitionText { get; set; }
        public string Example { get; set; }
        public List<object> Synonyms { get; set; }
        public List<object> Antonyms { get; set; }
    }

    public class Meaning
    {
        public string PartOfSpeech { get; set; }
        public List<Definition> Definitions { get; set; }
    }

    public class Phonetic
    {
        public string Text { get; set; }
        public string Audio { get; set; }
    }

    public class DictionaryModel
    {
        public string Word { get; set; }
        public string Phonetic { get; set; }
        public List<Phonetic> Phonetics { get; set; }
        public string Origin { get; set; }
        public List<Meaning> Meanings { get; set; }
    }
}
