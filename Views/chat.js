// Check authentication
const authToken = sessionStorage.getItem('authToken');
const userId = parseInt(sessionStorage.getItem('userId'));

if (!authToken || !userId) {
    // Not authenticated, redirect to login
    window.location.href = '/api/auth/login';
}

const chatMessages = document.getElementById('chatMessages');
const messageInput = document.getElementById('messageInput');
const sendButton = document.getElementById('sendButton');
const errorMessage = document.getElementById('errorMessage');

// Generate or retrieve conversation ID from session storage
let conversationId = sessionStorage.getItem('conversationId');
if (!conversationId) {
    conversationId = 'conv_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    sessionStorage.setItem('conversationId', conversationId);
}

// Configure marked to interpret newlines as <br>
marked.setOptions({ breaks: true });

/**
 * Add a message to the chat UI
 * @param {string} text - The message text
 * @param {string} type - Either 'user' or 'assistant'
 * @returns {HTMLElement} The message content element
 */
function addMessage(text, type) {
    const messageDiv = document.createElement('div');
    messageDiv.className = `message ${type}`;

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.textContent = type === 'user' ? 'You' : 'AI';

    const content = document.createElement('div');
    content.className = 'message-content';
    content.textContent = text;

    messageDiv.appendChild(avatar);
    messageDiv.appendChild(content);
    chatMessages.appendChild(messageDiv);

    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;

    return content;
}

/**
 * Add typing indicator to show AI is responding
 * @returns {HTMLElement} The typing indicator message element
 */
function addTypingIndicator() {
    const messageDiv = document.createElement('div');
    messageDiv.className = 'message assistant';
    messageDiv.id = 'typing-indicator';

    const avatar = document.createElement('div');
    avatar.className = 'message-avatar';
    avatar.textContent = 'AI';

    const content = document.createElement('div');
    content.className = 'message-content';

    const typingDiv = document.createElement('div');
    typingDiv.className = 'typing-indicator';

    for (let i = 0; i < 3; i++) {
        const dot = document.createElement('div');
        dot.className = 'typing-dot';
        typingDiv.appendChild(dot);
    }

    content.appendChild(typingDiv);
    messageDiv.appendChild(avatar);
    messageDiv.appendChild(content);
    chatMessages.appendChild(messageDiv);

    // Scroll to bottom
    chatMessages.scrollTop = chatMessages.scrollHeight;

    return messageDiv;
}

/**
 * Remove typing indicator from chat
 */
function removeTypingIndicator() {
    const indicator = document.getElementById('typing-indicator');
    if (indicator) {
        indicator.remove();
    }
}

/**
 * Show error message to user
 * @param {string} message - The error message to display
 */
function showError(message) {
    errorMessage.textContent = message;
    errorMessage.classList.add('active');
    setTimeout(() => {
        errorMessage.classList.remove('active');
    }, 5000);
}

/**
 * Add a status message (for tool calls)
 * @param {string} text - The status text
 */
function addStatusMessage(text) {
    const statusDiv = document.createElement('div');
    statusDiv.className = 'status-message';
    statusDiv.textContent = text;
    chatMessages.appendChild(statusDiv);
    chatMessages.scrollTop = chatMessages.scrollHeight;
}

/**
 * Send a message to the AI and handle the streaming response with tool calls
 */
async function sendMessage() {
    const message = messageInput.value.trim();
    if (!message) return;

    // Add user message to UI
    addMessage(message, 'user');
    messageInput.value = '';
    sendButton.disabled = true;

    // Show typing indicator
    const typingIndicator = addTypingIndicator();

    try {
        const response = await fetch(`/ai/conversation/${conversationId}`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${authToken}`
            },
            body: JSON.stringify({
                message: message,
                userId: userId
            })
        });

        if (!response.ok) {
            throw new Error(`Server error: ${response.status} ${response.statusText}`);
        }

        // Remove typing indicator before streaming starts
        removeTypingIndicator();

        const reader = response.body.getReader();
        const decoder = new TextDecoder();

        let currentMessageContent = null;
        let currentFullText = '';
        let hasReceivedContent = false;

        while (true) {
            const { done, value } = await reader.read();
            if (done) break;

            const chunk = decoder.decode(value);
            const lines = chunk.split('\n');

            for (const line of lines) {
                if (line.startsWith('data: ')) {
                    const data = line.substring(6);
                    if (data === '[DONE]') continue;

                    try {
                        const parsed = JSON.parse(data);

                        // Handle regular content
                        if (parsed.content) {
                            hasReceivedContent = true;

                            if (!currentMessageContent) {
                                currentMessageContent = addMessage('', 'assistant');
                                currentFullText = '';
                            }

                            currentFullText += parsed.content;
                            // Use marked to parse markdown
                            currentMessageContent.innerHTML = marked.parse(currentFullText);
                            chatMessages.scrollTop = chatMessages.scrollHeight;
                        }

                        // Handle tool calls
                        if (parsed.tool_call) {
                            hasReceivedContent = true;
                            // Force new bubble for next content
                            currentMessageContent = null;
                            addStatusMessage(`Using tool: ${parsed.tool_call}`);
                        }

                        // Handle tool results
                        if (parsed.tool_result) {
                            const resultObj = JSON.parse(parsed.result);
                            let statusText = 'Tool completed';

                            // Customize status message based on result content
                            if (resultObj.status === 'sent') {
                                statusText = '✅ Email sent successfully';
                            } else if (resultObj.to && resultObj.subject) {
                                statusText = `📝 Drafted email to ${resultObj.to}`;
                            }

                            addStatusMessage(statusText);
                        }

                        // Handle errors
                        if (parsed.error) {
                            showError(parsed.error);
                        }
                    } catch (e) {
                        // Ignore parse errors for incomplete chunks
                        console.warn('Failed to parse chunk:', e);
                    }
                }
            }
        }

        // Check if we received any content
        if (!hasReceivedContent) {
            showError('Received an empty response from the AI. Please try again.');
        }

    } catch (error) {
        console.error('Error:', error);
        removeTypingIndicator();
        showError(`Failed to send message: ${error.message}`);
    } finally {
        sendButton.disabled = false;
        messageInput.focus();
    }
}

// Event listeners
sendButton.addEventListener('click', sendMessage);
messageInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
        sendMessage();
    }
});

// Sign out button
const signOutButton = document.getElementById('signOutButton');
let isSigningOut = false;

signOutButton.addEventListener('click', () => {
    isSigningOut = true;
    // Clear session storage
    sessionStorage.clear();
    // Redirect to login
    window.location.href = '/api/auth/login';
});

// Restore chat history on load
const savedHistory = sessionStorage.getItem('chatHistory');
if (savedHistory) {
    chatMessages.innerHTML = savedHistory;
    // Remove any typing indicator that might have been saved
    const savedIndicator = document.getElementById('typing-indicator');
    if (savedIndicator) {
        savedIndicator.remove();
    }
    chatMessages.scrollTop = chatMessages.scrollHeight;
    // Clear it after restoring so we don't keep stale data if the user navigates away and back without refreshing
    // But the requirement says "then delete", which implies we consume it.
    sessionStorage.removeItem('chatHistory');
}

// Save chat history before unload
window.addEventListener('beforeunload', () => {
    if (!isSigningOut && chatMessages.innerHTML.trim()) {
        sessionStorage.setItem('chatHistory', chatMessages.innerHTML);
    }
});

// Focus input on load
messageInput.focus();
