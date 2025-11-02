using Microsoft.EntityFrameworkCore;
using MyChatApp.Data;
using MyChatApp.Models;

namespace MyChatApp.Services
{
    public class ChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<Chat>> GetAllAsync()
        {
            return await _db.Chats.Include(c => c.Messages).OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<Chat?> GetAsync(int id)
        {
            return await _db.Chats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Chat> CreateAsync(string title, string model)
        {
            var chat = new Chat { Title = title, Model = model };
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync();
            return chat;
        }

        public async Task<Message> AddMessageAsync(int chatId, string role, string content)
        {
            var msg = new Message { ChatId = chatId, Role = role, Content = content };
            _db.Messages.Add(msg);
            await _db.SaveChangesAsync();
            return msg;
        }
    }
}
