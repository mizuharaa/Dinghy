using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using Dinghy.Blackjack;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;

namespace Dinghy.BotCommands
{
    public class StatTrack : BaseCommandModule
    {
        private static List<string> mathFacts = null;
        private static List<string> funFacts = null;
        private static readonly Random Rand = new Random();
        public static readonly EconManager econ = new EconManager();
        private void EnsureFactsLoaded()
        {
            if (mathFacts == null)
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "math_facts.txt");
                if (File.Exists(filePath))
                    mathFacts = new List<string>(File.ReadAllLines(filePath));
                else
                    mathFacts = new List<string> { "Oops! No facts found. Make sure 'math_facts.txt' exists!" };
            }
        }
        private void EnsureFunFactsLoaded()
        {
            if (funFacts == null)
            {
                string filefPath = Path.Combine(AppContext.BaseDirectory, "ff.txt");
                if (File.Exists(filefPath))
                    funFacts = new List<string>(File.ReadAllLines(filefPath));
                else
                    funFacts = new List<string> { "Oops! No facts found. Make sure 'ff.txt' exists!" };
            }
        }

        [Command("mathfact")]
        public async Task SendQuote(CommandContext ctx)
        {
            EnsureFactsLoaded();
            int index = Rand.Next(mathFacts.Count);
            await ctx.Channel.SendMessageAsync(mathFacts[index]);
        }
        [Command("funfact")]
        public async Task FunFact(CommandContext ctx)
        {
            EnsureFunFactsLoaded();
            int index_1 = Rand.Next(funFacts.Count);
            await ctx.Channel.SendMessageAsync(funFacts[index_1]);
        }

        [Command("hi")]
        public async Task SayHi(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"wsp {ctx.User.Username} 💔");
        }

        [Command("8ball")]
        public async Task EightBall(CommandContext ctx)
        {
            string[] responses = {
        "Yes", "No", "Maybe", "Ask again later", "Definitely", "Absolutely not", "Go for it", "I wouldn't if I were you"
    };

            // Send a prompt asking for a question
            var prompt = await ctx.Channel.SendMessageAsync("🎱 What would you like to ask? You have 20 seconds...");

            var interactivity = ctx.Client.GetInteractivity();

            // Wait for the user's response or for the timeout to occur
            var userResponseTask = interactivity.WaitForMessageAsync(
                m => m.Author.Id == ctx.User.Id && m.ChannelId == ctx.Channel.Id,
                TimeSpan.FromSeconds(20)
            );

            // Add a countdown warning at 15 seconds
            var delayWarningTask = Task.Delay(TimeSpan.FromSeconds(15));

            // Wait for the first task (either user response or warning)
            var completedTask = await Task.WhenAny(userResponseTask, delayWarningTask);

            if (completedTask == delayWarningTask)
            {
                // Send warning at 15 seconds if the user hasn't responded
                await ctx.Channel.SendMessageAsync("⏳ 5 seconds left to ask your question...");
            }

            // Now wait for the user response (either they respond within 20 seconds or it times out)
            var response = await userResponseTask;

            if (response.TimedOut)
            {
                // If the user took too long, notify them and exit
                await ctx.Channel.SendMessageAsync("⌛ You took too long! Ask me again later.");
                return; // No further action will happen here
            }

            // If the user responded within the time limit, pick a random answer
            int index = new Random().Next(responses.Length);
            await ctx.Channel.SendMessageAsync($"🎱 {responses[index]}");
        }
        [Command("commands")]
        public async Task ShowCmds(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder
            {
                Title = "📜 Dinghy Bot Command List",
                Description = "Here are all the commands you can use with this bot:",
                Color = DiscordColor.Goldenrod
            };

            embed.AddField("📚 Fun & Utility",
                "`!mathfact` - Get a random math fact\n" +
                "`!funfact` - Get a fun general fact\n" +
                "`!hi` - Say hi in a cool way\n" +
                "`!8ball` - Ask the magic 8-ball a question\n" +
                "`!erm` - Nerd check");

            embed.AddField("💰 Economy ",
                "`!daily` - Claim your daily DCoins\n" +
                "`!balance` - Check your DCoin balance\n" +
                "`!give @user <amount>` - Transfer DCoins\n" +
                "`!bless @user <amount>` - (Owner only) Give free DCoins");

            embed.AddField("🃏 Games & WIP",
                "`!bj <amount>` / `!blackjack <amount>` - Play Blackjack\n" +
                "`!roll` - Aura roll (WIP)");

            embed.AddField("🔧 Developer",
                "`!embedtest` - Embed test message");

            embed.WithFooter("Use prefix ! for all commands ");
            embed.WithTimestamp(DateTime.UtcNow);

            await ctx.Channel.SendMessageAsync(embed: embed);
        }
        [Command("embedtest")]
        public async Task EmbedMessege(CommandContext ctx)
        {
            var messege = new DiscordEmbedBuilder
            {
                Title = "Test1",
                Description = "EmbedTesting",
                Color = DiscordColor.Red
            };
            await ctx.Channel.SendMessageAsync(embed: messege);
        }
        public static Dictionary<ulong, (BlackjackGame game, long bet, DateTime lastAction, DiscordMessage gameMessage)> activeGames = new Dictionary<ulong, (BlackjackGame game, long bet, DateTime lastAction, DiscordMessage gameMessage)> { };

        [Command("bj")]
        [Aliases("blackjack")]
        public async Task Blackjack(CommandContext ctx, string amountArg = null)
        {
            ulong userId = ctx.User.Id;
            var balance = econ.GetBalance(userId);

            if (amountArg == null)
            {
                await ctx.RespondAsync("❓ Please specify an amount to bet or use `!bj allin`.");
                return;
            }

            long bet;
            if (amountArg.ToLower() == "allin")
            {
                bet = balance;
            }
            else if (!long.TryParse(amountArg, out bet) || bet <= 0)
            {
                await ctx.RespondAsync("❌ Invalid bet amount.");
                return;
            }

            if (bet > balance)
            {
                await ctx.RespondAsync($"💸 You can't bet more than your current balance of {balance} DCoins.");
                return;
            }

            // Deduct the bet amount up front
            econ.LoadBalance(userId, -bet);

            var game = new BlackjackGame();
            game.ShuffleAndDeal();

            var embed = StatTrack.BuildBlackjackEmbed(ctx.User.Username, game.PlayerHand, game.DealerHand, true, 30);

            var msg = await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithEmbed(embed.Build())
                .AddComponents(
                    new DiscordButtonComponent(ButtonStyle.Primary, $"hit_{userId}", "🃏 Hit"),
                    new DiscordButtonComponent(ButtonStyle.Secondary, $"stand_{userId}", "🛑 Stand"),
                    new DiscordButtonComponent(ButtonStyle.Danger, $"forfeit_{userId}", "🏳️ Forfeit")
                ));

            // If the game ends immediately (Blackjack, bust, etc.), resolve and skip the loop
            if (game.IsFinished)
            {
                await ResolveGame(ctx, userId, game, bet, msg);
                return;
            }

            activeGames[userId] = (game, bet, DateTime.UtcNow, msg);
            _ = Task.Run(async () => await BlackjackTimeoutHandler(ctx, userId));
        }

        private async Task BlackjackTimeoutHandler(CommandContext ctx, ulong userId)
        {
            while (activeGames.ContainsKey(userId))
            {
                var (game, bet, lastAction, msg) = activeGames[userId];
                int timeLeft = 30 - (int)(DateTime.UtcNow - lastAction).TotalSeconds;

                if (timeLeft <= 0 || game.IsFinished)
                {
                    activeGames.Remove(userId);
                    string resultText = game.IsFinished
                        ? $"Game ended: **{game.Result}**\nYour hand: {string.Join(" ", game.PlayerHand)}\nDealer's hand: {string.Join(" ", game.DealerHand)}"
                        : $"You took too long to respond. You lost your bet of {bet} DCoins.";

                    var finalEmbed = new DiscordEmbedBuilder()
                    {
                        Title = game.IsFinished ? "🃏 Blackjack - Final Result" : "🕒 Blackjack Timeout",
                        Description = resultText,
                        Color = game.IsFinished ? DiscordColor.Gold : DiscordColor.DarkRed
                    };

                    await msg.ModifyAsync(new DiscordMessageBuilder()
                        .WithEmbed(finalEmbed.Build()).WithContent(string.Empty).AddComponents()
);
                    return;
                }

                var updatedEmbed = StatTrack.BuildBlackjackEmbed(ctx.User.Username, game.PlayerHand, game.DealerHand, true, timeLeft);
                await msg.ModifyAsync(embed: updatedEmbed.Build());
                int delay = Math.Max(0, 1000 - (int)(DateTime.UtcNow - lastAction).TotalMilliseconds % 1000);
                await Task.Delay(delay);
            }
        }

        private async Task ResolveGame(CommandContext ctx, ulong userId, BlackjackGame game, long bet, DiscordMessage msg)
        {
            activeGames.Remove(userId);

            string outcome = game.Result.ToString();
            string payoutMessage = "You lost your bet.";

            if (game.Result == GameResult.PlayerWin)
            {
                long payout = bet * 2;
                econ.LoadBalance(userId, payout);
                payoutMessage = $"You win! You earned **{payout} DCoins**.";
            }
            else if (game.Result == GameResult.BlackJack)
            {
                long payout = bet * 3;
                econ.LoadBalance(userId, payout);
                payoutMessage = $"**Blackjack!** You earned **{payout} DCoins**.";
            }
            else if (game.Result == GameResult.DoubleAce)
            {
                long payout = bet * 4;
                econ.LoadBalance(userId, payout);
                payoutMessage = $"💥 **Double Ace Jackpot!** You earned **{payout} DCoins**!";
            }

            var resultEmbed = new DiscordEmbedBuilder()
            {
                Title = "🃏 Blackjack - Final Result",
                Description = $"Result: **{outcome}**\n{payoutMessage}\n\n**Your hand:** {string.Join(" ", game.PlayerHand)}\n**Dealer's hand:** {string.Join(" ", game.DealerHand)}",
                Color = DiscordColor.Gold
            };

            await msg.ModifyAsync(new DiscordMessageBuilder().WithEmbed(resultEmbed.Build()).WithContent(string.Empty).AddComponents()
);
        }

        public static DiscordEmbedBuilder BuildBlackjackEmbed(string username, List<string> playerHand, List<string> dealerHand, bool hideDealerSecondCard, int timeLeft)
        {
            string playerDisplay = string.Join(" ", playerHand);
            string dealerDisplay;

            if (hideDealerSecondCard)
            {
                if (dealerHand.Count == 0)
                    dealerDisplay = "❓";
                else if (dealerHand.Count == 1)
                    dealerDisplay = $"{dealerHand[0]} ❓";
                else
                    dealerDisplay = $"{dealerHand[0]} ❓"; // Always hide extra cards
            }
            else
            {
                dealerDisplay = string.Join(" ", dealerHand);
            }


            var embed = new DiscordEmbedBuilder()
            {
                Title = $"🃏 Blackjack - {username}'s Game",
                Description = $"**Your hand:** {playerDisplay}\n**Dealer's hand:** {dealerDisplay}\n⏳ Time left: **{timeLeft}s**",
                Color = DiscordColor.Green
            };

            return embed;
        }

        [Command("daily")]
        public async Task Daily(CommandContext ctx)
        {
            ulong userId = ctx.User.Id;

            if (!econ.CanClaimDaily(userId, out TimeSpan remaining))
            {
                await ctx.RespondAsync($"⏳ You have already claimed your daily. Try again in **{remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s**.");
                return;
            }
            string message;
            Random rand = new Random();
            long reward;

            double roll = rand.NextDouble() * 100;
            if (roll <= 0.1)
            {
                reward = 1_000_000;
                message = "💸 **Millionaire Roll!** You just hit the 0.1% chance and got **1,000,000 DCoins**!";
            }
            else if (roll <= 2.1) // +2% Jackpot
            {
                reward = 777_777;
                message = "🎉 **JACKPOT!** You won **777,777 DCoins**!";
            }
            else if (roll <= 12.1) // +10% Devil
            {
                reward = 66_666;
                message = "😈 **Devil's Luck!** Here's **66,666 DCoins** from the underworld.";
            }
            else if (roll <= 19.0) // +6.9% Meme
            {
                reward = 69_696;
                message = "😂 **sechsech Money!** You got exactly **69,696 DCoins**. Nice.";
            }
            else // Normal roll
            {
                reward = rand.Next(5_000, 50_001);
                message = $"💰 You opened a regular chest and found **{reward} DCoins**.";
            }

            econ.LoadBalance(ctx.User.Id, reward);
            long newBalance = econ.GetBalance(ctx.User.Id);

            var embed = new DiscordEmbedBuilder
            {
                Title = "🪙 Daily DCoin Chest",
                Description = $"{message}\n\nYour new balance: **{newBalance} DCoins**.",
                Color = DiscordColor.Gold,
                Timestamp = DateTime.UtcNow
            };

            await ctx.Channel.SendMessageAsync(embed: embed);

            //Set cooldown 
            econ.UpdateLastClaim(ctx.User.Id);
        }
        [Command("balance")]
        public async Task CheckBalance(CommandContext ctx)
        {
            long balance = econ.GetBalance(ctx.User.Id);
            await ctx.Channel.SendMessageAsync($"🧾 {ctx.User.Username}, you have **{balance} DCoins**.");
        }
        [Command("erm")]
        public async Task Nerd(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync($"erm awktually, {ctx.User.Username} is a big nerd 🤓☝️");
        }
        [Command("give")]
        public async Task Transaction(CommandContext ctx, DiscordUser recipient, long amount)
        {
            ulong SenderID = ctx.User.Id;
            ulong recipientID = recipient.Id;

            // Basic validations
            if (SenderID == recipientID)
            {
                await ctx.RespondAsync("❌ You can't send DCoins to yourself.");
                return;
            }

            if (recipient.IsBot)
            {
                await ctx.RespondAsync("🤖 You can't send DCoins to a bot.");
                return;
            }

            if (amount <= 0)
            {
                await ctx.RespondAsync("❌ Amount must be greater than zero.");
                return;
            }

            long senderBalance = econ.GetBalance(SenderID);
            if (senderBalance < amount)
            {
                await ctx.RespondAsync($"ur broke vro, ur current balance is: {senderBalance}");
                return;
            }

            // Confirmation message with buttons
            var embed = new DiscordEmbedBuilder
            {
                Title = "DCoins Transfer Request",
                Description = $"{recipient.Mention}, do you accept **{amount} DCoins** from {ctx.User.Mention}?",
                Color = DiscordColor.Blurple
            };

            var msg = await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithEmbed(embed)
                .AddComponents(
                    new DiscordButtonComponent(ButtonStyle.Success, "accept_btn", "✅ Accept"),
                    new DiscordButtonComponent(ButtonStyle.Danger, "decline_btn", "❌ Decline")
                )
            );

            var interactivity = ctx.Client.GetInteractivity();
            var response = await interactivity.WaitForButtonAsync(msg, recipient, TimeSpan.FromSeconds(30));

            if (response.TimedOut)
            {
                await msg.DeleteAsync();
                await ctx.Channel.SendMessageAsync("⏰ Transfer request timed out.");
                return;
            }

            if (response.Result.Id == "accept_btn")
            {
                econ.LoadBalance(SenderID, -amount);
                econ.LoadBalance(recipientID, amount);

                await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"✅ Transfer complete! {ctx.User.Mention} sent **{amount} DCoins** to {recipient.Mention}.")
                );
            }
            else if (response.Result.Id == "decline_btn")
            {
                await response.Result.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("❌ Transfer declined by the recipient.")
                );
            }
        }
        [Command("roll")]
        public async Task randomRoll(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("WIP");
        }
        [Command("bless")]
        public async Task BlessUser(CommandContext ctx, DiscordUser recipient, long amount)
        {
            const ulong OWNER_ID = 453088753481678849; // ownder

            if (ctx.User.Id != OWNER_ID)
            {
                await ctx.RespondAsync("❌ You are not allowed to use this command.");
                return;
            }

            if (amount <= 0)
            {
                await ctx.RespondAsync("⚠️ Amount must be greater than zero.");
                return;
            }

            if (recipient.IsBot)
            {
                await ctx.RespondAsync("🤖 Cannot bless bots.");
                return;
            }

            econ.LoadBalance(recipient.Id, amount);

            await ctx.Channel.SendMessageAsync($"✨ {ctx.User.Mention} has blessed {recipient.Mention} with **{amount} DCoins**.");
        }
        public static async Task ResolveGameFromInteraction(ComponentInteractionCreateEventArgs e,BlackjackGame game,long bet,DiscordMessage msg,EconManager econ)
        {
            string outcome = game.Result.ToString();
            string payoutMessage = "Haha you lost your bet bro";
            ulong userId = e.User.Id;

            switch (game.Result)
            {
                case GameResult.PlayerWin:
                    {
                        long payout = bet * 2;
                        econ.LoadBalance(userId, payout);
                        payoutMessage = $"🎉 You win! You earned **{payout} DCoins**.";
                        break;
                    }
                case GameResult.BlackJack:
                    {
                        long payout = bet * 3;
                        econ.LoadBalance(userId, payout);
                        payoutMessage = $"🃏 **Blackjack!** You earned **{payout} DCoins**.";
                        break;
                    }
                case GameResult.DoubleAce:
                    {
                        long payout = bet * 4;
                        econ.LoadBalance(userId, payout);
                        payoutMessage = $"💥 **Double Ace Jackpot!** You earned **{payout} DCoins**!";
                        break;
                    }
                case GameResult.Tie:
                    {
                        econ.LoadBalance(userId, bet); // Refund the original bet
                        payoutMessage = $"🤝 It's a tie! Your bet of **{bet} DCoins** has been returned.";
                        break;
                    }
                case GameResult.Forfeit:
                    {
                        payoutMessage = $"🏳️ You forfeited. Your bet of **{bet} DCoins** is gone.";
                        break;
                    }
                case GameResult.DealerWin:
                    {
                        payoutMessage = $"💀 You lost to the dealer. Better luck next time.";
                        break;
                    }
                default:
                    {
                        payoutMessage = $"⚠️ Unexpected result: {outcome}. No payout processed.";
                        break;
                    }
            }
            var resultEmbed = new DiscordEmbedBuilder()
            {
                Title = "🃏 Blackjack - Final Result",
                Description = $"Result: **{outcome}**\n{payoutMessage}\n\n**Your hand:** {string.Join(" ", game.PlayerHand)}\n**Dealer's hand:** {string.Join(" ", game.DealerHand)}",
                Color = DiscordColor.Gold
            };

            var builder = new DiscordMessageBuilder()
                .WithEmbed(resultEmbed.Build())
                .WithContent("");

            builder.ClearComponents();

            await msg.ModifyAsync(builder);
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
        }

    }
}
