﻿using System.Linq;

using PrisonerExchange.Services;
using PrisonerExchange.Services.Chat;
using PrisonerExchange.Utility;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

internal class AdminCommands
{
	/// <summary>
	/// Removes a specific user's active sale or swap request.
	/// </summary>
	[Command("pe remove", description: "Remove a specific user’s pending prisoner sale or swap.", adminOnly: true)]
	public void RemoveExchange(ChatCommandContext ctx, string username)
	{
		var user = UserUtil.GetUserByCharacterName(username);
		if (user == null)
		{
			ctx.Reply($"{Markup.Prefix}No user found: {Markup.Highlight(username)}.");
			return;
		}

		var sale = SalesService.GetAll().FirstOrDefault(s =>
				s.Seller.PlatformId == user.PlatformId || s.Buyer.PlatformId == user.PlatformId);

		if (sale != null)
		{
			BuffUtil.RemoveBuff(sale.PrisonerEntity, BuffUtil._electricBuff);

			SalesService.RemoveSale(sale.Seller);
			ctx.Reply($"{Markup.Prefix}Removed sale request involving {user.CharacterName}.");
			return;
		}

		var swap = SwapService.GetAll().FirstOrDefault(s =>
				s.Seller.PlatformId == user.PlatformId || s.Buyer.PlatformId == user.PlatformId);

		if (swap != null)
		{
			BuffUtil.RemoveBuff(swap.PrisonerA.PrisonerEntity, BuffUtil._electricBuff);
			BuffUtil.RemoveBuff(swap.PrisonerB.PrisonerEntity, BuffUtil._electricBuff);

			SwapService.RemoveSwap(swap.Seller);
			ctx.Reply($"{Markup.Prefix}Removed swap request involving {user.CharacterName}.");
			return;
		}

		ctx.Reply($"{Markup.Prefix}No active sale or swap found for user: {Markup.Highlight(username)}.");
	}

	/// <summary>
	/// Clear all active sales and swaps.
	/// </summary>
	[Command("pe clear", description: "Clear all active prisoner exchanges (both sales and swaps).", adminOnly: true)]
	public void ClearExchanges(ChatCommandContext ctx)
	{
		var allSales = SalesService.GetAll();
		foreach (var sale in allSales)
		{
			BuffUtil.RemoveBuff(sale.PrisonerEntity, BuffUtil._electricBuff);
		}
		SalesService.ClearAll();

		var allSwaps = SwapService.GetAll();
		foreach (var swap in allSwaps)
		{
			BuffUtil.RemoveBuff(swap.PrisonerA.PrisonerEntity, BuffUtil._electricBuff);
			BuffUtil.RemoveBuff(swap.PrisonerB.PrisonerEntity, BuffUtil._electricBuff);
		}
		SwapService.ClearAll();

		ctx.Reply($"{Markup.Prefix}Cleared all active prisoner sales and swaps!");
	}

	/// <summary>
	/// Clear all active cooldowns on specific user
	/// </summary>
	[Command("pe removecooldown", "Removes command cooldowns for a user")]
	public static void RemoveCooldownCommand(ChatCommandContext ctx, string username)
	{
		var targetUser = UserUtil.GetUserByCharacterName(username);
		if (targetUser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not find user {username}.");
			return;
		}

		ctx.Reply($"{Markup.Prefix}Removed all cooldowns for user {username}.");
		CooldownTracker.ClearAllCooldowns(targetUser.PlatformId);
	}
}