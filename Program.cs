using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using Dinghy.BotCommands;
using DSharpPlus.Interactivity;
using DSharpPlus.Entities;

namespace Dinghy.Bot
{
    internal class Program
    {
        private static DiscordClient UserClient { get; set; } // Fire up the Discord client
        private static CommandsNextExtension Commands { get; set; }

        private static async Task Main(string[] args)
        {
            var jsonReader = new JSONReader(); // 
            await jsonReader.ReadJSON(); // Multi-threaded reading 

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            UserClient = new DiscordClient(discordConfig); // Apply config

            // Initialize the Interactivity extension
            UserClient.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromSeconds(30) // Set timeout
            });

            var commandConfigs = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix }, // Command prefix
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = true,
                IgnoreExtraArguments = false,
                CaseSensitive = false,
            };

            Commands = UserClient.UseCommandsNext(commandConfigs); // Apply config
            UserClient.Ready += UserClient_Ready; // Event handler
            Commands.RegisterCommands<StatTrack>(); // Register cmds
            UserClient.ComponentInteractionCreated += async (s, e) =>
            {
                if (!e.Id.StartsWith("hit_") && !e.Id.StartsWith("stand_") && !e.Id.StartsWith("forfeit_"))
                    return;

                if (!ulong.TryParse(e.Id.Split('_')[1], out var buttonUserId))
                    return;

                if (e.User.Id != buttonUserId)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("❌ You are not allowed to interact with this game."));
                    return;
                }

                if (!StatTrack.activeGames.TryGetValue(buttonUserId, out var gameState))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("⚠️ Game not found or expired."));
                    return;
                }

                var (game, bet, _, msg) = gameState;

                // ⬇️ Hit
                if (e.Id.StartsWith("hit_"))
                {
                    game.PlayerHit();
                    if (game.IsFinished)
                    {
                        StatTrack.activeGames.Remove(buttonUserId);
                        await StatTrack.ResolveGameFromInteraction(e, game, bet, msg, StatTrack.econ);
                        return;
                    }

                    StatTrack.activeGames[buttonUserId] = (game, bet, DateTime.UtcNow, msg);
                }
                // ⬇️ Stand
                else if (e.Id.StartsWith("stand_"))
                {
                    game.PlayerStand();
                    StatTrack.activeGames.Remove(buttonUserId);
                    await StatTrack.ResolveGameFromInteraction(e, game, bet, msg, StatTrack.econ);
                    return;
                }
                // ⬇️ Forfeit
                else if (e.Id.StartsWith("forfeit_"))
                {
                    game.PlayerForfeit();
                    StatTrack.activeGames.Remove(buttonUserId);
                    await StatTrack.ResolveGameFromInteraction(e, game, bet, msg, StatTrack.econ);
                    return;
                }

                // ⬇️ Ongoing game, update embed
                var updatedEmbed = StatTrack.BuildBlackjackEmbed(
                    e.User.Username,
                    game.PlayerHand,
                    game.DealerHand,
                    false,
                    0
                );
                await msg.ModifyAsync(embed: updatedEmbed.Build());
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

            };

            await UserClient.ConnectAsync(); // Connect the bot
            await Task.Delay(-1); // Keep the program running
        }

        private static Task UserClient_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            // Bot is ready, can be used for logging purposes or custom startup behavior
            return Task.CompletedTask;
        }
    }
}
