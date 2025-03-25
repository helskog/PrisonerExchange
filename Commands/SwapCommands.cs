using System.Collections.Generic;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Services;
using PrisonerExchange.Utility;
using PrisonerExchange.Utility.Chat;

using ProjectM;

using Unity.Entities;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

public class SwapCommands
{
	/// <summary>
	/// Initiate a swap request to another user.
	/// </summary>
	[Command("pe swap", description: "Swap one prisoner for another.", adminOnly: true)]
	public void RequestSwap(ChatCommandContext ctx, string username)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);
		UserModel targetuser = UserUtil.GetUserByCharacterName(username);

		if (PromptManager.IsWaiting(localuser.PlatformId))
			return;

		//if (targetuser == null || !targetuser.User.IsConnected)
		//{
		//	ctx.Reply($"{Markup.Prefix}User is invalid or offline.");
		//}

		List<PrisonerModel> prisonerList = PrisonerService.GetPrisonerList(targetuser);

		// Send formatted message to initiator including prisoners
		StringBuilders.SendPrisonerList(ctx, prisonerList);

		// Prompt to select prisoner index they want
		PromptHelper.UserInput(ctx, id =>
		{
			if (!int.TryParse(id, out var selection))
			{
				ctx.Reply("Something went wrong with the selection, please try again.");
				Plugin.Logger.Error("SwapService", "Could not convert string to int.");
				return;
			}

			if (selection < 0 || selection > prisonerList.Count + 1)
			{
				ctx.Reply("Invalid selection.");
				return;
			}

			PrisonerModel prisonerA = prisonerList[selection - 1]; // The selected prisoner

			Entity prisonCellEntity = EntityUtil.FindClosestInRadius<PrisonCell>(localuser.Entity, 3);

			// Need to fix this, maybe link cell -> heart -> clan/user instead of teamid
			if (!localuser.Entity.SameTeam(prisonCellEntity))
			{
				ctx.Reply($"{Markup.Prefix}You cannot swap another clans prisoner!");
				return;
			}

			if (prisonCellEntity == Entity.Null)
			{
				ctx.Reply($"{Markup.Prefix}No suitable prison cell within range.");
				return;
			}

			if (!Core.EntityManager.TryGetComponentData<PrisonCell>(prisonCellEntity, out var prisonCellData))
			{
				Plugin.Logger.Error("SwapCommands", "Could not retrieve prison cell data.");
				ctx.Reply("Could not retrieve prison cell data.");
				return;
			}

			if (!prisonCellEntity.HasPrisoner())
			{
				ctx.Reply($"{Markup.Prefix}Your prison cell does not have a prisoner to swap!");
				return;
			}

			// Prisoner from nearby cell
			PrisonerModel prisonerB = new PrisonerModel(prisonCellData.ImprisonedEntity._Entity);

			// Add new swap entry
			PendingSwap newSwap = new(
				seller: localuser,
				buyer: targetuser,
				prisonera: prisonerA,
				prisonerb: prisonerB,
				lifetimeSeconds: Configuration.ExpireExchangeAfter
			);

			SwapService.AddSwap(newSwap);

			// Buff active request NPCs to visualize.
			BuffUtil.BuffNPC(prisonerA.PrisonerEntity, targetuser.Entity, BuffUtil.ELECTRIC_BUFF, 120);
			BuffUtil.BuffNPC(prisonerB.PrisonerEntity, localuser.Entity, BuffUtil.ELECTRIC_BUFF, 120);

			// Send message to target user asking to confirm swap
			string msg = StringBuilders.SwapInfoMessage(newSwap);
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, targetuser.User, msg);

			// Inform sender that the offer has been sent.
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, localuser.User, $"{Markup.Prefix}Request sent to user {targetuser.CharacterName}.");
		}, null);
	}

	/// <summary>
	/// Accept swap request
	/// </summary>
	[Command("pe swap accept", description: "Accept incoming prisoner swap request")]
	public static void AcceptSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap decline", description: "Decline incoming prisoner swap request")]
	public static void DeclineSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap cancel", description: "Cancel your outgoing prisoner swap request")]
	public static void CancelSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap list", description: "List all active prisoner swaps", adminOnly: true)]
	public void ListSwaps(ChatCommandContext ctx)
	{
	}

	[Command("pe swap remove", description: "Remove a specific user’s swap request", adminOnly: true)]
	public void RemoveSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap clear", description: "Clear all active prisoner swaps", adminOnly: true)]
	public void ClearSwaps(ChatCommandContext ctx)
	{
	}
}