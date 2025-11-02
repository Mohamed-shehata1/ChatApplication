using Microsoft.AspNetCore.Mvc;
using MyChatApp.Models;
using MyChatApp.Services;

namespace MyChatApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly ChatService _chatService;
        private readonly OpenAiService _openAi;
        private readonly IConfiguration _config;
        private readonly ILogger<ChatController> _logger;

        public ChatController(ChatService chatService, OpenAiService openAi, IConfiguration config, ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _openAi = openAi;
            _config = config;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var chats = await _chatService.GetAllAsync();
            return View(chats);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string title, string model)
        {
            var m = string.IsNullOrWhiteSpace(model) ? _config.GetValue<string>("OpenAI:DefaultModel") : model;
            await _chatService.CreateAsync(title ?? "New Chat", m ?? "gpt-4o-mini");
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Open(int id)
        {
            var chat = await _chatService.GetAsync(id);
            if (chat == null) return NotFound();
            return View(chat);
        }

        [HttpPost]
        [Produces("application/json")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
        {
            var chat = await _chatService.GetAsync(req.ChatId);
            if (chat == null) return NotFound();

            await _chatService.AddMessageAsync(req.ChatId, "user", req.Content);

            var history = (await _chat_service_get_messages(req.ChatId));
            history.Add(new Message { Role = "user", Content = req.Content });

            var model = chat.Model ?? _config.GetValue<string>("OpenAI:DefaultModel");
            var assistantReply = await _openAi.SendChatAsync(history, model ?? "gpt-4o-mini");
            // Log assistant reply (or error)
            _logger.LogInformation("Assistant reply for chat {ChatId}: {Reply}", req.ChatId, assistantReply);

            // If OpenAiService returned an error string prefixed with "ERROR:", log and return 500 with details
            if (!string.IsNullOrEmpty(assistantReply) && assistantReply.StartsWith("ERROR:"))
            {
                _logger.LogWarning("OpenAI returned error for chat {ChatId}: {Error}", req.ChatId, assistantReply);
                // Save assistant message containing error for visibility
                await _chatService.AddMessageAsync(req.ChatId, "assistant", assistantReply);
                return StatusCode(500, new { error = assistantReply });
            }

            // Save assistant reply and return JSON
            await _chatService.AddMessageAsync(req.ChatId, "assistant", assistantReply ?? string.Empty);

            return Ok(new { reply = assistantReply ?? string.Empty });
        }

        // Helper to get current messages for a chat ordered by CreatedAt
        private async Task<List<Message>> _chat_service_get_messages(int chatId)
        {
            var chat = await _chatService.GetAsync(chatId);
            return chat?.Messages.OrderBy(m => m.CreatedAt).Select(m => new Message { Role = m.Role, Content = m.Content }).ToList() ?? new List<Message>();
        }

        public class SendMessageRequest
        {
            public int ChatId { get; set; }
            public string Content { get; set; } = string.Empty;
        }
    }
}
