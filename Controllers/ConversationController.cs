using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DotNetRefreshApp.Data;
using DotNetRefreshApp.Models;
using DotNetRefreshApp.Services;
using OpenAI.Chat;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace DotNetRefreshApp.Controllers
{
    [ApiController]
    [Route("ai")]
    public class ConversationController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConversationController> _logger;
        private readonly IWebHostEnvironment _env;

        public ConversationController(
            AppDbContext context, 
            IConfiguration configuration, 
            ILogger<ConversationController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// GET /chat.css - Serves the chat UI (Server-Side Rendering)
        /// </summary>
        [HttpGet("chat.css")]
        public async Task<IActionResult> ChatCss()
        {
            var cssPath = Path.Combine(_env.ContentRootPath, "Views", "chat.css");
            
            if (!System.IO.File.Exists(cssPath))
            {
                return NotFound("Chat UI not found");
            }

            var css = await System.IO.File.ReadAllTextAsync(cssPath);
            return Content(css, "text/css");
        }

        /// <summary>
        /// GET /chat.js - Serves the chat UI (Server-Side Rendering)
        /// </summary>
        [HttpGet("chat.js")]
        public async Task<IActionResult> ChatJs()
        {
            var jsPath = Path.Combine(_env.ContentRootPath, "Views", "chat.js");
            
            if (!System.IO.File.Exists(jsPath))
            {
                return NotFound("Chat UI not found");
            }

            var js = await System.IO.File.ReadAllTextAsync(jsPath);
            return Content(js, "text/javascript");
        }

        /// <summary>
        /// GET /favicon.svg - Serves the favicon
        /// </summary>
        [HttpGet("favicon.svg")]
        public async Task<IActionResult> Favicon()
        {
            var faviconPath = Path.Combine(_env.ContentRootPath, "Views", "favicon.svg");
            
            if (!System.IO.File.Exists(faviconPath))
            {
                return NotFound("Favicon not found");
            }

            var svg = await System.IO.File.ReadAllTextAsync(faviconPath);
            return Content(svg, "image/svg+xml");
        }


        /// <summary>
        /// GET /conversation - Serves the chat UI (Server-Side Rendering)
        /// </summary>
        [HttpGet("conversation")]
        public async Task<IActionResult> Index()
        {
            var htmlPath = Path.Combine(_env.ContentRootPath, "Views", "chat.html");
            
            if (!System.IO.File.Exists(htmlPath))
            {
                return NotFound("Chat UI not found");
            }

            var html = await System.IO.File.ReadAllTextAsync(htmlPath);
            return Content(html, "text/html");
        }

        /// <summary>
        /// POST /conversation/{conversationId} - Handles chat messages with streaming response and function calling
        /// </summary>
        [Authorize]
        [HttpPost("conversation/{conversationId}")]
        public async Task StreamChat([FromBody] ChatRequest request, [FromRoute] string conversationId, [FromServices] EmailAgentService emailAgent)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            try
            {
                // Get UserId from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    Response.StatusCode = 401;
                    await Response.WriteAsync("Unauthorized");
                    return;
                }

                // Verify user exists in database (in case of DB wipe or deletion)
                if (!await _context.Users.AnyAsync(u => u.Id == userId))
                {
                    Response.StatusCode = 401;
                    await Response.WriteAsync("User not found");
                    return;
                }

                // Get conversation
                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.UserId == userId);

                // Create conversation if it doesn't exist
                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        ConversationId = conversationId,
                        UserId = userId,
                        MessagesJson = "[]",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Conversations.Add(conversation);
                }

                // Get existing messages
                var messages = System.Text.Json.JsonSerializer.Deserialize<List<ConversationMessageDto>>(conversation.MessagesJson);

                // Get OpenAI configuration
                var apiKey = _configuration["OPENAI_API_KEY"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
                var model = _configuration["OPENAI_MODEL"] ?? Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4";

                // Validate OpenAI API key
                if (string.IsNullOrEmpty(apiKey) || apiKey.Contains("your-api-key-here"))
                {
                    await WriteStreamError("OpenAI API key not configured. Please set OPENAI_API_KEY in your .env file.");
                    return;
                }

                // Initialize OpenAI client
                var client = new ChatClient(model, apiKey);

                // Build message list for OpenAI
                var chatMessages = new List<ChatMessage>();
                
                // Add system message with email assistant instructions
                chatMessages.Add(new SystemChatMessage(emailAgent.GetSystemInstructions()));

                // Add conversation history
                foreach (var msg in messages)
                {
                    // Add user message to OpenAI chat
                    if (msg.MessageType == "user")
                    {
                        chatMessages.Add(new UserChatMessage(msg.MessageText));
                    }
                    // Add assistant message to OpenAI chat
                    else if (msg.MessageType == "assistant")
                    {
                        chatMessages.Add(new AssistantChatMessage(msg.MessageText));
                    }
                }

                // Add current user message
                chatMessages.Add(new UserChatMessage(request.Message));

                // Add user message to conversation history
                messages.Add(new ConversationMessageDto
                {
                    MessageText = request.Message,
                    MessageType = "user",
                    Timestamp = DateTime.UtcNow
                });

                // Get tools for function calling
                var tools = emailAgent.GetTools();
                var chatOptions = new ChatCompletionOptions();
                foreach (var tool in tools)
                {
                    chatOptions.Tools.Add(tool);
                }

                // Iterative function calling loop
                var fullResponse = new StringBuilder();
                var maxIterations = 10; // Prevent infinite loops
                var iteration = 0;

                while (iteration < maxIterations)
                {
                    iteration++;
                    
                    // Call OpenAI with streaming
                    var currentResponse = new StringBuilder();
                    var toolCallsDict = new Dictionary<int, (string id, string name, StringBuilder args)>();
                    
                    await foreach (StreamingChatCompletionUpdate update in client.CompleteChatStreamingAsync(chatMessages, chatOptions))
                    {
                        // Handle text content
                        foreach (ChatMessageContentPart contentPart in update.ContentUpdate)
                        {
                            if (!string.IsNullOrEmpty(contentPart.Text))
                            {
                                var content = contentPart.Text;
                                currentResponse.Append(content);
                                fullResponse.Append(content);

                                // Send SSE chunk to client
                                var json = System.Text.Json.JsonSerializer.Serialize(new { content });
                                await Response.WriteAsync($"data: {json}\n\n");
                                await Response.Body.FlushAsync();
                            }
                        }

                        // Handle tool calls - accumulate streaming updates
                        foreach (StreamingChatToolCallUpdate toolCallUpdate in update.ToolCallUpdates)
                        {
                            var index = toolCallUpdate.Index;
                            
                            if (!toolCallsDict.ContainsKey(index))
                            {
                                toolCallsDict[index] = (
                                    toolCallUpdate.ToolCallId ?? string.Empty,
                                    toolCallUpdate.FunctionName ?? string.Empty,
                                    new StringBuilder()
                                );
                            }
                            
                            // Append function arguments as they stream in
                            if (toolCallUpdate.FunctionArgumentsUpdate != null)
                            {
                                var current = toolCallsDict[index];
                                current.args.Append(toolCallUpdate.FunctionArgumentsUpdate.ToString());
                                toolCallsDict[index] = current;
                            }
                        }
                    }

                    // If no tool calls, we're done
                    if (toolCallsDict.Count == 0)
                    {
                        break;
                    }

                    // Execute tool calls and stream to frontend
                    foreach (var (index, (id, name, args)) in toolCallsDict.OrderBy(x => x.Key))
                    {
                        var toolName = name;
                        var toolArgs = args.ToString();

                        // Stream tool call info to frontend
                        var toolCallJson = System.Text.Json.JsonSerializer.Serialize(new 
                        { 
                            tool_call = toolName,
                            arguments = toolArgs
                        });
                        await Response.WriteAsync($"data: {toolCallJson}\n\n");
                        await Response.Body.FlushAsync();

                        // Execute the tool
                        var toolResult = await emailAgent.ExecuteToolAsync(toolName, toolArgs, request.UserId);

                        // Stream tool result to frontend
                        var toolResultJson = System.Text.Json.JsonSerializer.Serialize(new 
                        { 
                            tool_result = toolName,
                            result = toolResult
                        });
                        await Response.WriteAsync($"data: {toolResultJson}\n\n");
                        await Response.Body.FlushAsync();

                        // Add tool call and result to chat history for next iteration
                        var toolCall = ChatToolCall.CreateFunctionToolCall(id, toolName, BinaryData.FromString(toolArgs));
                        chatMessages.Add(new AssistantChatMessage(new[] { toolCall }));
                        chatMessages.Add(ChatMessage.CreateToolMessage(id, toolResult));
                    }

                    // Continue loop to let AI process tool results
                }

                // Add assistant response to conversation history
                messages.Add(new ConversationMessageDto
                {
                    MessageText = fullResponse.ToString(),
                    MessageType = "assistant",
                    Timestamp = DateTime.UtcNow
                });

                // Update conversation in database
                conversation.MessagesJson = System.Text.Json.JsonSerializer.Serialize(messages);
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Send completion signal
                await Response.WriteAsync("data: [DONE]\n\n");
                await Response.Body.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in chat streaming");
                await WriteStreamError($"Error: {ex.Message}");
            }
        }


        private async Task WriteStreamError(string error)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(new { error });
            await Response.WriteAsync($"data: {json}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    // Request model for chat endpoint
    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public int UserId { get; set; }
    }
}
