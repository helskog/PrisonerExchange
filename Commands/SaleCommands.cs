using MortiumGames.Utils;
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
internal class SaleCommands
{
	/// <summary>
	/// Make an offer to sell a prisoner to another user.
	/// </summary>
	[Command("sell", description: "Sell a prisoner for a price.")]
	public static void ExchangeCommand(ChatCommandContext ctx, string username, int price)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);
		UserModel targetuser = UserUtil.GetUserByCharacterName(username);

		if (targetuser == null)
		{
			ctx.Reply($"{Markup.Prefix}Could not find user with the name {Markup.Highlight(username)}.");
			return;
		}

		if (SalesService.SaleExists(targetuser) || SwapService.SwapExists(targetuser))
		{
			ctx.Reply($"{Markup.Prefix}Target already have a pending exchange offer!");
			return;
		}

		if (SalesService.SaleExists(localuser))
		{
			ctx.Reply($"{Markup.Prefix}You already have a pending exchange offer!");
			return;
		}

		if (!localuser.IsAdmin)
		{
			if (!Configuration.SellingEnabled)
			{
				ctx.Reply($"{Markup.Prefix}Selling prisoners is not enabled!");
				return;
			}

			if (CooldownTracker.IsOnCooldown(localuser.PlatformId, "sell"))
			{
				var remaining = CooldownTracker.GetRemainingSeconds(localuser.PlatformId, "sell");
				ctx.Reply($"{Markup.Prefix}You must wait another {(int)remaining} seconds before using /swap accept again!");
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

			if (price < Configuration.MinimumSalePrice)
			{
				ctx.Reply($"{Markup.Prefix}You can only sell a prisoner for {Configuration.MinimumSalePrice} {Configuration.CurrencyName} or more!");
				return;
			}

			if (price > Configuration.MaximumSalePrice)
			{
				ctx.Reply($"{Markup.Prefix}Sale cannot exceed {Configuration.MaximumSalePrice} {Configuration.CurrencyName}!");
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
		BuffUtil.BuffNpc(unitEntity, localuser.Entity, BuffUtil._electricBuff, Configuration.ExpireExchangeAfter);

		// Send message to receiving user.
		var msg = new FixedString512Bytes(StringBuilders.SalesInfoMessage(newSale));
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, targetuser.User, ref msg);

		// Notify incoming offer
		SequenceUtils.SpawnSequence(targetuser.Entity, new(-1177491659), 5);

		// Inform sender that the offer has been sent.
		var informMessage = new FixedString512Bytes($"{Markup.Prefix}Request sent to user {targetuser.CharacterName}");
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, localuser.User, ref informMessage);

		CooldownTracker.SetCooldown(localuser.PlatformId, "sell");
	}

	/// <summary>
	/// Cancel outgoing exchange request.
	/// </summary>

	[Command("cancelsale", description: "Cancel your outgoing exchange request.")]
	public static void CancelCommand(ChatCommandContext ctx)
	{
		UserModel localuser = UserUtil.GetCurrentUser(ctx);

		var sale = SalesService.GetSaleBySeller(localuser);

		if (sale == null)
		{
			ctx.Reply($"{Markup.Prefix}No active exchange request found.");
			return;
		}

		if (!BuffUtil.RemoveBuff(sale.PrisonerEntity, BuffUtil._electricBuff))
			Plugin.Logger.Error("UserCommands", $"Failed to apply buff to {sale.PrisonerEntity}.");

		SalesService.RemoveSale(localuser);
		ctx.Reply($"{Markup.Prefix}Your exchange request has been canceled.");

		Plugin.Logger.Info("UserCommands", $"User '{localuser.CharacterName}' cancelled their prisoner sale.");
	}

	/// <summary>
	/// Accept incoming prisoner exchange request.
	/// </summary>

	[Command("acceptsale", description: "Accept incoming prisoner exchange request.")]
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
			var payoutErrorMessage = new FixedString512Bytes($"Something went wrong when paying out {price} {Configuration.CurrencyName} contact an administrator!");
			ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, seller.User, ref payoutErrorMessage);
		}

		// Complete transaction
		SalesService.RemoveSale(seller);
		var completedTransactionMessage = new FixedString512Bytes($"{localuser.CharacterName} has purchased your prisoner for {price} {Configuration.CurrencyName}!");
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, seller.User, ref completedTransactionMessage);

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
	[Command("declinesale", description: "Decline incoming prisoner exchange request.")]
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
		var declinedExchangeMessage = new FixedString512Bytes($"{Markup.Prefix} {sale.Buyer.CharacterName} has declined your exchange request.");
		ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, sale.Seller.User, ref declinedExchangeMessage);

		// Remove sale
		SalesService.RemoveSale(sale.Seller);

		Plugin.Logger.Info("UserCommands", $"Exchange request from '{sale.Seller.CharacterName}' declined by '{sale.Buyer.CharacterName}'");
	}
}