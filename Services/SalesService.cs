using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;

using ProjectM;

namespace PrisonerExchange.Services;

public static class SalesService
{
	private static readonly List<PendingSale> list = new List<PendingSale>();
	private static Timer cleanupTimer;

	static SalesService()
	{
		cleanupTimer = new Timer(5_000);
		cleanupTimer.Elapsed += (s, e) => RemoveExpiredSales();
		cleanupTimer.AutoReset = true;
		cleanupTimer.Enabled = true;
	}

	public static void AddSale(PendingSale sale)
	{
		list.Add(sale);
		Plugin.Logger.Info("SalesService", $"Prisoner exchange created : Seller={sale.Seller.CharacterName}, Buyer={sale.Buyer.CharacterName}, Price={sale.Price}");
	}

	public static void RemoveSale(UserModel seller)
	{
		var sale = GetSaleBySeller(seller);

		if (sale != null)
		{
			list.Remove(sale);
			Plugin.Logger.Info("SalesService", $"Prisoner exchange removed : Seller={sale.Seller.CharacterName}, Buyer={sale.Buyer.CharacterName}");
		}
	}

	public static List<PendingSale> GetAll()
	{
		return list;
	}

	public static PendingSale GetSaleBySeller(UserModel seller)
	{
		return list.FirstOrDefault(s => s.Seller.PlatformId == seller.PlatformId);
	}

	public static PendingSale GetSaleByBuyer(UserModel buyer)
	{
		return list.FirstOrDefault(s => s.Buyer.PlatformId == buyer.PlatformId);
	}

	public static void ClearAll()
	{
		list.Clear();
	}

	public static bool SaleExists(UserModel user)
	{
		return SalesService.GetAll().Any(sale => sale.Seller.PlatformId == user.PlatformId);
	}

	public static void RemoveExpiredSales()
	{
		lock (list)
		{
			foreach (var expired in list.Where(sale => (DateTime.UtcNow - sale.CreatedAt).TotalSeconds >= sale.LifetimeSeconds).ToList())
			{
				// Notify players
				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, expired.Seller.User,
						$"{Markup.Prefix}Your prisoner exchange request to {expired.Buyer.CharacterName} has expired.");

				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, expired.Buyer.User,
						$"{Markup.Prefix}The prisoner exchange request from {expired.Seller.CharacterName} has expired.");

				list.Remove(expired);
			}
		}
	}
}