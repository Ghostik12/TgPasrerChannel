

using TL;

namespace TelegramClientParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            const int apiId = ;
            const string apiHash = "";
            WTelegram.Client client = new WTelegram.Client(apiId, apiHash);
            var myself = await client.LoginUserIfNeeded();
            Console.WriteLine($"We are logged-in as {myself} (id {myself.id})");
            await DoLogin("");

            async Task DoLogin(string loginInfo)
            {
                while (client.User == null)
                    switch (await client.Login(loginInfo))
                    {
                        case "verification_code":
                            Console.WriteLine("Code:");
                            loginInfo = Console.ReadLine();
                            break;
                        default:
                            loginInfo = null;
                            break;
                    }
                Console.WriteLine($"We are logged-in as {client.User} (id {client.User.id})");

                var chats = await client.Messages_GetAllChats();
                var dialogs = await client.Messages_GetAllDialogs();
                Console.WriteLine("This user has joined the following:");
                foreach (Dialog dialog in dialogs.dialogs)
                        Console.WriteLine($"{dialog.top_message,10}: {dialog.folder_id}");
                Console.Write("Type a chat ID to send a message: ");
            }
        }
    }
}