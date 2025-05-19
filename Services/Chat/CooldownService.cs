using System;
using System.Collections.Concurrent;

namespace PrisonerExchange.Services.Chat;

public static class CooldownTracker
{
	// platformId, commandName
	private static readonly ConcurrentDictionary<(ulong platFormId, string CommandName), DateTime> Cooldowns = new ConcurrentDictionary<(ulong, string), DateTime>();

	public static bool IsOnCooldown(ulong platFormId, string commandName)
	{
		if (Cooldowns.TryGetValue((platFormId, commandName), out var nextAllowed))
		{
			return DateTime.UtcNow < nextAllowed;
		}
		return false;
	}

	public static double GetRemainingSeconds(ulong platFormId, string commandName)
	{
		if (Cooldowns.TryGetValue((platFormId, commandName), out var nextAllowed))
		{
			var remaining = nextAllowed - DateTime.UtcNow;
			return remaining.TotalSeconds > 0 ? remaining.TotalSeconds : 0;
		}
		return 0;
	}

	public static void SetCooldown(ulong platFormId, string commandName)
	{
		var nextAllowed = DateTime.UtcNow.Add(TimeSpan.FromMinutes(Configuration.CommandCoolDownPeriod));
		Cooldowns[(platFormId, commandName)] = nextAllowed;
	}

	public static void ClearAllCooldowns(ulong platFormId)
	{
		foreach (var key in Cooldowns.Keys)
		{
			if (key.platFormId == platFormId)
				Cooldowns.TryRemove(key, out _);
		}
	}
}