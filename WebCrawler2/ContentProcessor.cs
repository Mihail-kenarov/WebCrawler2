using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler2
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    namespace WebsiteOllamaAssistant
    {
        public class ContentProcessor
        {
            // HashSet of common stop words to exclude from keyword extraction
            private readonly HashSet<string> _stopWords;

            public ContentProcessor()
            {
                // Initialize stopwords
                _stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "a", "about", "above", "after", "again", "against", "all", "am", "an", "and", "any", "are", "aren't",
                "as", "at", "be", "because", "been", "before", "being", "below", "between", "both", "but", "by",
                "can't", "cannot", "could", "couldn't", "did", "didn't", "do", "does", "doesn't", "doing", "don't",
                "down", "during", "each", "few", "for", "from", "further", "had", "hadn't", "has", "hasn't", "have",
                "haven't", "having", "he", "he'd", "he'll", "he's", "her", "here", "here's", "hers", "herself", "him",
                "himself", "his", "how", "how's", "i", "i'd", "i'll", "i'm", "i've", "if", "in", "into", "is", "isn't",
                "it", "it's", "its", "itself", "let's", "me", "more", "most", "mustn't", "my", "myself", "no", "nor",
                "not", "of", "off", "on", "once", "only", "or", "other", "ought", "our", "ours", "ourselves", "out",
                "over", "own", "same", "shan't", "she", "she'd", "she'll", "she's", "should", "shouldn't", "so", "some",
                "such", "than", "that", "that's", "the", "their", "theirs", "them", "themselves", "then", "there",
                "there's", "these", "they", "they'd", "they'll", "they're", "they've", "this", "those", "through", "to",
                "too", "under", "until", "up", "very", "was", "wasn't", "we", "we'd", "we'll", "we're", "we've", "were",
                "weren't", "what", "what's", "when", "when's", "where", "where's", "which", "while", "who", "who's",
                "whom", "why", "why's", "with", "won't", "would", "wouldn't", "you", "you'd", "you'll", "you're", "you've",
                "your", "yours", "yourself", "yourselves", "what", "who", "where", "when", "why", "how", "does", "do",
                "is", "can"
            };
            }

            public string ExtractUrl(string content)
            {
                var match = Regex.Match(content, @"Source URL: (.+)");
                return match.Success ? match.Groups[1].Value.Trim() : "Unknown URL";
            }

            public string ExtractTitle(string content)
            {
                var match = Regex.Match(content, @"Title: (.+)");
                return match.Success ? match.Groups[1].Value.Trim() : "Untitled Page";
            }

            public List<string> ExtractHeadings(string content)
            {
                var headings = new List<string>();
                var matches = Regex.Matches(content, @"H\d: (.+)");

                foreach (Match match in matches)
                {
                    headings.Add(match.Groups[1].Value.Trim());
                }

                return headings;
            }

            public List<string> ExtractKeywords(string text)
            {
                // Split the text into words
                var words = Regex.Matches(text.ToLower(), @"\b[a-z0-9]+\b")
                    .Cast<Match>()
                    .Select(m => m.Value)
                    .Where(w => !_stopWords.Contains(w) && w.Length > 2)
                    .Distinct()
                    .ToList();

                return words;
            }
        }
    }
}