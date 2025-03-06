using OllamaSharp.Models;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebCrawler2
{
    public class LanguageModelInterface
    {
        private readonly string _modelName;

        public LanguageModelInterface(string modelName)
        {
            _modelName = modelName;
        }

        public async Task ProcessQuestionWithTwoStageApproach(string question, string relevantContent, OllamaApiClient ollama)
        {
            try
            {
                Console.WriteLine("\nThinking...");

                // Stage 1: Content understanding and information retrieval (language agnostic)
                string contentAnalysisPrompt = CreateContentAnalysisPrompt(question, relevantContent);

                var contentAnalysisRequest = new GenerateRequest
                {
                    Model = _modelName,
                    Prompt = contentAnalysisPrompt,
                    Options = new RequestOptions
                    {
                        Temperature = 0.1f,
                        NumPredict = 2048
                    }
                };

                // Get content analysis result
                string contentAnalysisResult = await GetNonStreamingResponse(contentAnalysisRequest, ollama);

                // Stage 2: Format response in the appropriate language
                string languageFormattingPrompt = CreateLanguageFormattingPrompt(question, contentAnalysisResult);

                var languageFormattingRequest = new GenerateRequest
                {
                    Model = _modelName,
                    Prompt = languageFormattingPrompt,
                    Options = new RequestOptions
                    {
                        Temperature = 0.1f,
                        NumPredict = 2048
                    },
                    Stream = true
                };

                Console.WriteLine("\nAnswer:");

                // Stream the final response for better UX
                await foreach (var response in ollama.GenerateAsync(languageFormattingRequest))
                {
                    Console.Write(response.Response);
                }

                Console.WriteLine("\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError generating response: {ex.Message}");
                Console.WriteLine("Make sure Ollama is running and the model is correctly installed.");
            }
        }

        private async Task<string> GetNonStreamingResponse(GenerateRequest request, OllamaApiClient ollama)
        {
            bool originalStreamSetting = request.Stream;

            try
            {
                // Ensure streaming is disabled
                request.Stream = false;

                // Because GenerateAsync returns an IAsyncEnumerable<GenerateResponseStream>,
                // we have to consume it with await foreach:
                var sb = new StringBuilder();
                await foreach (var chunk in ollama.GenerateAsync(request))
                {
                    if (chunk?.Response != null)
                    {
                        sb.Append(chunk.Response);
                    }
                }
                return sb.ToString();
            }
            finally
            {
                // Restore original stream setting
                request.Stream = originalStreamSetting;
            }
        }

        private string CreateContentAnalysisPrompt(string question, string relevantContent)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"You are a digital workmate website content analyzer. Your task is to:
1. Find relevant information about the user's question
2. Extract and organize key facts from the website content
3. Create a comprehensive analysis that can later be formatted in the appropriate language

IMPORTANT: This is a content analysis stage. Focus on gathering accurate information without worrying about the final response format or language.");
            sb.AppendLine();

            sb.AppendLine("WEBSITE CONTENT:");
            sb.AppendLine(relevantContent);
            sb.AppendLine();

            sb.AppendLine("USER QUESTION:");
            sb.AppendLine(question);
            sb.AppendLine();

            sb.AppendLine("INSTRUCTIONS:");
            sb.AppendLine("1. Analyze the question to understand what information is being requested");
            sb.AppendLine("2. Extract all relevant facts from the website content");
            sb.AppendLine("3. Organize information in a structured way");
            sb.AppendLine("4. Indicate if the information is not found in the content");
            sb.AppendLine("5. Include page references for where information was found");
            sb.AppendLine("6. Focus on accuracy and completeness, not language or formatting");
            sb.AppendLine();

            sb.AppendLine("OUTPUT FORMAT: Provide a detailed, structured analysis with all key facts and sources.");

            return sb.ToString();
        }

        private string CreateLanguageFormattingPrompt(string question, string contentAnalysis)
        {
            var sb = new StringBuilder();

            sb.AppendLine(@"You are a multilingual digital workmate assistant. Your task is to:
1. Identify the language of the user's question
2. Provide a BRIEF, DIRECT answer to their question based on the content analysis
3. Use the EXACT SAME LANGUAGE as the user's question

CRITICAL INSTRUCTIONS:
- Be concise and direct - keep responses to 4-5 sentences maximum
- Don't use headers, bullet points, or complex formatting unless absolutely necessary
- Don't mention 'content analysis', 'page references', or your internal processes
- Don't label the language you've detected
- Maintain a conversational, helpful tone
- ONLY RESPOND IN THE LANGUAGE THAT THE USER HAS ASKED THE QUESTION IN
- If information isn't available, simply state that briefly");

            sb.AppendLine();
            sb.AppendLine("USER QUESTION:");
            sb.AppendLine(question);
            sb.AppendLine();
            sb.AppendLine("CONTENT ANALYSIS:");
            sb.AppendLine(contentAnalysis);
            sb.AppendLine();


            sb.AppendLine("INSTRUCTIONS FOR RESPONSE FORMAT:");
            sb.AppendLine("1. Start with a direct answer to the question");
            sb.AppendLine("2. Provide only 3-5 key details that are most relevant");
            sb.AppendLine("3. Keep the entire response under 100 words");
            sb.AppendLine("4. Don't mention sources, page references, or missing information unless directly asked");
            sb.AppendLine("5. Use a conversational tone as if speaking directly to the user");

            sb.AppendLine();
            sb.AppendLine($"RESPONSE LANGUAGE: Respond in the EXACT same language as: \"{question}\"");

            return sb.ToString();
        }
    }
}
