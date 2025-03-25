using BepInEx.Configuration;

namespace PrisonerExchange;

internal class Configuration
{
	internal static bool AnnounceExchange { get; private set; }
	internal static string CurrencyPrefab { get; private set; }
	internal static string CurrencyName { get; private set; }

	internal static bool ClanLeaderOnly { get; private set; }

	internal static int CommandCoolDownPeriod { get; private set; }
	internal static int ExpireExchangeAfter { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		AnnounceExchange = config.Bind("General", "AnnounceExchange", true, "Announce completed transactions globally.").Value;
		CurrencyPrefab = config.Bind("General", "CurrencyPrefab", "-257494203", "Prefab GUID for the currency. (Crystals by default)").Value;
		CurrencyName = config.Bind("General", "CurrencyName", "Crystals", "Name of currency.").Value;

		ClanLeaderOnly = config.Bind("General", "ClanLeaderOnly", false, "Only allow clan leader to sell prisoners.").Value;

		CommandCoolDownPeriod = config.Bind("General", "CommandCoolDownPeriod", 5, "Adds a fixed cooldown period for selling/swapping prisoners (minutes).").Value;
		ExpireExchangeAfter = config.Bind("General", "ExpireExchangeAfter", 120, "Automatically expire active exchange requests. (seconds)").Value;
	}
}