using System.Linq;

using PrisonerExchange.Services;
using PrisonerExchange.Utility;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

internal class AdminCommands
{
	/// <summary>
	/// Removes a specific user's active sale or swap request.
	/// </summary>
	[Command("pe remove", description: "Remove a specific user’s active prisoner sale or swap.", adminOnly: true)]
	public void RemoveExchange(ChatCommandContext ctx, string from = "username")
	{
		var user = UserUtil.GetUserByCharacterName(from);
		if (user == null)
		{
			ctx.Reply($"{Markup.Prefix}No user found: {Markup.Highlight(from)}.");
			return;
		}

		var sale = SalesService.GetAll().FirstOrDefault(s =>
				s.Seller.PlatformId == user.PlatformId || s.Buyer.PlatformId == user.PlatformId);

		if (sale != null)
		{
			BuffUtil.RemoveBuff(sale.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);

			SalesService.RemoveSale(sale.Seller);
			ctx.Reply($"{Markup.Prefix}Removed sale request involving {user.CharacterName}.");
			return;
		}

		var swap = SwapService.GetAll().FirstOrDefault(s =>
				s.Seller.PlatformId == user.PlatformId || s.Buyer.PlatformId == user.PlatformId);

		if (swap != null)
		{
			// Remove the electric buff from both prisoners
			BuffUtil.RemoveBuff(swap.PrisonerA.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);
			BuffUtil.RemoveBuff(swap.PrisonerB.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);

			// Remove the swap
			SwapService.RemoveSwap(swap.Seller);
			ctx.Reply($"{Markup.Prefix}Removed swap request involving {user.CharacterName}.");
			return;
		}

		ctx.Reply($"{Markup.Prefix}No active sale or swap found for user: {Markup.Highlight(from)}.");
	}

	/// <summary>
	/// Clears all active sales and swaps.
	/// </summary>
	[Command("pe clear", description: "Clear all active prisoner exchanges (both sales and swaps).", adminOnly: true)]
	public void ClearExchanges(ChatCommandContext ctx)
	{
		SalesService.ClearAll();
		SwapService.ClearAll();

		ctx.Reply($"{Markup.Prefix}Cleared all active prisoner sales and swaps!");
	}
}