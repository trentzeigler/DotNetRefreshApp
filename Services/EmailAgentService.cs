using DotNetRefreshApp.Data;
using DotNetRefreshApp.Models;
using Microsoft.EntityFrameworkCore;
using OpenAI.Chat;
using System.Text.Json;

namespace DotNetRefreshApp.Services
{
    /// <summary>
    /// Service to manage email-related AI agent operations with function calling
    /// </summary>
    public class EmailAgentService
    {
        private readonly IEmailService _emailService;
        private readonly AppDbContext _context;
        private readonly ILogger<EmailAgentService> _logger;

        public EmailAgentService(IEmailService emailService, AppDbContext context, ILogger<EmailAgentService> logger)
        {
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get the system instructions for the email assistant
        /// </summary>
        public string GetSystemInstructions()
        {
            return @"You are an intelligent email assistant helping users manage their email communications.
You can draft emails, send emails, and help users with email-related tasks.

When drafting emails:
- Be professional and courteous
- Ask for clarification if the request is ambiguous
- Confirm important details before sending
- Use proper email formatting

When the user asks you to send an email:
1. First draft the email and show it to them using the draft_email tool
2. Ask for explicit confirmation before sending
3. Only send after the user explicitly approves (e.g., ""yes, send it"", ""looks good, send"", ""send the email"")
4. NEVER send an email without explicit user approval

Always be helpful, concise, and professional.";
        }

        /// <summary>
        /// Get the function/tool definitions for OpenAI
        /// </summary>
        public List<ChatTool> GetTools()
        {
            var tools = new List<ChatTool>();

            // Tool 1: Draft Email
            tools.Add(ChatTool.CreateFunctionTool(
                functionName: "draft_email",
                functionDescription: "Creates an email draft to show to the user for review. Use this when the user asks to compose or draft an email.",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""recipient_email"": {
                            ""type"": ""string"",
                            ""description"": ""The recipient's email address""
                        },
                        ""subject"": {
                            ""type"": ""string"",
                            ""description"": ""The email subject line""
                        },
                        ""body"": {
                            ""type"": ""string"",
                            ""description"": ""The email body content""
                        }
                    },
                    ""required"": [""recipient_email"", ""subject"", ""body""]
                }")
            ));

            // Tool 2: Send Email
            tools.Add(ChatTool.CreateFunctionTool(
                functionName: "send_email",
                functionDescription: "Sends an email. ONLY use this after the user has explicitly approved sending the email. Never call this without explicit user confirmation.",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {
                        ""recipient_email"": {
                            ""type"": ""string"",
                            ""description"": ""The recipient's email address""
                        },
                        ""subject"": {
                            ""type"": ""string"",
                            ""description"": ""The email subject line""
                        },
                        ""body"": {
                            ""type"": ""string"",
                            ""description"": ""The email body content""
                        }
                    },
                    ""required"": [""recipient_email"", ""subject"", ""body""]
                }")
            ));

            // Tool 3: Get User Email
            tools.Add(ChatTool.CreateFunctionTool(
                functionName: "get_user_email",
                functionDescription: "Gets the authenticated user's email address to use as the sender.",
                functionParameters: BinaryData.FromString(@"{
                    ""type"": ""object"",
                    ""properties"": {},
                    ""required"": []
                }")
            ));

            return tools;
        }

        /// <summary>
        /// Execute a tool/function call
        /// </summary>
        public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson, int userId)
        {
            try
            {
                _logger.LogInformation($"Executing tool: {toolName} with args: {argumentsJson}");

                switch (toolName)
                {
                    case "draft_email":
                        return await DraftEmail(argumentsJson);

                    case "send_email":
                        return await SendEmail(argumentsJson, userId);

                    case "get_user_email":
                        return await GetUserEmail(userId);

                    default:
                        return JsonSerializer.Serialize(new { error = $"Unknown tool: {toolName}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing tool {toolName}");
                return JsonSerializer.Serialize(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Draft an email and return it for user review
        /// </summary>
        private async Task<string> DraftEmail(string argumentsJson)
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argumentsJson);
            
            if (args == null)
                return JsonSerializer.Serialize(new { error = "Invalid arguments" });

            var draft = new
            {
                to = args.GetValueOrDefault("recipient_email", ""),
                subject = args.GetValueOrDefault("subject", ""),
                body = args.GetValueOrDefault("body", ""),
                status = "draft_created"
            };

            await Task.CompletedTask; // Placeholder for async
            return JsonSerializer.Serialize(draft);
        }

        /// <summary>
        /// Send an email using the email service
        /// </summary>
        private async Task<string> SendEmail(string argumentsJson, int userId)
        {
            var args = JsonSerializer.Deserialize<Dictionary<string, string>>(argumentsJson);
            
            if (args == null)
                return JsonSerializer.Serialize(new { error = "Invalid arguments" });

            // Get user's email address
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return JsonSerializer.Serialize(new { error = "User not found" });

            var emailDraft = new EmailDraft
            {
                To = args.GetValueOrDefault("recipient_email", ""),
                Subject = args.GetValueOrDefault("subject", ""),
                Body = args.GetValueOrDefault("body", ""),
                From = user.Email,
                IsHtml = false
            };

            try
            {
                var success = await _emailService.SendEmailAsync(emailDraft);
                
                if (success)
                {
                    return JsonSerializer.Serialize(new 
                    { 
                        status = "sent",
                        message = $"Email sent successfully to {emailDraft.To}"
                    });
                }
                else
                {
                    return JsonSerializer.Serialize(new 
                    { 
                        status = "failed",
                        error = "Failed to send email"
                    });
                }
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new 
                { 
                    status = "failed",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get the user's email address
        /// </summary>
        private async Task<string> GetUserEmail(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
                return JsonSerializer.Serialize(new { error = "User not found" });

            return JsonSerializer.Serialize(new 
            { 
                email = user.Email,
                name = user.Name
            });
        }
    }
}
