using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgPars.Services
{
    public class TelegramClientService
    {
        private readonly WTelegram.Client _client;
        private readonly MessageHandler _messageHandler;

        public TelegramClientService(string apiId, string apiHash, string phoneNumber, MessageHandler messageHandler)
        {
            _client = new WTelegram.Client(Config);
            _messageHandler = messageHandler;

            string Config(string what)
            {
                return what switch
                {
                    "api_id" => apiId,
                    "api_hash" => apiHash,
                    "phone_number" => phoneNumber,
                    "verification_code" => ReadVerificationCode(),
                    "session_pathname" => "session.dat",
                    _ => null
                };
            }
        }

        private static string ReadVerificationCode()
        {
            Console.Write("Введите код подтверждения: ");
            return Console.ReadLine();
        }

        public async Task<User> StartAsync()
        {
            var user = await _client.LoginUserIfNeeded();
            Console.WriteLine($"Авторизован как {user.username} (ID: {user.id})");

            _client.OnUpdate += _messageHandler.HandleUpdateAsync;
            await _client.RunUpdatesAsync();

            return user;
        }
    }
}
