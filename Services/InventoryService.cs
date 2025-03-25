using PrisonerExchange.Extensions;
using PrisonerExchange.Models;

using ProjectM;

using Stunlock.Core;

using Unity.Entities;

namespace PrisonerExchange.Services;

public class InventoryService
{
	public static PrefabGUID currencyPrefab = new PrefabGUID(int.Parse(Configuration.CurrencyPrefab));

	public static AddItemResponse TryAddUserInventoryItem(Entity CharacterEntity, PrefabGUID itemGuid, int stacks)
	{
		return Core.ServerScriptMapper.GetServerGameManager().TryAddInventoryItem(CharacterEntity, itemGuid, stacks);
	}

	public static bool AddCurrencyToInventory(UserModel user, int amount)
	{
		Entity userCharacterEntity = user.User.LocalCharacter._Entity;

		AddItemResponse response = TryAddUserInventoryItem(userCharacterEntity, currencyPrefab, amount);
		if (response.Success)
		{
			return true;
		}

		if (response.RemainingAmount > 0)
		{
			Plugin.Logger.Error("InventoryService", $"{user.CharacterName} only had space for {amount - response.RemainingAmount} {Configuration.CurrencyName}.");
		}

		return false;
	}

	public static bool RemoveCurrencyFromInventory(UserModel user, int amount)
	{
		Entity userCharacterEntity = user.User.LocalCharacter._Entity;

		if (InventoryUtilitiesServer.TryRemoveItem(Core.EntityManager, userCharacterEntity, currencyPrefab, amount))
		{
			return true;
		}

		return false;
	}

	public static bool CanAfford(UserModel user, int price)
	{
		Entity userCharacterEntity = user.User.LocalCharacter._Entity;
		int totalCurrency = InventoryUtilities.GetItemAmount(Core.EntityManager, userCharacterEntity, new PrefabGUID(-257494203));
		Plugin.Logger.Info("InventoryService", $"Currency in inventory: {totalCurrency}");

		if (totalCurrency >= price)
		{
			return true;
		}

		return false;
	}
}