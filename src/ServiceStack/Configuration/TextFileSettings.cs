using System.IO;

namespace ServiceStack.Configuration
{
    public class TextFileSettings : DictionarySettings
    {
        public TextFileSettings(string fileName, string delimiter = " ") : base(File.ReadAllText(fileName).ParseKeyValueText(delimiter)) { }
    }
}