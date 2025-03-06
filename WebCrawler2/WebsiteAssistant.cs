using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebCrawler2.WebsiteOllamaAssistant;

namespace WebCrawler2
{
    public class WebsiteAssistant
    {
        // Configuration
        private readonly string _dataFolder = "crawled_data";
        private readonly string _modelName = "llama3.2:latest"; // Change to your installed model (e.g., llama3, mistral, gemma)
        private readonly int _contextSize = 8000; // Adjust based on your model's capabilities

        // Store indexed pages
        private List<PageData> _pageIndex = new List<PageData>();

        // Ollama API client
        private OllamaApiClient _ollama = new OllamaApiClient("http://localhost:11434");

        // Content processor
        private ContentProcessor _contentProcessor;

        // LLM Interface
        private LanguageModelInterface _llmInterface;

        public WebsiteAssistant()
        {
            _contentProcessor = new ContentProcessor();
            _llmInterface = new LanguageModelInterface(_modelName);
        }

        public async Task<bool> Initialize()
        {
            // Check if data folder exists
            if (!Directory.Exists(_dataFolder))
            {
                Console.WriteLine($"Error: Folder '{_dataFolder}' not found. Please run the web crawler first.");
                return false;
            }

            Console.WriteLine("Loading website content...");
            await Task.Run(() => LoadAndIndexContent());

            if (_pageIndex.Count == 0)
            {
                Console.WriteLine("No text files found in the crawled_data folder. Please run the web crawler first.");
                return false;
            }

            Console.WriteLine($"Loaded {_pageIndex.Count} pages from the website.");

            Console.WriteLine($"Initializing Ollama with model '{_modelName}'...");

            try
            {
                // Check if the model is available
                var models = await _ollama.ListLocalModelsAsync();
                bool modelExists = models.Any(m => m.Name.Equals(_modelName, StringComparison.OrdinalIgnoreCase));

                if (!modelExists)
                {
                    Console.WriteLine($"Warning: Model '{_modelName}' not found in Ollama.");
                    Console.WriteLine("Available models:");
                    foreach (var model in models)
                    {
                        Console.WriteLine($"  - {model.Name}");
                    }

                    Console.WriteLine("\nPlease update the _modelName variable in the code to use one of these models.");
                    Console.WriteLine($"Or install the model with: ollama pull {_modelName}");

                    // Ask if the user wants to continue anyway
                    Console.Write("\nDo you want to try continuing anyway? (y/n): ");
                    string response = Console.ReadLine()?.ToLower() ?? "n";
                    return response == "y";
                }

                Console.WriteLine("Ollama initialization successful!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to Ollama: {ex.Message}");
                Console.WriteLine("Make sure Ollama is running on this machine (http://localhost:11434 by default).");
                Console.WriteLine("You can download Ollama from: https://ollama.ai/");
                return false;
            }
        }

        public void ShowHelp()
        {
            Console.WriteLine("\nAvailable commands:");
            Console.WriteLine("  help                - Show this help message");
            Console.WriteLine("  list                - List all indexed pages");
            Console.WriteLine("  search [term]       - Search for specific terms in the content");
            Console.WriteLine("  exit                - Exit the program");
            Console.WriteLine("\nFor any other input, the assistant will try to answer your question.");
        }

        public void ListPages()
        {
            Console.WriteLine("\nIndexed Website Pages:");
            for (int i = 0; i < _pageIndex.Count; i++)
            {
                Console.WriteLine($"  {i + 1}. {_pageIndex[i].Title}");
                Console.WriteLine($"     URL: {_pageIndex[i].Url}");
            }
        }

