using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NAU_Web.Models;

namespace NAU_Web.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IChatbotKnowledgeService _knowledgeService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatbotService> _logger;
        private ChatbotOptions _options;

        public ChatbotService(
            IChatbotKnowledgeService knowledgeService,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ChatbotService> logger)
        {
            _knowledgeService = knowledgeService;
            _httpClient = httpClient;
            _logger = logger;

            _options = new ChatbotOptions();
            configuration.GetSection("Chatbot").Bind(_options);
        }

        public ChatbotOptions GetCurrentOptions() => _options;

        public void UpdateOptions(ChatbotOptions options)
        {
            _options = options;
        }

        public async Task<ChatResponseModel> ProcessQueryAsync(string userMessage, List<ChatMessageDto>? history = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return new ChatResponseModel
                {
                    Success = true,
                    Reply = "Please ask a question about National Aviation University (NAU), degree programs, ATP 2026 courses, admissions, fees, scholarships, or campus facilities."
                };
            }

            // Direct greeting check - returns Walaikum Assalam for salam/aoa and Hello for hi/hello
            var greetingReply = GetGreetingReply(userMessage);
            if (!string.IsNullOrEmpty(greetingReply))
            {
                return new ChatResponseModel
                {
                    Success = true,
                    Reply = greetingReply,
                    Sources = new List<KnowledgeSnippet>(),
                    SuggestedQuestions = new List<string> { "Admissions 2026", "Fee Structure", "Degree Programs", "Hostel & Facilities" }
                };
            }

            try
            {
                // 1. Retrieve dynamic real-time knowledge from Database, ATP Catalog & CMS
                var knowledge = await _knowledgeService.GetRelevantKnowledgeAsync(userMessage, 8);
                var masterFactbook = await _knowledgeService.GetMasterKnowledgeSummaryAsync();

                // 2. If a valid AI API Key is configured and Provider is Gemini or OpenAI, try AI generation
                if (!string.IsNullOrWhiteSpace(_options.ApiKey) &&
                    !string.Equals(_options.Provider, "BuiltIn", StringComparison.OrdinalIgnoreCase) &&
                    IsKeyFormatValid(_options.Provider, _options.ApiKey))
                {
                    var aiReply = await TryCallGenerativeAiAsync(userMessage, knowledge, masterFactbook, history);
                    if (!string.IsNullOrWhiteSpace(aiReply))
                    {
                        return new ChatResponseModel
                        {
                            Success = true,
                            Reply = StripEmojis(aiReply),
                            Sources = new List<KnowledgeSnippet>(),
                            SuggestedQuestions = GenerateSuggestions(userMessage, knowledge)
                        };
                    }
                }

                // 3. Fallback / Built-in Smart Semantic Response Engine
                var builtInReply = GenerateSmartSemanticResponse(userMessage, knowledge);

                return new ChatResponseModel
                {
                    Success = true,
                    Reply = StripEmojis(builtInReply),
                    Sources = new List<KnowledgeSnippet>(),
                    SuggestedQuestions = GenerateSuggestions(userMessage, knowledge)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chatbot query: {Query}", userMessage);
                return new ChatResponseModel
                {
                    Success = false,
                    Reply = "I apologize, but I encountered a temporary processing issue. You can visit our [Admissions Portal](/Account/Register) or contact the NAU Academics Office directly at **+92 22 9260310-19** or email **AdID.Academics@paa.gov.pk**.",
                    ErrorMessage = ex.Message
                };
            }
        }

        private static bool IsKeyFormatValid(string? provider, string? apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return false;
            var key = apiKey.Trim();
            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
            {
                return key.StartsWith("AIzaSy", StringComparison.Ordinal);
            }
            if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return key.StartsWith("sk-", StringComparison.Ordinal);
            }
            return false;
        }

        private async Task<string?> TryCallGenerativeAiAsync(
            string userMessage,
            List<KnowledgeSnippet> knowledge,
            string masterFactbook,
            List<ChatMessageDto>? history)
        {
            try
            {
                if (string.Equals(_options.Provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                {
                    return await CallGeminiApiAsync(userMessage, knowledge, masterFactbook, history);
                }
                else if (string.Equals(_options.Provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
                {
                    return await CallOpenAiApiAsync(userMessage, knowledge, masterFactbook, history);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Generative AI API call failed, automatically falling back to built-in knowledge response engine.");
            }
            return null;
        }

        private async Task<string?> CallGeminiApiAsync(
            string userMessage,
            List<KnowledgeSnippet> knowledge,
            string masterFactbook,
            List<ChatMessageDto>? history)
        {
            // Build comprehensive system instructions with grounding factbook + live DB context
            var liveContext = new StringBuilder();
            liveContext.AppendLine("--- LIVE RETRIEVED FACTS FROM DATABASE & CMS ---");
            foreach (var k in knowledge)
            {
                liveContext.AppendLine($"[{k.Category}] {k.Title}: {k.Content} (Page Link: {k.Url})");
            }

            var systemPrompt = $"{_options.SystemPrompt}\n\n" +
                               $"{masterFactbook}\n\n" +
                               $"{liveContext}\n\n" +
                               "CRITICAL RULES FOR STRAIGHTFORWARD & TO-THE-POINT REPLIES:\n" +
                               "1. DIRECT ANSWER FIRST: Jump straight to the facts/numbers without any introductory fluff or boilerplate phrases (NEVER say 'National Aviation University offers...', 'Here are the programs...', 'Certainly!', or 'Welcome to NAU...').\n" +
                               "2. COUNT / NUMBER QUESTIONS: If the user asks 'how many', 'total', 'count', or 'kitne', state the exact count immediately upfront (e.g., 'NAU offers **6 degree programs** and over **300+ certified aviation training short courses (ATP 2026)** across 4 schools:').\n" +
                               "3. HIGHLY CONCISE: Limit the response strictly to 2 to 4 crisp bullet points or short lines. Avoid long descriptive paragraphs.\n" +
                               "4. BOLD KEY DATA: Bold key numbers, fees, percentages, eligibility requirements, and program names.\n" +
                               "5. LINKS: Include at most ONE relevant markdown link at the end (e.g. [Course Catalog](/CourseCatalog) or [Fee Structure](/Financial) or [Apply Online](/Account/Register) or [Contact Us](/Contact)). Never attach links to greetings.\n" +
                               "6. LANGUAGE MATCH: If user asks in Roman Urdu or Urdu (e.g., 'fees kitni hai', 'hostel hai kya', 'kahan par hai'), reply directly in short Roman Urdu / Urdu. If English, reply in concise English.\n" +
                               "7. GREETINGS: If user only greets (hi, hello, salam), reply ONLY: 'Hello! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?'\n" +
                               "8. NO EMOJIS: Never output any emoji under any circumstances.\n" +
                               "9. COMPLETE RESPONSES: Always complete every sentence and list fully. Never leave any bullet point cut off or unfinished.";

            // Format multi-turn conversation contents
            var contentsList = new List<object>();

            if (history != null && history.Any())
            {
                foreach (var h in history.TakeLast(6))
                {
                    var role = string.Equals(h.Role, "assistant", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(h.Role, "model", StringComparison.OrdinalIgnoreCase)
                        ? "model"
                        : "user";

                    contentsList.Add(new
                    {
                        role = role,
                        parts = new[] { new { text = h.Content } }
                    });
                }
            }

            // Append current user message
            contentsList.Add(new
            {
                role = "user",
                parts = new[] { new { text = userMessage } }
            });

            var requestPayload = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = contentsList,
                generationConfig = new
                {
                    temperature = 0.2,
                    maxOutputTokens = 1200,
                    topP = 0.95
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestPayload);

            // Valid Gemini model candidates
            var candidateModels = new List<string>();
            var primaryModel = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-1.5-flash" : _options.Model.Trim();
            if (string.Equals(primaryModel, "gemini-3.5-flash", StringComparison.OrdinalIgnoreCase))
            {
                primaryModel = "gemini-1.5-flash";
            }
            candidateModels.Add(primaryModel);

            var standardFallbacks = new[] { "gemini-1.5-flash", "gemini-2.0-flash", "gemini-1.5-pro" };
            foreach (var m in standardFallbacks)
            {
                if (!candidateModels.Contains(m, StringComparer.OrdinalIgnoreCase))
                {
                    candidateModels.Add(m);
                }
            }

            foreach (var model in candidateModels)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2.5));
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={_options.ApiKey}";
                    using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(url, content, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync(cts.Token);
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0 &&
                            candidates[0].TryGetProperty("content", out var candContent) &&
                            candContent.TryGetProperty("parts", out var parts) &&
                            parts.GetArrayLength() > 0 &&
                            parts[0].TryGetProperty("text", out var textEl))
                        {
                            var reply = textEl.GetString();
                            if (!string.IsNullOrWhiteSpace(reply))
                            {
                                return reply;
                            }
                        }
                    }
                    else
                    {
                        var err = await response.Content.ReadAsStringAsync(cts.Token);
                        _logger.LogWarning("Gemini API call to {Model} returned {StatusCode}: {Error}.", model, response.StatusCode, err);
                        // If auth/key error or 400/404, break immediately
                        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                            response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
                            response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                            response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception calling Gemini model {Model}.", model);
                }
            }

            return null;
        }

        private async Task<string?> CallOpenAiApiAsync(
            string userMessage,
            List<KnowledgeSnippet> knowledge,
            string masterFactbook,
            List<ChatMessageDto>? history)
        {
            var model = string.IsNullOrWhiteSpace(_options.Model) ? "gpt-4o-mini" : _options.Model;
            var url = "https://api.openai.com/v1/chat/completions";

            var knowledgeText = new StringBuilder();
            knowledgeText.AppendLine(masterFactbook);
            knowledgeText.AppendLine("LIVE DATABASE FACTS FROM NAU CMS:");
            foreach (var k in knowledge)
            {
                knowledgeText.AppendLine($"[{k.Category}] {k.Title}: {k.Content} (Link: {k.Url})");
            }

            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = $"{_options.SystemPrompt}\n\n{knowledgeText}\nAlways answer questions directly, straightforwardly, and concisely with facts and zero introductory filler. Limit to 2-4 lines/bullets. Do not use any emojis."
                }
            };

            if (history != null && history.Any())
            {
                foreach (var h in history.TakeLast(6))
                {
                    messages.Add(new { role = h.Role == "assistant" ? "assistant" : "user", content = h.Content });
                }
            }

            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = model,
                messages = messages,
                temperature = 0.2,
                max_tokens = 1200
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var msg) &&
                msg.TryGetProperty("content", out var contentEl))
            {
                return contentEl.GetString();
            }

            return null;
        }

        private string GenerateSmartSemanticResponse(string query, List<KnowledgeSnippet> knowledge)
        {
            var lower = query.Trim().ToLowerInvariant();

            // 1. Greetings
            var greetingReply = GetGreetingReply(lower);
            if (!string.IsNullOrEmpty(greetingReply))
            {
                return greetingReply;
            }

            // 2. Quantity / Count queries (e.g. "how many courses", "kitne programs", "total courses")
            if ((lower.Contains("how many") || lower.Contains("kitne") || lower.Contains("kitna") || lower.Contains("total") || lower.Contains("count") || lower.Contains("number of"))
                && (lower.Contains("course") || lower.Contains("program") || lower.Contains("degree")))
            {
                return "NAU offers **6 major degree programs** and over **300+ certified aviation training short courses (ATP 2026)** across 4 schools:\n\n" +
                       "- **Degree Programs (6):** BE ANS & Airport Engineering, BS Aviation Management, BS Computer Science & AI, BS Cyber Security & Data Science, BS Aircraft Maintenance Technology, MBA Aviation Management.\n" +
                       "- **Short Courses (300+):** Air Navigation Services, Regulatory Services, Airport Services, Aviation Management.\n\n" +
                       "View details in the [Course Catalog](/CourseCatalog) or [Apply Online](/Account/Register).";
            }

            // 3. Faculties query (e.g. "how many faculties", "faculties list", "which faculties")
            if (lower.Contains("faculty") || lower.Contains("faculties"))
            {
                return "NAU has **4 Academic Faculties** offering undergraduate and graduate degree programs:\n\n" +
                       "- **Faculty of Engineering:** BE Air Navigation Services (ANS) & Airport Engineering\n" +
                       "- **Faculty of IT & Cyber Security:** BS Computer Science & AI, BS Cyber Security & Data Science\n" +
                       "- **Faculty of Aviation Operations:** BS Aviation Management, BS Aircraft Maintenance Technology\n" +
                       "- **Faculty of Management Sciences:** Aviation Management & MBA Aviation Management\n\n" +
                       "Explore all departments on the [Faculties Page](/Faculties/Public).";
            }

            // 4. Schools query (e.g. "how many schools", "schools list", "which schools")
            if (lower.Contains("school") || lower.Contains("schools"))
            {
                return "NAU has **4 specialized aviation training schools** for ATP 2026 certified short courses:\n\n" +
                       "- **School of Air Navigation Services (ANS):** ATC, CNS, and Radar Training\n" +
                       "- **School of Regulatory Services:** Flight Safety, Standards, and Airworthiness Inspection\n" +
                       "- **School of Airport Services:** Aviation Security (AVSEC) and Aerodrome Firefighting (AFF)\n" +
                       "- **School of Aviation Management:** Safety Management Systems (SMS) and Leadership\n\n" +
                       "Explore training programs on the [Course Catalog](/CourseCatalog).";
            }

            // 5. Homepage / Structure Overview
            if (lower.Contains("homepage") || lower.Contains("structure") || lower.Contains("department"))
            {
                return "National Aviation University (NAU) is established at CATI Hyderabad under Pakistan Civil Aviation Authority (PCAA).\n\n" +
                       "- **4 Academic Faculties:** Engineering, IT & Cyber Security, Aviation Operations, Management Sciences (6 Degree Programs).\n" +
                       "- **4 Training Schools:** Air Navigation Services, Regulatory Services, Airport Services, Aviation Management (300+ ATP Courses).\n" +
                       "- **Admissions 2026:** [Apply Online Portal](/Account/Register) | [Course Catalog](/CourseCatalog)";
            }

            // 6. Admissions & Degree Programs
            if (lower.Contains("admission") || lower.Contains("apply") || lower.Contains("dakhla") || lower.Contains("eligibility") || lower.Contains("program") || lower.Contains("degree"))
            {
                return "**NAU Degree Programs:**\n" +
                       "- **BE Air Navigation Services (ANS) & Airport Engineering** (4 Years)\n" +
                       "- **BS Aviation Management** (4 Years)\n" +
                       "- **BS Computer Science & AI** (4 Years)\n" +
                       "- **BS Cyber Security & Data Science** (4 Years)\n" +
                       "- **BS Aircraft Maintenance Technology** (4 Years)\n" +
                       "- **MBA Aviation Management** (2 Years)\n\n" +
                       "**Eligibility:** Min 50%-60% in FSc / ICS / A-Levels (or Graduation for MBA).\n" +
                       "Apply online at [Admissions Portal](/Account/Register).";
            }

            // 5. Fee Structure / Scholarships
            if (lower.Contains("fee") || lower.Contains("fees") || lower.Contains("tuition") || lower.Contains("scholarship") || lower.Contains("cost") || lower.Contains("paisa") || lower.Contains("charges") || lower.Contains("kharcha"))
            {
                return "**Per-Semester Tuition Fees (2026):**\n" +
                       "- **BE Programs:** PKR 54,500/semester (Tuition PKR 45,000 + Charges PKR 9,500)\n" +
                       "- **BS Programs:** PKR 45,500/semester (Tuition PKR 38,000 + Charges PKR 7,500)\n" +
                       "- **MBA Program:** PKR 62,000/semester (Tuition PKR 55,000 + Charges PKR 7,000)\n" +
                       "- **Registration Fee:** PKR 5,000 (one-time) | **Hostel:** PKR 8,000/month\n\n" +
                       "**Scholarships:** Merit (50%-100%), Need-Based (25%-75%), Hafiz-e-Quran (100%), CAA Employee Concession (30%-50%).\n" +
                       "Full breakdown on [Fee Structure & Financial](/Financial).";
            }

            // 6. Short Courses / ATP 2026 Catalog
            if (lower.Contains("short course") || lower.Contains("course") || lower.Contains("training") || lower.Contains("atp") || lower.Contains("catalog"))
            {
                return "NAU offers **300+ certified CPD and technical aviation short courses** across 4 schools:\n" +
                       "- **Air Navigation Services:** Basic ATC (40 wks), ATC Refresher, Radar & CNS Training\n" +
                       "- **Regulatory Services:** Flight Safety, Aerodrome Standards, Airworthiness Inspection\n" +
                       "- **Airport Services:** Aviation Security (AVSEC), Aerodrome Firefighting (AFF)\n" +
                       "- **Aviation Management:** Safety Management Systems (SMS), Aviation Leadership\n\n" +
                       "Browse all courses and schedules on the [Course Catalog](/CourseCatalog).";
            }

            // 7. Facilities / Hostel / Mess / Sports / Library
            if (lower.Contains("hostel") || lower.Contains("facility") || lower.Contains("facilities") || lower.Contains("library") || lower.Contains("sports") || lower.Contains("mess") || lower.Contains("medical") || lower.Contains("masjid") || lower.Contains("mosque") || lower.Contains("rehaish"))
            {
                return "**NAU Campus Facilities:**\n" +
                       "- **Hostels:** Secure on-campus residences for boys & girls (PKR 8,000/month, 24/7 power, Wi-Fi)\n" +
                       "- **Central Library:** 15,000+ aviation and engineering books & e-library\n" +
                       "- **Sports Complex:** Cricket stadium, tennis courts, modern gym/fitness center\n" +
                       "- **Medical Center:** 24/7 emergency clinic with doctor & ambulance\n" +
                       "- **Central Mosque:** Air-conditioned prayer hall on campus\n\n" +
                       "Explore details on [Campus Facilities](/Facilities/Public).";
            }

            // 8. Simulation Labs / Simulators
            if (lower.Contains("simulator") || lower.Contains("simulation") || lower.Contains("sim") || lower.Contains("boeing") || lower.Contains("airbus") || lower.Contains("radar") || lower.Contains("atc") || lower.Contains("avionics") || lower.Contains("tayara"))
            {
                return "**NAU Aviation Simulators & Labs:**\n" +
                       "- **Boeing 737-800 Full Flight Simulator:** Complete flight deck replica\n" +
                       "- **Airbus A320 Flight Training Device (FTD):** Glass-cockpit operational training\n" +
                       "- **ATC Radar & 360 Aerodrome Control Tower Simulator**\n" +
                       "- **Avionics & Radar Systems Lab**\n\n" +
                       "View details on [Simulation & Labs Page](/SimulationLabs/Public).";
            }

            // 8b. Upcoming Events, Seminars, Workshops & Gala Schedule
            if (lower.Contains("event") || lower.Contains("events") || lower.Contains("seminar") || lower.Contains("workshop") || lower.Contains("gala") || lower.Contains("upcoming") || lower.Contains("held") || lower.Contains("kab hoga") || lower.Contains("kahan hoga") || lower.Contains("kahan honge") || lower.Contains("kab honge"))
            {
                var eventSnippet = knowledge.FirstOrDefault(k => (k.Category ?? "").Contains("Events") || (k.Title ?? "").Contains("Event"));
                if (eventSnippet != null && !string.IsNullOrWhiteSpace(eventSnippet.Content))
                {
                    return eventSnippet.Content + "\n\nFull details on [News & Media Portal](/NewsPosts/Public).";
                }

                return "**Upcoming Official NAU Events & Seminars 2026:**\n\n" +
                       "• **Aviation Safety & Flight Operations Seminar 2026**\n" +
                       "  - **Date & Time:** 15 September 2026 (10:00 AM – 01:00 PM)\n" +
                       "  - **Location:** NAU Auditorium, Main Campus\n\n" +
                       "• **Advanced Aircraft Maintenance & Avionics Workshop**\n" +
                       "  - **Date & Time:** 28 September 2026 (09:00 AM – 03:00 PM)\n" +
                       "  - **Location:** Hangar Training Facility\n\n" +
                       "• **Aviation Career Day & Industry Interaction 2026**\n" +
                       "  - **Date & Time:** 12 October 2026 (11:00 AM – 02:00 PM)\n" +
                       "  - **Location:** NAU Campus Grounds\n\n" +
                       "• **Annual Aviation Excellence Gala & Convocation Prep**\n" +
                       "  - **Date & Time:** 14 November 2026 (08:00 AM – 09:30 AM)\n" +
                       "  - **Location:** Main Parade Ground, NAU\n\n" +
                       "View complete announcements on [News & Media](/NewsPosts/Public).";
            }

            // 9. Contact / Helpline / Address
            if (lower.Contains("contact") || lower.Contains("phone") || lower.Contains("email") || lower.Contains("number") || lower.Contains("rabta") || lower.Contains("address") || lower.Contains("location") || lower.Contains("timing") || lower.Contains("helpline"))
            {
                return "**Contact National Aviation University (NAU):**\n" +
                       "- **Address:** Airport Road, Civil Aviation Training Institute (CATI), Hyderabad, Sindh\n" +
                       "- **Admissions Phone:** `+92 22 9260310-19` | **Email:** `AdID.Academics@paa.gov.pk`\n" +
                       "- **Office Hours:** Monday to Friday, 08:00 AM – 04:00 PM\n" +
                       "- **Apply Online:** [Admissions Portal](/Account/Register)";
            }

            // 10. General Knowledge Match
            if (knowledge.Any(k => k.Score > 0.05))
            {
                var top = knowledge.Where(k => k.Score > 0.05).First();
                return $"**{top.Title}:**\n{top.Content}\n\n[More Details]({top.Url ?? "/"})";
            }

            // 11. Default Fallback
            return "NAU offers **6 Degree Programs** and **300+ Aviation Short Courses (ATP 2026)**.\n\n" +
                   "You can ask about:\n" +
                   "- **Admissions & Degrees** ([Course Catalog](/CourseCatalog) | [Apply Online](/Account/Register))\n" +
                   "- **Tuition Fees & Scholarships** ([Fee Structure](/Financial))\n" +
                   "- **Campus Facilities & Hostels** ([Facilities](/Facilities/Public))\n" +
                   "- **Simulators & Labs** ([Simulation Labs](/SimulationLabs/Public))\n" +
                   "- **Contact & Helpdesk** ([Contact Us](/Contact))";
        }

        public static string StripEmojis(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            return Regex.Replace(text, @"\p{Cs}|[\u2600-\u27BF]|[\u2300-\u23FF]|[\u2B50\u2B55\u25AA\u25AB\u25B6\u25C0\u25FB-\u25FE]|[\uD83C-\uDBFF\uDC00-\uDFFF]", "").Trim();
        }

        private static string? GetGreetingReply(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var clean = Regex.Replace(text.Trim().ToLowerInvariant(), @"[^\w\s]", "").Trim();

            var islamicGreetings = new[] {
                "assalam", "salam", "aoa", "assalamu alaikum", "assalam o alaikum",
                "assalamoalaikum", "assalamalaikum", "slam", "slaam", "adaab",
                "assalam u alaikum", "assalam valaikum", "assalam walekum", "walekum assalam",
                "assalamo alaikum", "assalamo-alaikum", "assalam-o-alaikum"
            };

            var generalGreetings = new[] {
                "hi", "hello", "hey", "good morning", "good evening", "good afternoon",
                "kese ho", "kaise ho", "help"
            };

            foreach (var g in islamicGreetings)
            {
                if (clean == g || clean == g + " nau" || clean == g + " bot" || clean == g + " assistant" || clean == "walam" || clean == "wsalam" || clean == "w alaikum assalam")
                {
                    return "Walaikum Assalam! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?";
                }
            }

            foreach (var g in generalGreetings)
            {
                if (clean == g || clean == g + " nau" || clean == g + " bot" || clean == g + " assistant")
                {
                    return "Hello! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?";
                }
            }

            return null;
        }

        private static List<string> GenerateSuggestions(string query, List<KnowledgeSnippet> knowledge)
        {
            var lower = query.ToLowerInvariant();
            if (lower.Contains("admission") || lower.Contains("program") || lower.Contains("degree") || lower.Contains("dakhla"))
            {
                return new List<string> { "What is the fee structure?", "What are the hostel facilities?", "How do I apply online?" };
            }
            if (lower.Contains("fee") || lower.Contains("cost") || lower.Contains("scholarship"))
            {
                return new List<string> { "What scholarships are available?", "What are the payment methods?", "How can I apply for admissions?" };
            }
            if (lower.Contains("course") || lower.Contains("training") || lower.Contains("atp"))
            {
                return new List<string> { "What are the course durations?", "What flight simulators do you have?", "Contact the Academics Desk" };
            }
            if (lower.Contains("facility") || lower.Contains("hostel") || lower.Contains("mess"))
            {
                return new List<string> { "Tell me about the hostel facilities", "What are the library timings?", "Where is the NAU campus located?" };
            }
            if (lower.Contains("simulator") || lower.Contains("lab"))
            {
                return new List<string> { "What degree programs are offered?", "What are the admission requirements?", "What are the tuition fees?" };
            }

            return new List<string> { "Admissions 2026", "Degree Programs", "ATP 2026 Short Courses", "Fee Structure", "Hostel & Facilities", "Flight Simulators" };
        }
    }
}
