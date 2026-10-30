namespace NAU_Web.Models
{
    public class ChatbotOptions
    {
        public string Provider { get; set; } = "Gemini"; // "Gemini", "OpenAI", or "BuiltIn"
        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = "gemini-3.5-flash"; // e.g. "gemini-3.5-flash", "gemini-flash-latest", "gemini-3.1-flash-lite", "gpt-4o-mini"
        public string BotName { get; set; } = "NAU AI Assistant";
        public string WelcomeMessage { get; set; } = "Hello! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?";
        public string SystemPrompt { get; set; } = "You are NAU AI Assistant, the official virtual assistant of National Aviation University (NAU) Pakistan. Always provide direct, straightforward, and to-the-point answers with ZERO filler, zero fluff, and no unnecessary introductory sentences. If asked for a count or quantity (e.g. 'how many courses'), state the exact numbers immediately. Keep responses short (under 3-5 lines), bold key figures/facts, and include at most one relevant markdown link. Never use emojis.";
        public bool EnableSound { get; set; } = true;
    }

    public class ChatMessageDto
    {
        public string Role { get; set; } = "user"; // "user" or "assistant"
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequestModel
    {
        public string Message { get; set; } = string.Empty;
        public List<ChatMessageDto>? History { get; set; }
    }

    public class KnowledgeSnippet
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Icon { get; set; }
        public double Score { get; set; }
    }

    public class ChatResponseModel
    {
        public bool Success { get; set; } = true;
        public string Reply { get; set; } = string.Empty;
        public List<KnowledgeSnippet>? Sources { get; set; }
        public List<string>? SuggestedQuestions { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
