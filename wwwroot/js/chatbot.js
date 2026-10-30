/**
 * NAU CMS - Intelligent AI Assistant Client
 * Connects directly to real-time Database & CMS Knowledge
 */
(function () {
    'use strict';

    var chatHistory = [];
    var isOpen = false;
    var isWaiting = false;

    var launcherBtn = document.getElementById('nauChatLauncher');
    var chatWindow = document.getElementById('nauChatWindow');
    var closeBtn = document.getElementById('nauChatCloseBtn');
    var clearBtn = document.getElementById('nauChatClearBtn');
    var messagesContainer = document.getElementById('nauChatMessages');
    var inputField = document.getElementById('nauChatInput');
    var sendBtn = document.getElementById('nauChatSendBtn');
    var typingIndicator = document.getElementById('nauChatTyping');
    var suggestionsContainer = document.getElementById('nauChatSuggestions');
    var suggestPrevBtn = document.getElementById('nauChatSuggestPrev');
    var suggestNextBtn = document.getElementById('nauChatSuggestNext');

    if (!launcherBtn || !chatWindow) {
        return;
    }

    // Toggle Chat Window
    function toggleChat(forceState) {
        isOpen = (typeof forceState === 'boolean') ? forceState : !isOpen;
        if (isOpen) {
            chatWindow.classList.add('open');
            launcherBtn.classList.add('active');
            if (inputField) {
                setTimeout(function () { inputField.focus(); }, 150);
            }
            scrollToBottom();
            updateArrowStates();
        } else {
            chatWindow.classList.remove('open');
            launcherBtn.classList.remove('active');
        }
    }

    launcherBtn.addEventListener('click', function () {
        toggleChat();
    });

    if (closeBtn) {
        closeBtn.addEventListener('click', function () {
            toggleChat(false);
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener('click', function () {
            if (confirm('Clear chat history?')) {
                chatHistory = [];
                sessionStorage.removeItem('nau_chat_history');
                renderInitialWelcome();
            }
        });
    }

    // Handle Input Events
    if (inputField) {
        inputField.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                sendMessage();
            }
        });
    }

    if (sendBtn) {
        sendBtn.addEventListener('click', function () {
            sendMessage();
        });
    }

    // --- Suggestions Carousel Scroll Controls ---
    function updateArrowStates() {
        if (!suggestionsContainer) return;
        var maxScroll = suggestionsContainer.scrollWidth - suggestionsContainer.clientWidth;
        if (suggestPrevBtn) {
            if (suggestionsContainer.scrollLeft <= 5) {
                suggestPrevBtn.classList.add('hidden');
            } else {
                suggestPrevBtn.classList.remove('hidden');
            }
        }
        if (suggestNextBtn) {
            if (maxScroll <= 5 || suggestionsContainer.scrollLeft >= maxScroll - 5) {
                suggestNextBtn.classList.add('hidden');
            } else {
                suggestNextBtn.classList.remove('hidden');
            }
        }
    }

    if (suggestPrevBtn && suggestionsContainer) {
        suggestPrevBtn.addEventListener('click', function () {
            suggestionsContainer.scrollBy({ left: -190, behavior: 'smooth' });
            setTimeout(updateArrowStates, 250);
        });
    }

    if (suggestNextBtn && suggestionsContainer) {
        suggestNextBtn.addEventListener('click', function () {
            suggestionsContainer.scrollBy({ left: 190, behavior: 'smooth' });
            setTimeout(updateArrowStates, 250);
        });
    }

    // Mouse Wheel on suggestions container scrolls horizontally
    if (suggestionsContainer) {
        suggestionsContainer.addEventListener('wheel', function (e) {
            if (e.deltaY !== 0) {
                e.preventDefault();
                suggestionsContainer.scrollLeft += e.deltaY * 0.9;
                updateArrowStates();
            }
        }, { passive: false });

        suggestionsContainer.addEventListener('scroll', function () {
            updateArrowStates();
        });

        // Mouse Drag-to-Scroll
        var isDown = false;
        var startX = 0;
        var scrollLeftVal = 0;
        var dragDistance = 0;

        suggestionsContainer.addEventListener('mousedown', function (e) {
            isDown = true;
            dragDistance = 0;
            suggestionsContainer.classList.add('dragging');
            startX = e.pageX - suggestionsContainer.offsetLeft;
            scrollLeftVal = suggestionsContainer.scrollLeft;
        });

        suggestionsContainer.addEventListener('mouseleave', function () {
            isDown = false;
            suggestionsContainer.classList.remove('dragging');
        });

        suggestionsContainer.addEventListener('mouseup', function () {
            isDown = false;
            suggestionsContainer.classList.remove('dragging');
        });

        suggestionsContainer.addEventListener('mousemove', function (e) {
            if (!isDown) return;
            e.preventDefault();
            var x = e.pageX - suggestionsContainer.offsetLeft;
            var walk = (x - startX) * 1.4;
            dragDistance = Math.abs(x - startX);
            suggestionsContainer.scrollLeft = scrollLeftVal - walk;
            updateArrowStates();
        });

        // Handle Suggestion Chips Click
        suggestionsContainer.addEventListener('click', function (e) {
            if (dragDistance > 6) {
                // User was dragging, ignore click
                return;
            }
            var chip = e.target.closest('.nau-chat-chip');
            if (chip) {
                var query = chip.getAttribute('data-query') || chip.textContent.trim();
                if (query && !isWaiting) {
                    if (inputField) inputField.value = query;
                    sendMessage();
                }
            }
        });
    }

    // Send Message
    async function sendMessage() {
        if (!inputField || isWaiting) return;
        var text = inputField.value.trim();
        if (!text) return;

        inputField.value = '';
        inputField.disabled = true;
        if (sendBtn) sendBtn.disabled = true;

        // Append User Message
        appendMessage('user', text);
        chatHistory.push({ role: 'user', content: text });
        saveHistory();

        // Check for simple greetings (hello, hi, etc.)
        var cleanQuery = text.toLowerCase().replace(/[^a-z0-9\s]/g, '').trim();
        var greetingWords = ['hi', 'hello', 'hey', 'salam', 'assalam', 'aoa', 'assalamu alaikum', 'assalam o alaikum', 'good morning', 'good evening', 'good afternoon'];
        var isGreeting = greetingWords.some(function (g) {
            return cleanQuery === g || cleanQuery === g + ' nau' || cleanQuery === g + ' bot' || cleanQuery === g + ' assistant' || cleanQuery.startsWith(g + ' ');
        });

        if (isGreeting) {
            var greetingReply = "Hello! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?";
            appendMessage('assistant', greetingReply);
            chatHistory.push({ role: 'assistant', content: greetingReply });
            saveHistory();
            isWaiting = false;
            if (inputField) {
                inputField.disabled = false;
                inputField.focus();
            }
            if (sendBtn) sendBtn.disabled = false;
            scrollToBottom();
            return;
        }

        // Show Typing Indicator
        isWaiting = true;
        showTyping(true);

        try {
            var response = await fetch('/Chatbot/Ask', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    message: text,
                    history: chatHistory.slice(-6)
                })
            });

            if (!response.ok) {
                throw new Error('Network error ' + response.status);
            }

            var data = await response.json();
            showTyping(false);

            if (data && data.reply) {
                appendMessage('assistant', data.reply);
                chatHistory.push({ role: 'assistant', content: data.reply });
                saveHistory();

                // Update suggestions if provided
                if (data.suggestedQuestions && data.suggestedQuestions.length > 0) {
                    updateSuggestions(data.suggestedQuestions);
                }
            } else {
                appendMessage('assistant', "I'm sorry, I could not retrieve an answer right now. Please contact the NAU helpdesk at **+92 22 9260310-19**.");
            }
        } catch (err) {
            console.error('Chatbot error:', err);
            showTyping(false);
            appendMessage('assistant', "Connection issue. Please check your internet or reach out directly to the Academics Office at **AdID.Academics@paa.gov.pk**.");
        } finally {
            isWaiting = false;
            if (inputField) {
                inputField.disabled = false;
                inputField.focus();
            }
            if (sendBtn) sendBtn.disabled = false;
            scrollToBottom();
        }
    }

    function appendMessage(role, rawContent) {
        if (!messagesContainer) return;

        var msgDiv = document.createElement('div');
        msgDiv.className = 'nau-chat-msg ' + role;

        var avatarDiv = document.createElement('div');
        avatarDiv.className = 'nau-chat-msg-avatar';
        avatarDiv.innerHTML = role === 'user' ? '<i class="fas fa-user"></i>' : '<i class="fas fa-robot"></i>';

        var bubbleDiv = document.createElement('div');
        bubbleDiv.className = 'nau-chat-bubble';
        bubbleDiv.innerHTML = formatMarkdown(rawContent);

        msgDiv.appendChild(avatarDiv);
        msgDiv.appendChild(bubbleDiv);

        // Insert before typing indicator
        if (typingIndicator && typingIndicator.parentNode === messagesContainer) {
            messagesContainer.insertBefore(msgDiv, typingIndicator);
        } else {
            messagesContainer.appendChild(msgDiv);
        }

        scrollToBottom();
    }

    function showTyping(show) {
        if (!typingIndicator) return;
        if (show) {
            typingIndicator.classList.add('active');
        } else {
            typingIndicator.classList.remove('active');
        }
        scrollToBottom();
    }

    function scrollToBottom() {
        if (messagesContainer) {
            setTimeout(function () {
                messagesContainer.scrollTop = messagesContainer.scrollHeight;
            }, 50);
        }
    }

    function getSuggestionIcon(q) {
        var lower = q.toLowerCase();
        if (lower.includes('admission') || lower.includes('apply') || lower.includes('register')) return 'fas fa-graduation-cap';
        if (lower.includes('degree') || lower.includes('program') || lower.includes('faculty') || lower.includes('faculties')) return 'fas fa-award';
        if (lower.includes('course') || lower.includes('training') || lower.includes('atp')) return 'fas fa-book-open';
        if (lower.includes('fee') || lower.includes('cost') || lower.includes('scholarship')) return 'fas fa-coins';
        if (lower.includes('hostel') || lower.includes('facility') || lower.includes('facilities') || lower.includes('library') || lower.includes('mess')) return 'fas fa-building-columns';
        if (lower.includes('sim') || lower.includes('flight') || lower.includes('radar') || lower.includes('lab')) return 'fas fa-plane-departure';
        if (lower.includes('contact') || lower.includes('helpline') || lower.includes('location') || lower.includes('address') || lower.includes('desk')) return 'fas fa-headset';
        return 'fas fa-circle-question';
    }

    function updateSuggestions(questions) {
        if (!suggestionsContainer) return;
        suggestionsContainer.innerHTML = '';
        questions.forEach(function (q) {
            var chip = document.createElement('button');
            chip.type = 'button';
            chip.className = 'nau-chat-chip';
            chip.setAttribute('data-query', q);
            chip.innerHTML = '<i class="' + getSuggestionIcon(q) + '"></i> ' + escapeHtml(q);
            suggestionsContainer.appendChild(chip);
        });
        suggestionsContainer.scrollLeft = 0;
        updateArrowStates();
    }

    function stripEmojis(str) {
        if (!str) return '';
        // Match emoji ranges and pictographs
        return str.replace(/[\u{1F300}-\u{1F9FF}\u{2600}-\u{26FF}\u{2700}-\u{27BF}\u{1F600}-\u{1F64F}\u{1F680}-\u{1F6FF}\u{2B50}\u{2B55}]/gu, '');
    }

    function formatMarkdown(text) {
        if (!text) return '';
        text = stripEmojis(text);
        var html = escapeHtml(text);

        // Headings: ### Heading or ## Heading
        html = html.replace(/^### (.*$)/gim, '<h3 style="font-size:14px; font-weight:700; margin:8px 0 4px 0;">$1</h3>');
        html = html.replace(/^## (.*$)/gim, '<h3 style="font-size:14.5px; font-weight:700; margin:8px 0 4px 0;">$1</h3>');

        // Bold: **text**
        html = html.replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>');
        // Italic: *text*
        html = html.replace(/\*(.*?)\*/g, '<em>$1</em>');
        // Inline code: `code`
        html = html.replace(/`(.*?)`/g, '<code>$1</code>');
        // Links: [text](url)
        html = html.replace(/\[(.*?)\]\((.*?)\)/g, '<a href="$2" class="nau-chat-link" target="_self">$1</a>');

        // Split into lines to cleanly handle lists and paragraphs
        var lines = html.split('\n');
        var result = [];
        var inList = false;

        for (var i = 0; i < lines.length; i++) {
            var line = lines[i].trim();
            var bulletMatch = line.match(/^[-*•]\s+(.*)$/);
            var numMatch = line.match(/^\d+\.\s+(.*)$/);

            if (bulletMatch || numMatch) {
                if (!inList) {
                    result.push('<ul style="margin:6px 0; padding-left:18px;">');
                    inList = true;
                }
                var itemContent = bulletMatch ? bulletMatch[1] : numMatch[1];
                result.push('<li style="margin-bottom:4px;">' + itemContent + '</li>');
            } else {
                if (inList) {
                    result.push('</ul>');
                    inList = false;
                }
                if (line.length > 0) {
                    result.push(line);
                } else if (result.length > 0 && result[result.length - 1] !== '<br>') {
                    result.push('<br>');
                }
            }
        }
        if (inList) {
            result.push('</ul>');
        }

        return result.join('<br>').replace(/(<br>\s*)+(<ul)/g, '$2').replace(/(<\/ul>\s*)+(<br>\s*)+/g, '</ul>');
    }

    function escapeHtml(str) {
        if (!str) return '';
        return str
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }

    function saveHistory() {
        try {
            sessionStorage.setItem('nau_chat_history', JSON.stringify(chatHistory));
        } catch (e) { }
    }

    function loadHistory() {
        try {
            var saved = sessionStorage.getItem('nau_chat_history');
            if (saved) {
                chatHistory = JSON.parse(saved);
                if (chatHistory && chatHistory.length > 0) {
                    if (messagesContainer) {
                        // Clear existing default greeting before restoring history
                        var existingMsgs = messagesContainer.querySelectorAll('.nau-chat-msg');
                        existingMsgs.forEach(function (m) { m.remove(); });
                    }
                    chatHistory.forEach(function (m) {
                        appendMessage(m.role, m.content);
                    });
                    return true;
                }
            }
        } catch (e) { }
        return false;
    }

    function renderInitialWelcome() {
        if (!messagesContainer) return;
        var existingMsgs = messagesContainer.querySelectorAll('.nau-chat-msg');
        existingMsgs.forEach(function (m) { m.remove(); });

        var welcomeText = "Hello! Welcome to National Aviation University (NAU) Pakistan. How can I assist you today?";

        appendMessage('assistant', welcomeText);
    }

    // Initialize
    var restored = loadHistory();
    if (!restored) {
        renderInitialWelcome();
    }
    setTimeout(updateArrowStates, 300);
})();
