using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Services;
using PrisonerExchange.Services.Chat;
using PrisonerExchange.Utility;

using ProjectM;

using Unity.Collections;
using Unity.Entities;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

[CommandGroup("pe", "prisonerexchange")]
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

		if (targetuser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not locate target user.");
			return;
		}

		if (SwapService.SwapExists(targetuser) || SalesService.SaleExists(targetuser))
		{
			ctx.Reply($"{Markup.Prefix}Target already have a pending exchange offer!");
			return;
		}

		if (!targetuser.User.IsConnected)
		{
			ctx.Reply($"{Markup.Prefix}Target user needs to be online!");
			return;
		}

		if (!localuser.IsAdmin)
		{
			if (!Configuration.SwappingEnabled)
			{
				ctx.Reply($"{Markup.Prefix}Swapping prisoners is not enabled!");
				return;
			}

			if (CooldownTracker.IsOnCooldown(localuser.PlatformId, "swap"))
			{
				var remaining = CooldownTracker.GetRemainingSeconds(localuser.PlatformId, "swap");
				ctx.Reply($"{Markup.Prefix}You must wait another {(int)remaining} seconds before using .pe swap again.");
				return;
			}

			if (localuser.Equals(targetuser))
			{
				ctx.Reply($"{Markup.Prefix}Cannot swap a prisoner with yourself!");
				return;
			}

			if (localuser.Entity.SameTeam(targetuser.Entity))
			{
				ctx.Reply($"{Markup.Prefix}Cannot swap a prisoner with your own teammate!");
				return;
			}
		}

		var prisonerList = PrisonerService.GetPrisonerList(targetuser);
		if (prisonerList.Count == 0)
		{
			ctx.Reply($"{Markup.Prefix}{username} has no prisoners to swap.");
			return;
		}

		StringBuilders.SendPrisonerList(ctx, prisonerList);

		PromptHelper.UserInput(ctx, input =>
		{
			if (!int.TryParse(input, out var selection) || selection < 1 || selection > prisonerList.Count)
			{
				ctx.Reply($"{Markup.Prefix}Invalid selection, please select one of the prisoners in the list.");
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

			if (!prisonCellEntity.SameTeam(localuser.Entity))
			{
				ctx.Reply($"{Markup.Prefix}You cannot swap another clans prisoner!");
				return;
			}

			var prisonerB = new PrisonerModel(prisonCellData.ImprisonedEntity._Entity);

			var swap = new PendingSwap(localuser, targetuser, prisonerA, prisonerB, Configuration.ExpireExchangeAfter);
			SwapService.AddSwap(swap);

			BuffUtil.BuffNPC(prisonerA.PrisonerEntity, targetuser.Entity, BuffUtil.ELECTRIC_BUFF, Configuration.ExpireExchangeAfter);
			BuffUtil.BuffNPC(prisonerB.PrisonerEntity, localuser.Entity, BuffUtil.ELECTRIC_BUFF, Configuration.ExpireExchangeAfter);

			var msg = new FixedString512Bytes(StringBuilders.SwapInfoMessage(swap));
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, targetuser.User, ref msg);

			CooldownTracker.SetCooldown(localuser.PlatformId, "swap");

			ctx.Reply($"{Markup.Prefix}Swap request sent to {targetuser.CharacterName}.");
		});
	}

	/// <summary>
	/// Accept swap request
	/// </summary>
	[Command("acceptswap", description: "Accept incoming prisoner swap request")]
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

		var acceptedSwapMessage = new FixedString512Bytes($"{Markup.Prefix}{localuser.CharacterName} has accepted your prisoner swap.");
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, swap.Seller.User, ref acceptedSwapMessage);

		ctx.Reply($"{Markup.Prefix}Swap complete.");
	}

	[Command("declineswap", description: "Decline incoming prisoner swap request")]
	public static void DeclineSwap(ChatCommandContext ctx)
	{
		var localuser = UserUtil.GetCurrentUser(ctx);

		var swap = SwapService.GetActiveSwap(localuser);
		if (swap == null)
		{
			ctx.Reply($"{Markup.Prefix}No swap request found.");
			return;
		}

		if (swap.Buyer.PlatformId != localuser.PlatformId)
		{
			ctx.Reply($"{Markup.Prefix}You have no incoming swap request to decline.");
			return;
		}

		ctx.Reply($"{Markup.Prefix}Swap request from {swap.Seller.CharacterName} has been declined.");
		var swapDeclinedMessage = new FixedString512Bytes($"{Markup.Prefix}Your swap request was declined by {swap.Buyer.CharacterName}.");
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, swap.Seller.User, ref swapDeclinedMessage);

		BuffUtil.RemoveBuff(swap.PrisonerA.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);
		BuffUtil.RemoveBuff(swap.PrisonerB.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);

		SwapService.RemoveSwap(swap.Seller);

		Plugin.Logger.Info("SwapCommands",
				$"Swap request from '{swap.Seller.CharacterName}' declined by '{swap.Buyer.CharacterName}'.");
	}

	[Command("cancelswap", description: "Cancel your outgoing prisoner swap request")]
	public static void CancelSwap(ChatCommandContext ctx)
	{
		var localuser = UserUtil.GetCurrentUser(ctx);

		var swap = SwapService.GetActiveSwap(localuser);
		if (swap == null)
		{
			ctx.Reply($"{Markup.Prefix}No active swap request found to cancel.");
			return;
		}

		if (swap.Seller.PlatformId != localuser.PlatformId)
		{
			ctx.Reply($"{Markup.Prefix}You do not have an outgoing swap request to cancel.");
			return;
		}

		BuffUtil.RemoveBuff(swap.PrisonerA.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);
		BuffUtil.RemoveBuff(swap.PrisonerB.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);

		SwapService.RemoveSwap(swap.Seller);

		ctx.Reply($"{Markup.Prefix}Your swap request has been canceled.");
		Plugin.Logger.Info("SwapCommands", $"User '{localuser.CharacterName}' canceled their outgoing prisoner swap request.");
	}
}