using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AiController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    // ========== ANALYZE QUERY ==========
    [HttpPost("analyze-query")]
    public async Task<IActionResult> AnalyzeQuery([FromBody] AnalyzeQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { success = false, message = "Query is required" });

        var apiKey = _configuration["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new { success = false, message = "Groq API key not configured" });

        var categories = new[]
        {
            "Engineering Faculty",
            "Faculty of Science & Letters",
            "Medical Faculty",
            "Pharmacy Faculty",
            "Dentistry Faculty",
            "Health Sciences Faculty",
            "Law Faculty",
            "Fine Arts / Music / Conservatory",
            "Psychology",
            "Education Faculty",
            "Economics / Business / Management Faculty",
            "Communication Faculty",
            "Architecture & Design Faculty",
            "English & Language Exams"
        };

        var booksContext = "";
        if (request.Books != null && request.Books.Count > 0)
        {
            var bookLines = request.Books.Select(b =>
                $"[{b.BookId}] Title: \"{b.Title}\" | Author: \"{b.Author}\" | Category: \"{b.Category}\" | Description: \"{b.Description}\""
            );
            booksContext = $"\n\nAvailable books in the library:\n{string.Join("\n", bookLines)}";
        }

        var prompt = $@"You are an intelligent university library recommendation engine.

Available categories: {string.Join(", ", categories)}

User query: ""{request.Query}""
{booksContext}

Your job:
1. Understand the TOPIC and FIELD behind the query (can be in any language - Turkish, English, etc.)
2. Look at each book's title, author, category AND description to understand what it is about
3. Identify which books are relevant to the query topic — even if the book title doesn't contain the exact query word
   Example: query ""matematik"" → ""Calculus"" book is relevant because calculus IS mathematics
   Example: query ""software"" → ""Clean Code"" is relevant because it's about software development
   Example: query ""yazılım"" → ""Design Patterns"" is relevant because it's a software engineering book

Return ONLY a JSON object with NO extra text, NO markdown, NO backticks:
{{
  ""primaryCategory"": ""EXACT_CATEGORY_NAME"",
  ""relatedCategories"": [""CAT1"", ""CAT2""],
  ""searchKeywords"": [""word1"", ""word2"", ""word3"", ""word4"", ""word5"", ""word6"", ""word7"", ""word8""],
  ""recommendedBookIds"": [1, 2, 3]
}}

Rules:
- primaryCategory: must be exactly one of the available categories
- relatedCategories: 1-3 other relevant categories
- searchKeywords: 8-10 English keywords that describe this field broadly
- recommendedBookIds: list of book IDs from the library that are relevant to the query (can be empty [])";

        try
        {
            var client = _httpClientFactory.CreateClient();
            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.1,
                max_tokens = 800
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Groq API error: {StatusCode} {Body}", response.StatusCode, responseBody);
                return StatusCode(502, new { success = false, message = "Groq API error", detail = responseBody });
            }

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var cleanText = text.Replace("```json", "").Replace("```", "").Trim();
            using var resultDoc = JsonDocument.Parse(cleanText);

            return Ok(new { success = true, data = resultDoc.RootElement.Clone() });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Groq response");
            return Ok(new { success = false, message = "Could not parse AI response" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in AnalyzeQuery");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    // ========== SEARCH QUERY ==========
    [HttpPost("search-query")]
    public async Task<IActionResult> SearchQuery([FromBody] AnalyzeQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.Query))
            return BadRequest(new { success = false, message = "Query is required" });

        var apiKey = _configuration["Groq:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            return StatusCode(500, new { success = false, message = "Groq API key not configured" });

        var booksContext = "";
        if (request.Books != null && request.Books.Count > 0)
        {
            var bookLines = request.Books.Select(b =>
                $"[{b.BookId}] Title: \"{b.Title}\" | Author: \"{b.Author}\" | Category: \"{b.Category}\" | Description: \"{b.Description}\""
            );
            booksContext = string.Join("\n", bookLines);
        }

        var prompt = $@"You are a university library search and recommendation engine.

User query: ""{request.Query}""

Library books:
{booksContext}

Your job:
1. MATCHED books: Books that directly relate to the query topic
2. RECOMMENDED books: Books from the SAME or CLOSELY RELATED academic field only

STRICT RULES for recommendations:
- Law query → ONLY recommend: law, political science, history, economics, social science books
- Math query → ONLY recommend: mathematics, physics, engineering, computer science books  
- Medicine query → ONLY recommend: medicine, pharmacy, dentistry, health science books
- Psychology query → ONLY recommend: psychology, education, social science books
- Engineering/Software query → ONLY recommend: engineering, computer science, mathematics books
- NEVER recommend books from completely unrelated fields
- Pharmacy and Law have NOTHING in common — never recommend one for the other
- Always read the book's category AND description to judge relevance

Return ONLY a JSON object with NO extra text, NO markdown, NO backticks:
{{
  ""matchedBookIds"": [1, 2, 3],
  ""recommendedBookIds"": [4, 5, 6],
  ""primaryCategory"": ""EXACT_CATEGORY_NAME"",
  ""relatedCategories"": [""CAT1"", ""CAT2""],
  ""searchKeywords"": [""word1"", ""word2"", ""word3"", ""word4"", ""word5""]
}}

Available categories: Engineering Faculty, Faculty of Science & Letters, Medical Faculty, Pharmacy Faculty, Dentistry Faculty, Health Sciences Faculty, Law Faculty, Fine Arts / Music / Conservatory, Psychology, Education Faculty, Economics / Business / Management Faculty, Communication Faculty, Architecture & Design Faculty, English & Language Exams

Query can be in any language (Turkish, English, etc.)";
        try
        {
            var client = _httpClientFactory.CreateClient();
            var body = new
            {
                model = "llama-3.1-8b-instant",
                messages = new[] { new { role = "user", content = prompt } },
                temperature = 0.1,
                max_tokens = 800
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var response = await client.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode(502, new { success = false, message = "Groq API error", detail = responseBody });

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";

            var cleanText = text.Replace("```json", "").Replace("```", "").Trim();
            using var resultDoc = JsonDocument.Parse(cleanText);

            return Ok(new { success = true, data = resultDoc.RootElement.Clone() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchQuery error");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}

public class AnalyzeQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public List<BookContext> Books { get; set; } = new();
}

public class BookContext
{
    public int BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}