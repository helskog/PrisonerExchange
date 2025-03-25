using System;
using System.Collections.Concurrent;

namespace PrisonerExchange.Services.Chat;

public static class CooldownTracker
{
	// userId, commandName
	private static ConcurrentDictionary<(ulong UserId, string CommandName), DateTime> cooldowns = new ConcurrentDictionary<(ulong, string), DateTime>();

	public static bool IsOnCooldown(ulong userId, string commandName)
	{
		if (cooldowns.TryGetValue((userId, commandName), out var nextAllowed))
		{
			return DateTime.UtcNow < nextAllowed;
		}
		return false;
	}

	public static double GetRemainingSeconds(ulong userId, string commandName)
	{
		if (cooldowns.TryGetValue((userId, commandName), out var nextAllowed))
		{
			var remaining = nextAllowed - DateTime.UtcNow;
			return remaining.TotalSeconds > 0 ? remaining.TotalSeconds : 0;
		}
		return 0;
	}

	public static void SetCooldown(ulong userId, string commandName)
	{
		var nextAllowed = DateTime.UtcNow.Add(TimeSpan.FromMinutes(Configuration.CommandCoolDownPeriod));
		cooldowns[(userId, commandName)] = nextAllowed;
	}

	public static void ClearAllCooldowns(ulong userId)
	{
		foreach (var key in cooldowns.Keys)
		{
			if (key.UserId == userId)
				cooldowns.TryRemove(key, out _);
		}
	}
}