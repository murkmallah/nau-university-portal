using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IChatbotService
    {
        Task<ChatResponseModel> ProcessQueryAsync(string userMessage, List<ChatMessageDto>? history = null);
        ChatbotOptions GetCurrentOptions();
        void UpdateOptions(ChatbotOptions options);
    }
}
