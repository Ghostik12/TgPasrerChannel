using TgPars.Models;
using Microsoft.EntityFrameworkCore;
using TgPars.DB;
using TgPars.Services;

namespace TelegramClientParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Конфигурация
            const string apiId = "ВАШ_API_ID"; // Например, 1234567
            const string apiHash = "ВАШ_API_HASH"; // Например, 01234abcdef56789abcdef0123456789
            const string phoneNumber = "+ВАШ_НОМЕР_ТЕЛЕФОНА"; // Например, +1234567890
            const string adminUserId = "ВАШ_USER_ID"; // Ваш Telegram ID

            // Настройка базы данных
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=parserbot.db")
                .Options;

            using var dbContext = new AppDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();

            // Инициализация сервисов
            var dbService = new DatabaseService(dbContext);
            var client = new WTelegram.Client();
            var messageHandler = new MessageHandler(client, dbService, adminUserId, null); // Временный null, user установим позже
            var telegramService = new TelegramClientService(apiId, apiHash, phoneNumber, messageHandler);

            // Запуск
            var user = await telegramService.StartAsync();
            messageHandler = new MessageHandler(client, dbService, adminUserId, user); // Обновляем handler с user

            // Держим приложение открытым
            Console.ReadLine();
        }
    }
}