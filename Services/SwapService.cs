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
	private static readonly List<PendingSwap> Swaps = new();
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
		lock (Swaps)
		{
			Swaps.Add(swap);
		}
		Plugin.Logger.Info("SwapService", $"Created prisoner swap. Initiator={swap.Initiator.CharacterName}, Target={swap.TargetUser.CharacterName}");
	}

	public static List<PendingSwap> GetAll()
	{
		lock (Swaps)
		{
			return Swaps.ToList();
		}
	}

	public static PendingSwap GetSwapByInitiator(UserModel user)
	{
		lock (Swaps)
		{
			return Swaps.FirstOrDefault(s => s.Initiator.PlatformId == user.PlatformId);
		}
	}

	public static PendingSwap GetSwapByTarget(UserModel user)
	{
		lock (Swaps)
		{
			return Swaps.FirstOrDefault(s => s.TargetUser.PlatformId == user.PlatformId);
		}
	}

	public static bool SwapExists(UserModel user)
	{
		lock (Swaps)
		{
			return Swaps.Any(s => s.Initiator.PlatformId == user.PlatformId ||
														s.TargetUser.PlatformId == user.PlatformId);
		}
	}

	public static void RemoveSwap(UserModel initiator)
	{
		lock (Swaps)
		{
			var existing = GetSwapByInitiator(initiator);
			if (existing != null)
			{
				Swaps.Remove(existing);
				Plugin.Logger.Info("SwapService",
						$"Removed prisoner swap from initiator={initiator.CharacterName}");
			}
		}
	}

	public static void ClearAll()
	{
		lock (Swaps)
		{
			Swaps.Clear();
		}
	}

	private static void RemoveExpiredSwaps()
	{
		lock (Swaps)
		{
			var expired = Swaps
					.Where(s => (DateTime.UtcNow - s.CreatedAt).TotalSeconds >= s.LifeTimeSeconds)
					.ToList();

			foreach (var ex in expired)
			{
				Swaps.Remove(ex);

				// Inform both sides that it expired
				if (ex.Initiator.User.IsConnected)
				{
					var msg = $"{Markup.Prefix}Your prisoner swap request with {ex.TargetUser.CharacterName} has expired.";
					ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.Initiator.User, msg);
				}
				if (ex.TargetUser.User.IsConnected)
				{
					var msg = $"{Markup.Prefix}A prisoner swap request from {ex.Initiator.CharacterName} has expired.";
					ServerChatUtils.SendSystemMessageToClient(Core.EntityManager, ex.TargetUser.User, msg);
				}

				// Remove buff from the initiator’s prisoner
				var cellData = Core.EntityManager.GetComponentData<global::ProjectM.PrisonCell>(ex.InitiatorPrisonCell);
				BuffUtil.RemoveBuff(cellData.ImprisonedEntity._Entity, BuffUtil.electricBuff);
			}
		}
	}
}