using BepInEx.Configuration;

namespace PrisonerExchange;

internal class Configuration
{
	internal static bool AdminBypass { get; private set; }
	internal static bool SellingEnabled { get; private set; }
	internal static bool SwappingEnabled { get; private set; }

	internal static bool AnnounceExchange { get; private set; }
	internal static string CurrencyPrefab { get; private set; }
	internal static string CurrencyName { get; private set; }
	internal static int MinimumSalePrice { get; private set; }
	internal static int MaximumSalePrice { get; private set; }

	internal static bool ClanLeaderOnly { get; private set; }

	internal static int CommandCoolDownPeriod { get; private set; }
	internal static int ExpireExchangeAfter { get; private set; }

	internal static void Initialize(ConfigFile config)
	{
		AdminBypass = config.Bind("General", "AdminBypass", true, "Allow admins to bypass restrictions on selling/swapping prisoners.").Value;
		SellingEnabled = config.Bind("General", "SellingEnabled", true, "Enable or disable the ability to sell prisoners.").Value;
		SwappingEnabled = config.Bind("General", "SwappingEnabled", true, "Enable or disable the ability to swap prisoners.").Value;

		AnnounceExchange = config.Bind("General", "AnnounceExchange", true, "Announce completed sales in global chat.").Value;
		CurrencyPrefab = config.Bind("General", "CurrencyPrefab", "-257494203", "Prefab GUID for the currency. (Crystals by default)").Value;
		CurrencyName = config.Bind("General", "CurrencyName", "Crystals", "Name of currency.").Value;
		MinimumSalePrice = config.Bind("General", "MinimumSalePrice", 100, "Set the minimum amount of currency required for a sale.").Value;
		MaximumSalePrice = config.Bind("General", "MinimumSalePrice", 5000, "Set the maximum amount of currency allowed for a sale.").Value;

		ClanLeaderOnly = config.Bind("General", "ClanLeaderOnly", false, "Only allow clan leader to sell prisoners.").Value;

		CommandCoolDownPeriod = config.Bind("General", "CommandCoolDownPeriod", 5, "Adds a fixed cooldown period for selling/swapping prisoners (minutes).").Value;
		ExpireExchangeAfter = config.Bind("General", "ExpireExchangeAfter", 60, "Automatically expire sales and swap requests. (seconds)").Value;
	}
}