using System;
using System.Threading.Tasks;
using WebCrawler2;

namespace WebsiteOllamaAssistant
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("==============================================");
            Console.WriteLine("Digital Workmate Website Assistant");
            Console.WriteLine("==============================================");

            var assistant = new WebsiteAssistant();

            // Initialize the assistant
            if (!await assistant.Initialize())
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("\nAssistant is ready! You can now ask questions about digitalworkmate.info");
            Console.WriteLine("Type 'exit' to quit, 'help' for more commands");

            // Main interaction loop
            while (true)
            {
                Console.Write("\n> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                // Check for special commands
                if (input.ToLower() == "exit" || input.ToLower() == "quit")
                    break;

                if (input.ToLower() == "help")
                {
                    assistant.ShowHelp();
                    continue;
                }

                if (input.ToLower() == "list")
                {
                    assistant.ListPages();
                    continue;
                }

                if (input.ToLower().StartsWith("search "))
                {
                    string searchTerm = input.Substring("search ".Length);
                    assistant.SearchContent(searchTerm);
                    continue;
                }

                // Default action: treat as a question for Ollama
                await assistant.AskQuestion(input);
            }
        }
    }
}