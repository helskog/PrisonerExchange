using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Services;
using PrisonerExchange.Services.Chat;
using PrisonerExchange.Utility;

using VampireCommandFramework;

namespace PrisonerExchange.Commands;

internal class AdminCommands
{
	/// <summary>
	/// Test command for development.
	/// </summary>
	[Command("pe swap", description: "temporary", adminOnly: true)]
	public void CheckPrisoners(ChatCommandContext ctx, string username)
	{
		UserModel initiator = UserUtil.GetCurrentUser(ctx);
		UserModel target = UserUtil.GetUserByCharacterName(username);

		if (PromptManager.IsWaiting(initiator.PlatformId))
			return;

		if (target == null || !target.User.IsConnected)
		{
			ctx.Reply($"{Markup.Prefix}User is invalid or offline.");
		}

		PrisonerModel selectedPrisoner = null;
		List<PrisonerModel> prisonerList = PrisonerService.GetPrisonerList(target);

		// Send formatted message to initiator
		Services.Chat.StringBuilders.SendPrisonerList(ctx, prisonerList);

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

			selectedPrisoner = prisonerList[selection - 1];

			ctx.Reply($"User selected: {selectedPrisoner.Info.UnitType} with {selectedPrisoner.Info.BloodQuality}% {selectedPrisoner.Info.BloodType}!");
		});
	}

	/// <summary>
	/// Lists all active exchange requests between users.
	/// </summary>
	[Command("pe list", description: "List all active prisoner exchanges.", adminOnly: true)]
	public void ListExchanges(ChatCommandContext ctx)
	{
		var salesList = SalesService.GetAll()
			.Select(sale =>
			{
				var info = sale.GetPrisonerInformation;

				return new
				{
					SellerName = sale.Seller.CharacterName,
					PrisonerType = info.UnitType,
					BloodType = info.BloodType,
					BloodQuality = info.BloodQuality,
					BuyerName = sale.Buyer.CharacterName,
					Price = sale.Price
				};
			})
			.OrderByDescending(x => x.Price)
			.ToList();

		var sb = new StringBuilder();
		sb.AppendLine("<size=15><color=yellow>Pending prisoner exchanges</color></size>");

		foreach (var sale in salesList)
		{
			sb.AppendLine(
				$"{Markup.Highlight(sale.SellerName)} is selling " +
				$"{Markup.Highlight(sale.PrisonerType)} with " +
				$"{Markup.Highlight(sale.BloodType)} " +
				$"{Markup.Highlight($"{sale.BloodQuality}%")} " +
				$"to {Markup.Highlight(sale.BuyerName)} " +
				$"for <color={Markup.SecondaryColor}>{sale.Price}</color> {Configuration.CurrencyName}"
			);
		}

		ctx.Reply(sb.ToString());
	}

	/// <summary>
	/// Removes a specific exchange belonging to the user named `from`.
	/// </summary>
	[Command("pe remove", description: "Clear a specific prisoner exchange.", adminOnly: true)]
	public void RemoveExchange(ChatCommandContext ctx, string from = "username")
	{
		var sale = SalesService.GetAll().FirstOrDefault(s => s.Seller.CharacterName.Equals(from, StringComparison.OrdinalIgnoreCase));

		if (sale == null)
		{
			ctx.Reply($"{Markup.Prefix}No prisoner exchange found for {Markup.SecondaryColor}{from}.");
			return;
		}

		SalesService.RemoveSale(sale.Seller);
		ctx.Reply($"{Markup.Prefix}Prisoner exchange from {from} has been removed.");
	}

	/// <summary>
	/// Clears all active exchanges between users.
	/// </summary>
	[Command("pe clear", description: "Clear all active prisoner exchanges.", adminOnly: true)]
	public void ClearExchanges(ChatCommandContext ctx)
	{
		SalesService.ClearAll();
		ctx.Reply($"{Markup.Prefix}Cleared all active exchanges!");
	}
}