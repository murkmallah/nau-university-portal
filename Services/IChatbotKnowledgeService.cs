using NAU_Web.Models;

namespace NAU_Web.Services
{
    public interface IChatbotKnowledgeService
    {
        /// <summary>
        /// Retrieves the most relevant knowledge snippets from all database tables, ATP 2026 catalog, and CMS pages
        /// for a given user query in real time.
        /// </summary>
        Task<List<KnowledgeSnippet>> GetRelevantKnowledgeAsync(string query, int maxResults = 8);

        /// <summary>
        /// Retrieves an overview summary of indexed database tables and pages for diagnostics/admin testing.
        /// </summary>
        Task<Dictionary<string, int>> GetKnowledgeStatsAsync();

        /// <summary>
        /// Retrieves a comprehensive, structured Master University Factbook used for Gemini AI system context.
        /// </summary>
        Task<string> GetMasterKnowledgeSummaryAsync();
    }
}
