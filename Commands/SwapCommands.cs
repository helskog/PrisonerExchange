using System;
using System.Collections.Generic;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Services;
using PrisonerExchange.Services.Chat;
using PrisonerExchange.Utility;

using ProjectM;

using Unity.Entities;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

[CommandGroup("prisonerexchange", "pe")]
internal class SwapCommands
{
	/// <summary>
	/// Initiate a swap request to another user.
	/// </summary>
	[Command("swap", description: "Swap one prisoner for another.")]
	public void RequestSwap(ChatCommandContext ctx, string username)
	{
		var localuser = UserUtil.GetCurrentUser(ctx);
		var targetuser = UserUtil.GetUserByCharacterName(username);

		if (SwapService.SwapExists(localuser) || SwapService.SwapExists(targetuser))
		{
			ctx.Reply($"{Markup.Prefix}Both users must not be in an active exchange!");
			return;
		}

		if (localuser == null || targetuser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not locate one or both users.");
			return;
		}

		if (!localuser.User.IsConnected || !targetuser.User.IsConnected)
		{
			ctx.Reply($"{Markup.Prefix}Both users need to be online!");
			return;
		}

		var prisonerList = PrisonerService.GetPrisonerList(targetuser);
		if (prisonerList.Count == 0)
		{
			ctx.Reply($"{Markup.Prefix}Target user has no prisoners.");
			return;
		}

		StringBuilders.SendPrisonerList(ctx, prisonerList);

		PromptHelper.UserInput(ctx, input =>
		{
			if (!int.TryParse(input, out var selection) || selection < 1 || selection > prisonerList.Count)
			{
				ctx.Reply($"{Markup.Prefix}Invalid selection.");
				return;
			}

			var prisonerA = prisonerList[selection - 1];

			// Look for nearby prison cell to get PrisonerB
			Entity prisonCellEntity = EntityUtil.FindClosestInRadius<PrisonCell>(localuser.Entity, 3);

			if (prisonCellEntity == Entity.Null || !Core.EntityManager.TryGetComponentData<PrisonCell>(prisonCellEntity, out var prisonCellData))
			{
				ctx.Reply($"{Markup.Prefix}No valid prison cell found.");
				return;
			}

			if (!PrisonerService.HasPrisoner(prisonCellEntity))
			{
				ctx.Reply($"{Markup.Prefix}Your prison cell is empty.");
				return;
			}

			var prisonerB = new PrisonerModel(prisonCellData.ImprisonedEntity._Entity);

			var swap = new PendingSwap(localuser, targetuser, prisonerA, prisonerB, Configuration.ExpireExchangeAfter);
			SwapService.AddSwap(swap);

			BuffUtil.BuffNPC(prisonerA.PrisonerEntity, targetuser.Entity, BuffUtil.ELECTRIC_BUFF, Configuration.ExpireExchangeAfter);
			BuffUtil.BuffNPC(prisonerB.PrisonerEntity, localuser.Entity, BuffUtil.ELECTRIC_BUFF, Configuration.ExpireExchangeAfter);

			var msg = StringBuilders.SwapInfoMessage(swap);
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, targetuser.User, msg);
			ctx.Reply($"{Markup.Prefix}Swap request sent to {targetuser.CharacterName}.");
		});
	}

	/// <summary>
	/// Accept swap request
	/// </summary>
	[Command("swap accept", description: "Accept incoming prisoner swap request")]
	public static void AcceptSwap(ChatCommandContext ctx)
	{
		var localuser = UserUtil.GetCurrentUser(ctx);
		if (localuser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not determine your identity.");
			return;
		}

		var swap = SwapService.GetActiveSwap(localuser);
		if (swap == null)
		{
			ctx.Reply($"{Markup.Prefix}No swap request to accept.");
			return;
		}

		bool result = PrisonerService.SwapPrisoner(swap.PrisonerA, swap.PrisonerB, localuser);

		if (!result)
		{
			ctx.Reply($"{Markup.Prefix}Something went wrong during the swap.");
			return;
		}

		SwapService.RemoveSwap(swap.Seller);

		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, swap.Seller.User,
			$"{Markup.Prefix}{localuser.CharacterName} has accepted your prisoner swap.");

		ctx.Reply($"{Markup.Prefix}Swap complete.");
	}

	[Command("swap decline", description: "Decline incoming prisoner swap request")]
	public static void DeclineSwap(ChatCommandContext ctx)
	{
	}

	[Command("swap cancel", description: "Cancel your outgoing prisoner swap request")]
	public static void CancelSwap(ChatCommandContext ctx)
	{
	}

	[Command("swap list", description: "List all active prisoner swaps", adminOnly: true)]
	public void ListSwaps(ChatCommandContext ctx)
	{
	}

	[Command("swap remove", description: "Remove a specific user’s swap request", adminOnly: true)]
	public void RemoveSwap(ChatCommandContext ctx)
	{
	}

	[Command("swap clear", description: "Clear all active prisoner swaps", adminOnly: true)]
	public void ClearSwaps(ChatCommandContext ctx)
	{
	}
}