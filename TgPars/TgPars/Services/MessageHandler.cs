using TL;
using TgPars.Services;

namespace TgPars.Services
{
    public class MessageHandler
    {
        private readonly WTelegram.Client _client;
        private readonly DatabaseService _dbService;
        private readonly string _adminUserId;
        private readonly User _myUser;

        public MessageHandler(WTelegram.Client client, DatabaseService dbService, string adminUserId, User myUser)
        {
            _client = client;
            _dbService = dbService;
            _adminUserId = adminUserId;
            _myUser = myUser;
        }

        public async Task HandleUpdateAsync(IObject updates)
        {
            if (updates is not UpdatesBase { Updates: var updateList })
                return;

            foreach (var update in updateList)
            {
                if (update is UpdateNewMessage { message: Message message })
                {
                    var chat = message.PeerChat;
                    if (chat == null)
                        continue;

                    // Проверяем, есть ли чат в базе данных
                    if (!await _dbService.IsChatInDatabaseAsync(chat.ID))
                        continue;

                    // Применяем фильтры
                    var keywords = await _dbService.GetFilterKeywordsAsync();
                    if (keywords.Any() && message.message != null && !keywords.Any(k => message.message.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    // Обрабатываем сообщение
                    var chatToParse = await _dbService.GetChatAsync(chat.ID);
                    Console.WriteLine($"Сообщение в {chatToParse.ChatTitle} (ID: {chat.ID}): {message.message}");
                    await ProcessMessageAsync(message, chatToParse.ChatTitle);
                }
                else if (update is UpdateNewMessage { message: MessageService { message: string msg } message } && message.From.ID.ToString() == _adminUserId)
                {
                    // Обработка команд в личном чате
                    await HandleCommandAsync(message);
                }
            }
        }

        private async Task HandleCommandAsync(Message message)
        {
            var text = message.message?.ToLower() ?? "";
            var peer = new InputPeerUser(_myUser.id, _myUser.access_hash);

            if (text.StartsWith("/addchat"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2 || !long.TryParse(parts[1], out var chatId))
                {
                    await _client.SendMessageAsync(peer, "Укажите корректный ID чата: /addchat <chat_id>");
                    return;
                }

                if (await _dbService.IsChatInDatabaseAsync(chatId))
                {
                    await _client.SendMessageAsync(peer, $"Чат с ID {chatId} уже добавлен.");
                    return;
                }

                try
                {
                    var chat = await _client.Messages_GetChats(new[] { chatId });
                    var chatTitle = chat.chats[chatId].Title;
                    await _dbService.AddChatAsync(chatId, chatTitle);
                    await _client.SendMessageAsync(peer, $"Чат {chatTitle} (ID: {chatId}) добавлен для парсинга.");
                }
                catch (Exception ex)
                {
                    await _client.SendMessageAsync(peer, $"Ошибка: {ex.Message}. Убедитесь, что ваш аккаунт состоит в чате.");
                }
            }
            else if (text.StartsWith("/addfilter"))
            {
                var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2)
                {
                    await _client.SendMessageAsync(peer, "Укажите ключевое слово: /addfilter <keyword>");
                    return;
                }

                var keyword = parts[1];
                await _dbService.AddFilterKeywordAsync(keyword);
                await _client.SendMessageAsync(peer, $"Ключевое слово '{keyword}' добавлено.");
            }
            else
            {
                await _client.SendMessageAsync(peer, "Команды: /addchat <chat_id>, /addfilter <keyword>");
            }
        }

        private static async Task ProcessMessageAsync(Message message, string chatTitle)
        {
            await System.IO.File.AppendAllTextAsync("messages.txt",
                $"{DateTime.Now}: {chatTitle} (ID: {message.peer_id.ID}): {message.message}\n");
        }
    }
}