        public void SearchContent(string searchTerm)
        {
            Console.WriteLine($"\nSearching for '{searchTerm}':");
            int matches = 0;

            foreach (var page in _pageIndex)
            {
                if (page.Content.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  Found in: {page.Title}");
                    Console.WriteLine($"  URL: {page.Url}");
                    matches++;

                    // Show a small context snippet
                    int index = page.Content.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase);
                    int startIndex = Math.Max(0, index - 50);
                    int length = Math.Min(100, page.Content.Length - startIndex);
                    string snippet = page.Content.Substring(startIndex, length);

                    // Highlight the search term
                    snippet = Regex.Replace(snippet, searchTerm, match => $"[{match.Value}]", RegexOptions.IgnoreCase);
                    Console.WriteLine($"  Context: \"...{snippet}...\"");
                    Console.WriteLine();
                }
            }

            if (matches == 0)
            {
                Console.WriteLine("  No matches found on the website.");
            }
            else
            {
                Console.WriteLine($"  Found matches in {matches} pages.");
            }
        }

        public async Task AskQuestion(string question)
        {
            // Find relevant content for the question
            var relevantContent = FindRelevantContent(question, _contextSize);

            // Process with two-stage approach
            await _llmInterface.ProcessQuestionWithTwoStageApproach(question, relevantContent, _ollama);
        }

        private void LoadAndIndexContent()
        {
            try
            {
                foreach (var file in Directory.GetFiles(_dataFolder, "*.txt"))
                {
                    string fileName = Path.GetFileName(file);
                    string fileContent = File.ReadAllText(file);

                    var pageData = new PageData
                    {
                        FileName = fileName,
                        Content = fileContent,
                        Url = _contentProcessor.ExtractUrl(fileContent),
                        Title = _contentProcessor.ExtractTitle(fileContent),
                        Headings = _contentProcessor.ExtractHeadings(fileContent)
                    };

                    _pageIndex.Add(pageData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading website content: {ex.Message}");
            }
        }

        private string FindRelevantContent(string question, int contextSize)
        {
            // Extract keywords from the question
            var keywords = _contentProcessor.ExtractKeywords(question);

            // Rank pages by relevance
            var rankedPages = _pageIndex.Select(page =>
            {
                int score = keywords.Sum(keyword =>
                    Regex.Matches(page.Content, keyword, RegexOptions.IgnoreCase).Count);
                return (page, score);
            })
            .Where(item => item.score > 0)
            .OrderByDescending(item => item.score)
            .Take(5) // Take top 3 most relevant pages
            .ToList();

            var sb = new StringBuilder();

            if (rankedPages.Count == 0)
            {
                // If no specific pages match, include titles and headings from all pages
                sb.AppendLine("Website structure:");
                foreach (var page in _pageIndex)
                {
                    sb.AppendLine($"Page: {page.Title}");
                    sb.AppendLine($"URL: {page.Url}");
                    if (page.Headings.Count > 0)
                    {
                        sb.AppendLine("Headings:");
                        foreach (var heading in page.Headings.Take(5)) // Limit to top 5 headings
                        {
                            sb.AppendLine($"- {heading}");
                        }
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                // For relevant pages, include more detailed content
                foreach (var (page, score) in rankedPages)
                {
                    sb.AppendLine($"--- Page: {page.Title} ---");
                    sb.AppendLine($"URL: {page.Url}");

                    // Extract content section
                    var contentMatch = Regex.Match(page.Content, @"--- Content ---\s*([\s\S]*)");
                    if (contentMatch.Success)
                    {
                        string content = contentMatch.Groups[1].Value.Trim();

                        // Truncate if too long
                        if (content.Length > contextSize / rankedPages.Count)
                        {
                            content = content.Substring(0, contextSize / rankedPages.Count) + "...";
                        }

                        sb.AppendLine(content);
                    }
                    else
                    {
                        // If we can't find the content section, include the whole file (truncated)
                        string truncatedContent = page.Content;
                        if (truncatedContent.Length > contextSize / rankedPages.Count)
                        {
                            truncatedContent = truncatedContent.Substring(0, contextSize / rankedPages.Count) + "...";
                        }
                        sb.AppendLine(truncatedContent);
                    }

                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
