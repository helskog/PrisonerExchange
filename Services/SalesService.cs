﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;

using ProjectM;

namespace PrisonerExchange.Services;

public static class SalesService
{
	private static readonly List<PendingSale> SalesList = new List<PendingSale>();
	private static Timer CleanupTimer;

	static SalesService()
	{
		CleanupTimer = new Timer(5_000);
		CleanupTimer.Elapsed += (s, e) => RemoveExpiredSales();
		CleanupTimer.AutoReset = true;
		CleanupTimer.Enabled = true;
	}

	public static void AddSale(PendingSale sale)
	{
		SalesList.Add(sale);
		Plugin.Logger.Info("SalesService", $"Prisoner exchange created : Seller={sale.Seller.CharacterName}, Buyer={sale.Buyer.CharacterName}, Price={sale.Price}");
	}

	public static List<PendingSale> GetAll()
	{
		return SalesList;
	}

	public static PendingSale GetSaleBySeller(UserModel seller)
	{
		return SalesList.FirstOrDefault(s => s.Seller.PlatformId == seller.PlatformId);
	}

	public static PendingSale GetSaleByBuyer(UserModel buyer)
	{
		return SalesList.FirstOrDefault(s => s.Buyer.PlatformId == buyer.PlatformId);
	}

	public static void RemoveSale(UserModel seller)
	{
		lock (SalesList)
		{
			var sale = GetSaleBySeller(seller);
			if (sale != null)
			{
				SalesList.Remove(sale);
				Plugin.Logger.Info("SalesService", $"Prisoner exchange removed : Seller={sale.Seller.CharacterName}, Buyer={sale.Buyer.CharacterName}");
			}
		}
	}

	public static void ClearAll()
	{
		lock (SalesList)
		{
			SalesList.Clear();
		}
	}

	public static bool SaleExists(UserModel user)
	{
		lock (SalesList)
		{
			return SalesService.GetAll().Any(sale => sale.Seller.PlatformId == user.PlatformId);
		}
	}

	public static void RemoveExpiredSales()
	{
		lock (SalesList)
		{
			foreach (var expired in SalesList.Where(sale => (DateTime.UtcNow - sale.CreatedAt).TotalSeconds >= sale.LifetimeSeconds).ToList())
			{
				// Notify players
				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, expired.Seller.User,
						$"{Markup.Prefix}Your prisoner exchange request to {expired.Buyer.CharacterName} has expired.");

				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, expired.Buyer.User,
						$"{Markup.Prefix}The prisoner exchange request from {expired.Seller.CharacterName} has expired.");

				SalesList.Remove(expired);
			}
		}
	}
}