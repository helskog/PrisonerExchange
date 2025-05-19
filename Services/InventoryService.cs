using PrisonerExchange.Extensions;
using PrisonerExchange.Models;

using ProjectM;
using ProjectM.Scripting;

using Stunlock.Core;

using Unity.Entities;

namespace PrisonerExchange.Services;

public static class InventoryService
{
	private static readonly PrefabGUID CurrencyPrefab = new PrefabGUID(int.Parse(Configuration.CurrencyPrefab));

	// Credits to BloodyCore, Kindred for reference
	private static AddItemResponse TryAddUserInventoryItem(Entity characterEntity, PrefabGUID itemGuid, int stacks)
	{
		Core.ServerScriptMapper ??= Core.Server.GetExistingSystemManaged<ServerScriptMapper>();
		return Core.ServerScriptMapper.GetServerGameManager().TryAddInventoryItem(characterEntity, itemGuid, stacks);
	}

	public static bool AddCurrencyToInventory(UserModel user, int amount)
	{
		Entity userCharacterEntity = user.User.LocalCharacter._Entity;

		AddItemResponse response = TryAddUserInventoryItem(userCharacterEntity, CurrencyPrefab, amount);
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

		if (InventoryUtilitiesServer.TryRemoveItem(Core.EntityManager, userCharacterEntity, CurrencyPrefab, amount))
		{
			return true;
		}

		return false;
	}

	public static bool CanAfford(UserModel user, int price)
	{
		Entity userCharacterEntity = user.User.LocalCharacter._Entity;
		int totalCurrency = InventoryUtilities.GetItemAmount(Core.EntityManager, userCharacterEntity, CurrencyPrefab);
		Plugin.Logger.Info("InventoryService", $"Currency in inventory: {totalCurrency}");

		if (totalCurrency >= price)
		{
			return true;
		}

		return false;
	}
}