using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;

using PrisonerExchange.Extensions;
using PrisonerExchange.Models;
using PrisonerExchange.Utility;

using ProjectM;

namespace PrisonerExchange.Services;

public static class SwapService
{
	private static readonly List<PendingSwap> SwapsList = new();
	private static readonly Timer CleanupTimer;

	static SwapService()
	{
		CleanupTimer = new Timer(5_000);
		CleanupTimer.Elapsed += (s, e) => RemoveExpiredSwaps();
		CleanupTimer.AutoReset = true;
		CleanupTimer.Enabled = true;
	}

	public static void AddSwap(PendingSwap swap)
	{
		SwapsList.Add(swap);
		Plugin.Logger.Info("SwapService", $"Created prisoner swap. Initiator={swap.Seller.CharacterName}, Target={swap.Buyer.CharacterName}");
	}

	public static List<PendingSwap> GetAll()
	{
		return [.. SwapsList];
	}

	public static PendingSwap GetSwapBySeller(UserModel user)
	{
		return SwapsList.FirstOrDefault(s => s.Seller.PlatformId == user.PlatformId);
	}

	public static PendingSwap GetSwapByTarget(UserModel user)
	{
		return SwapsList.FirstOrDefault(s => s.Buyer.PlatformId == user.PlatformId);
	}

	public static bool SwapExists(UserModel user)
	{
		lock (SwapsList)
		{
			return SwapsList.Any(s => s.Seller.PlatformId == user.PlatformId ||
														s.Buyer.PlatformId == user.PlatformId);
		}
	}

	public static void RemoveSwap(UserModel seller)
	{
		lock (SwapsList)
		{
			var existing = GetSwapBySeller(seller);
			if (existing != null)
			{
				SwapsList.Remove(existing);
				Plugin.Logger.Info("SwapService",
						$"Removed prisoner swap from initiator={seller.CharacterName}");
			}
		}
	}

	public static void ClearAll()
	{
		lock (SwapsList)
		{
			SwapsList.Clear();
		}
	}

	private static void RemoveExpiredSwaps()
	{
		lock (SwapsList)
		{
			var expired = SwapsList
					.Where(s => (DateTime.UtcNow - s.CreatedAt).TotalSeconds >= s.LifetimeSeconds)
					.ToList();

			foreach (var ex in expired)
			{
				SwapsList.Remove(ex);

				// Inform both sides that it expired
				if (ex.Seller.User.IsConnected)
				{
					var msg = $"{Markup.Prefix}Your prisoner swap request with {ex.Buyer.CharacterName} has expired.";
					ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.Seller.User, msg);
				}
				if (ex.Buyer.User.IsConnected)
				{
					var msg = $"{Markup.Prefix}A prisoner swap request from {ex.Seller.CharacterName} has expired.";
					ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.Buyer.User, msg);
				}

				// Trt remove buff from both prisoners
				BuffUtil.RemoveBuff(ex.PrisonerA.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);
				BuffUtil.RemoveBuff(ex.PrisonerB.PrisonerEntity, BuffUtil.ELECTRIC_BUFF);
			}
		}
	}
}