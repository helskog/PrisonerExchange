using System.Linq;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Services;
using PrisonerExchange.Services.Chat;
using PrisonerExchange.Utility;

using ProjectM;

using Unity.Entities;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

[CommandGroup("pe", "prisonerexchange")]
internal class SaleCommands
{
	/// <summary>
	/// Make an offer to sell prisoner to another user.
	/// </summary>

	[Command("sell", description: "Sell a prisoner for a price.")]
	public static void ExchangeCommand(ChatCommandContext ctx, string username, int price)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);
		UserModel targetuser = UserUtil.GetUserByCharacterName(username);

		if (CooldownTracker.IsOnCooldown(localuser.PlatformId, "sell"))
		{
			var remaining = CooldownTracker.GetRemainingSeconds(localuser.PlatformId, "sell");
			ctx.Reply($"{Markup.Prefix}You must wait another {(int)remaining} seconds before using /swap accept again!");
			return;
		}

		if (targetuser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not find user with the name {Markup.Highlight(username)}.");
			return;
		}

		if (SalesService.GetAll().Any(s => s.Buyer == targetuser || s.Seller == targetuser))
		{
			ctx.Reply($"{Markup.Prefix}Target user already has a pending offer. They must finish the exchange before getting a new offer!");
			return;
		}

		if (SalesService.SaleExists(localuser))
		{
			ctx.Reply($"{Markup.Prefix}You already have a pending exchange offer!");
			return;
		}

		if (localuser.Equals(targetuser))
		{
			ctx.Reply($"{Markup.Prefix}Cannot sell a prisoner to yourself!");
			return;
		}

		if (localuser.Entity.SameTeam(targetuser.Entity))
		{
			ctx.Reply($"{Markup.Prefix}Cannot sell a prisoner to your own teammate!");
			return;
		}

		Entity prisonCellEntity = EntityUtil.FindClosestInRadius<PrisonCell>(localuser.Entity, 5);

		if (prisonCellEntity == Entity.Null)
		{
			ctx.Reply($"{Markup.Prefix}No suitable prison cell within range.");
			return;
		}

		if (!localuser.Entity.SameTeam(prisonCellEntity))
		{
			ctx.Reply($"{Markup.Prefix}You cannot sell another clans prisoner!");
			return;
		}

		if (Configuration.ClanLeaderOnly)
		{
			if (!localuser.IsClanLeader)
			{
				ctx.Reply($"{Markup.Prefix}Only clan leaders are allowed to sell prisoners!");
				return;
			}
		}

		if (!Core.EntityManager.TryGetComponentData<PrisonCell>(prisonCellEntity, out var prisonCellData))
		{
			Plugin.Logger.Error("UserCommands", "Could not retrieve prison cell data.");
			ctx.Reply("Could not retrieve prison cell data.");
			return;
		}

		if (!PrisonerService.HasPrisoner(prisonCellEntity))
		{
			ctx.Reply($"{Markup.Prefix}The prison cell does not have a prisoner to sell!");
			return;
		}

		Entity unitEntity = prisonCellData.ImprisonedEntity._Entity;

		PendingSale newSale = new(
			seller: localuser,
			prisonerEntity: unitEntity,
			buyer: targetuser,
			price: price,
			lifetimeSeconds: Configuration.ExpireExchangeAfter
		);

		SalesService.AddSale(newSale);

		Plugin.Logger.Info("UserCommands", $"User '{localuser.CharacterName}' initiated prisoner sale to '{targetuser.CharacterName}' for {price} {Configuration.CurrencyName}.");

		// Buff active request NPC to visualize.
		BuffUtil.BuffNPC(unitEntity, localuser.Entity, BuffUtil.ELECTRIC_BUFF, 120);

		// Send message to receiving user.
		string msg = StringBuilders.SalesInfoMessage(newSale);
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, targetuser.User, msg);

		// Inform sender that the offer has been sent.
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, localuser.User, $"{Markup.Prefix}Request sent to user {targetuser.CharacterName}");

		CooldownTracker.SetCooldown(localuser.PlatformId, "sell");
	}

	/// <summary>
	/// Cancel outgoing exchange request.
	/// </summary>

	[Command("sale cancel", description: "Cancel your outgoing exchange request.")]
	public static void CancelCommand(ChatCommandContext ctx)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);

		var sale = SalesService.GetSaleBySeller(localuser);

		if (sale == null)
		{
			ctx.Reply($"{Markup.Prefix}No active exchange request found.");
			return;
		}

		if (!BuffUtil.RemoveBuff(sale.PrisonerEntity, BuffUtil.ELECTRIC_BUFF))
			Plugin.Logger.Error("UserCommands", $"Failed to apply buff to {sale.PrisonerEntity}.");

		SalesService.RemoveSale(localuser);
		ctx.Reply($"{Markup.Prefix}Your exchange request has been canceled.");

		Plugin.Logger.Info("UserCommands", $"User '{localuser.CharacterName}' cancelled their prisoner sale.");
	}

	/// <summary>
	/// Accept incoming prisoner exchange request.
	/// </summary>

	[Command("sale accept", description: "Accept incoming prisoner exchange request.")]
	public static void AcceptExchange(ChatCommandContext ctx)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);
		var sale = SalesService.GetSaleByBuyer(localuser);

		if (sale == null)
		{
			ctx.Reply($"{Markup.Prefix}There is no exchange request to accept.");
			return;
		}

		UserModel seller = sale.Seller;
		PrisonerModel prisoner = new PrisonerModel(sale.PrisonerEntity);
		int price = sale.Price;

		if (seller == null || !seller.User.IsConnected)
		{
			ctx.Reply($"{Markup.Prefix}Seller cannot be found or is offline.");
			return;
		}

		Entity prisonCellEntity = EntityUtil.FindClosestInRadius<PrisonCell>(localuser.Entity, 3);

		if (prisonCellEntity == Entity.Null)
		{
			ctx.Reply($"{Markup.Prefix}No suitable prison cell within range.");
			return;
		}

		if (PrisonerService.HasPrisoner(prisonCellEntity))
		{
			ctx.Reply($"{Markup.Prefix}The prison cell is not empty!");
			return;
		}

		if (!InventoryService.CanAfford(localuser, price))
		{
			ctx.Reply($"{Markup.Prefix}You cannot afford to buy this prisoner.");
			return;
		}

		if (!InventoryService.RemoveCurrencyFromInventory(localuser, price))
		{
			Plugin.Logger.Error("UserCommands", $"Could not retrieve '{price}' '{Configuration.CurrencyName}' from the inventory of '{localuser.CharacterName}'");
			ctx.Reply($"Something went wrong with the transaction, please contact an administrator.");
			return;
		}

		// Handle prisoner transfer
		PrisonerService.MovePrisoner(prisoner, prisonCellEntity, localuser);

		if (!InventoryService.AddCurrencyToInventory(seller, price))
		{
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, seller.User, $"Something went wrong when paying out {price} {Configuration.CurrencyName} contact an administrator!");
		}

		// Complete transaction
		SalesService.RemoveSale(seller);
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, seller.User, $"{localuser.CharacterName} has purchased your prisoner for {price} {Configuration.CurrencyName}!");

		Plugin.Logger.Info("UserCommands", $"Prisoner sale accepted : Buyer='{localuser.CharacterName}', Seller='{seller.CharacterName}', Price={price}.");

		if (Configuration.AnnounceExchange)
		{
			StringBuilders.AnnounceSale(sale);
		}

		ctx.Reply($"{Markup.Prefix}Completed exchange for prisoner!");
	}

	/// <summary>
	/// Decline incoming prisoner exchange.
	/// </summary>
	[Command("sale decline", description: "Decline incoming prisoner exchange request.")]
	public static void DeclineExchange(ChatCommandContext ctx)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);
		var sale = SalesService.GetSaleByBuyer(localuser);

		if (sale == null)
		{
			ctx.Reply($"{Markup.Prefix}No exchange request found.");
			return;
		}

		ctx.Reply($"{Markup.Prefix}Exchange request from {sale.Seller.CharacterName} has been declined.");

		// Send message to seller
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, sale.Seller.User, $"{Markup.Prefix} {sale.Buyer.CharacterName} has declined your exchange request.");

		// Remove sale
		SalesService.RemoveSale(sale.Seller);

		Plugin.Logger.Info("UserCommands", $"Exchange request from '{sale.Seller.CharacterName}' declined by '{sale.Buyer.CharacterName}'");
	}
}