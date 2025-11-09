using System.Text.Json;
using TL;
using WTelegram;

namespace TelegramClientParser
{
    class Program
    {
        static Client client;
        static Config config;
        static HashSet<long> allowedUserIds = new HashSet<long>();
        static Dictionary<string, long> usernameToId = new Dictionary<string, long>();
        static Random random = new Random();
        static readonly object lockObj = new object();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Запуск Telegram UserBot...");

            // Загрузка конфига
            config = LoadConfig();

            // Загрузка фильтра
            LoadAllowedUsers();

            // Инициализация клиента
            client = new Client(ConfigCallback);
            client.FloodRetryThreshold = 5; // Авто-обход FloodWait
            client.OnUpdates += OnUpdate;

            try
            {
                var me = await client.LoginUserIfNeeded();
                Console.WriteLine($"Успешно вошёл как: {me.username ?? me.first_name} ({me.id})");

                // Сохраняем свой ID
                var myId = me.id;

                // Бесконечный цикл
                await Task.Delay(-1);
            }
            catch (Exception ex)
            {
                Log($"Ошибка входа: {ex.Message}", "ERROR");
            }
        }

        static async Task OnUpdate(UpdatesBase updates)
        {
            if (updates is not Updates update) return;

            foreach (var upd in update.UpdateList)
            {
                if (upd is not UpdateNewMessage newMsg) continue;
                if (newMsg.message is not Message msg) continue;
                if (msg.peer_id is not PeerUser peerUser) continue; // Только личные сообщения

                long senderId = peerUser.user_id;
                if (senderId == client.UserId) continue; // Игнорируем свои сообщения

                // Проверка в фильтре
                if (!allowedUserIds.Contains(senderId) && !await ResolveUsername(senderId, msg))
                    continue;

                // Формируем уведомление
                string senderName = "Unknown";
                try
                {
                    var user = await client.Users_GetUsers(new InputPeerUser(senderId, 0));
                    senderName = user[0].ToString().Split('\n')[0];
                }
                catch { }

                string text = $"Новое сообщение от {senderName} (@{senderId}):\n\n{msg.message}";

                // Задержка перед отправкой
                await RandomDelay();

                try
                {
                    await client.SendMessageAsync(PEER, text); // Указываете необходимый InputPeer
                    Log($"Уведомление отправлено: {senderName}");
                }
                catch (RpcException ex) when (ex.Message.Contains("FLOOD_WAIT"))
                {
                    Log($"FloodWait: {ex.Message}. Ждём...", "WARN");
                    await Task.Delay(ex.X * 1000 + 5000);
                }
                catch (Exception ex)
                {
                    Log($"Ошибка отправки: {ex.Message}", "ERROR");
                }
            }
        }

        static async Task<bool> ResolveUsername(long senderId, Message msg)
        {
            if (msg.from_id is not PeerUser fromPeer) return false;

            var user = await client.Users_GetUsers(new InputPeerUser(senderId, 0));
            var username = user[0].MainUsername;
            if (string.IsNullOrEmpty(username)) return false;

            username = username.ToLower();
            if (usernameToId.ContainsKey(username))
            {
                allowedUserIds.Add(senderId);
                return true;
            }
            return false;
        }

        static async Task RandomDelay()
        {
            int delay = random.Next(config.min_delay_ms, config.max_delay_ms + 1);
            await Task.Delay(delay);
        }

        static void LoadAllowedUsers()
        {
            lock (lockObj)
            {
                allowedUserIds.Clear();
                usernameToId.Clear();

                if (!File.Exists("allowed_users.txt"))
                {
                    File.WriteAllText("allowed_users.txt", "# Введите ID или @username\n");
                    Log("Создан allowed_users.txt — добавьте пользователей");
                    return;
                }

                var lines = File.ReadAllLines("allowed_users.txt");
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                    if (trimmed.StartsWith("@"))
                    {
                        var username = trimmed.Substring(1).ToLower();
                        usernameToId[username] = 0; // Заглушка
                    }
                    else if (long.TryParse(trimmed, out long id))
                    {
                        allowedUserIds.Add(id);
                    }
                }
                Log($"Загружено {allowedUserIds.Count} ID и {usernameToId.Count} username из фильтра");
            }
        }
        static string ConfigCallback(string what)
        {
            return what switch
            {
            "api_id" => config.api_id.ToString(),
            "api_hash" => config.api_hash,
            "phone_number" => config.phone_number,
            "verification_code" => Console.ReadLine(),
            "password" => Console.ReadLine(),
                _ => null
            };
        }

        static Config LoadConfig()
        {
            if (!File.Exists("config.json"))
            {
                var defaultConfig = new Config
                {
                    api_id = 0,
                    api_hash = "your_api_hash",
                    phone_number = "+79123456789",
                    notification_chat = "me"
                };
                File.WriteAllText("config.json", JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                Log("Создан config.json — заполните API ID и Hash на my.telegram.org");
                Environment.Exit(0);
            }

            var json = File.ReadAllText("config.json");
            return JsonSerializer.Deserialize<Config>(json);
        }

        static void Log(string message, string level = "INFO")
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            Console.WriteLine($"[{timestamp}] [{level}] {message}");
        }
    }

    class Config
    {
        public int api_id { get; set; }
        public string api_hash { get; set; }
        public string phone_number { get; set; }
        public string notification_chat { get; set; } = "me";
        public int min_delay_ms { get; set; } = 2000;
        public int max_delay_ms { get; set; } = 5000;
        public string log_level { get; set; } = "Info";
    }
}