using ProjectM.Network;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using PrisonerExchange.Extensions;
using ProjectM.Shared;
using UnityEngine.TextCore.Text;

namespace PrisonerExchange.Utility;

// Credits to bloody core
public static class BuffUtil
{
	private static readonly EntityManager Em = Core.EntityManager;
	public static PrefabGUID _electricBuff = new(1237097606);

	private const int NoDuration = 0;
	private const int DefaultDuration = -1;

	public static bool BuffNpc(Entity character, Entity user, PrefabGUID buff, int duration = DefaultDuration)
	{
		if (Core.DebugEventsSystem == null)
		{
			Core.DebugEventsSystem = Core.Server.GetExistingSystemManaged<DebugEventsSystem>();
		}

		var buffEvent = new ApplyBuffDebugEvent
		{
			BuffPrefabGUID = buff
		};

		var fromCharacter = new FromCharacter
		{
			User = user,
			Character = character
		};

		if (!BuffUtility.TryGetBuff(Em, character, buff, out var buffEntity))
		{
			Core.DebugEventsSystem.ApplyBuff(fromCharacter, buffEvent);

			if (!BuffUtility.TryGetBuff(Em, character, buff, out buffEntity))
			{
				Plugin.Logger.Info("BuffUtil", $"Failed to apply buff {buff.GuidHash} to entity {character.Index}.");
				return false;
			}

			buffEntity.TryRemoveComponent<CreateGameplayEventsOnSpawn>();
			buffEntity.TryRemoveComponent<GameplayEventListeners>();

			if (duration > 0 && duration != DefaultDuration)
			{
				if (buffEntity.Has<LifeTime>())
				{
					var lifetime = buffEntity.Read<LifeTime>();
					lifetime.Duration = duration;
					buffEntity.Write(lifetime);
				}
			}
			else if (duration == NoDuration)
			{
				if (buffEntity.Has<LifeTime>())
				{
					var lifetime = buffEntity.Read<LifeTime>();
					lifetime.Duration = -1;
					lifetime.EndAction = LifeTimeEndAction.None;
					buffEntity.Write(lifetime);
				}

				buffEntity.TryRemoveComponent<RemoveBuffOnGameplayEvent>();
				buffEntity.TryRemoveComponent<RemoveBuffOnGameplayEventEntry>();
			}

			Plugin.Logger.Info("BuffUtil", $"Successfully applied buff {buff.GuidHash} to entity {character.Index}.");
			return true;
		}

		Plugin.Logger.Info("BuffUtil", $"Buff {buff.GuidHash} is already present on entity {character.Index}.");
		return false;
	}

	public static bool RemoveBuff(Entity character, PrefabGUID buff)
	{
		if (BuffUtility.TryGetBuff(Em, character, buff, out var buffEntity))
		{
			DestroyUtility.Destroy(Em, buffEntity, DestroyDebugReason.TryRemoveBuff);

			Plugin.Logger.Info("BuffUtil", $"Successfully removed buff {buff.GuidHash} from entity {character.Index}.");
			return true;
		}

		Plugin.Logger.Info("BuffUtil", $"Successfully removed buff {buff.GuidHash} from entity {character.Index}.");
		return false;
	}
}
