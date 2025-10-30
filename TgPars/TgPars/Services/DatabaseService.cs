using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgPars.Models;

namespace TgPars.Services
{
    internal class DatabaseService
    {
        private readonly AppDbContext _dbContext;

        public DatabaseService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<bool> IsChatInDatabaseAsync(long chatId)
        {
            return await _dbContext.ChatsToParse.AnyAsync(c => c.ChatId == chatId);
        }

        public async Task<ChatToParse> GetChatAsync(long chatId)
        {
            return await _dbContext.ChatsToParse.FirstOrDefaultAsync(c => c.ChatId == chatId);
        }

        public async Task AddChatAsync(long chatId, string chatTitle)
        {
            _dbContext.ChatsToParse.Add(new ChatToParse { ChatId = chatId, ChatTitle = chatTitle });
            await _dbContext.SaveChangesAsync();
        }

        public async Task<List<string>> GetFilterKeywordsAsync()
        {
            return await _dbContext.FilterKeywords.Select(k => k.Keyword).ToListAsync();
        }

        public async Task AddFilterKeywordAsync(string keyword)
        {
            if (!await _dbContext.FilterKeywords.AnyAsync(k => k.Keyword == keyword))
            {
                _dbContext.FilterKeywords.Add(new FilterKeyword { Keyword = keyword });
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
