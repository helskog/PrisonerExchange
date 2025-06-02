using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Utility;
using ProjectM;

using Unity.Collections;

using Timer = System.Timers.Timer;

namespace PrisonerExchange.Services;

public static class SalesService
{
	private static readonly List<PendingSale> SalesList = new();
	private static readonly Timer CleanupTimer;

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
			return GetAll().Any(sale => sale.Seller.PlatformId == user.PlatformId);
		}
	}

	private static void RemoveExpiredSales()
	{
		lock (SalesList)
		{
			foreach (var ex in SalesList.Where(sale => (DateTime.UtcNow - sale.CreatedAt).TotalSeconds >= sale.LifetimeSeconds).ToList())
			{
				// Notify players
				var expiredSellerMessage = new FixedString512Bytes($"{Markup.Prefix}Your prisoner exchange request to {ex.Buyer.CharacterName} has expired.");
				var expiredBuyerMessage = new FixedString512Bytes($"{Markup.Prefix}The prisoner exchange request from {ex.Seller.CharacterName} has expired.");

				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.Seller.User, ref expiredSellerMessage);
				ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.Buyer.User, ref expiredBuyerMessage);

				SalesList.Remove(ex);
				BuffUtil.RemoveBuff(ex.PrisonerEntity, BuffUtil._electricBuff);
			}
		}
	}
}