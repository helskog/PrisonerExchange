using ProjectM.Network;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;
using PrisonerExchange.Extensions;
using ProjectM.Shared;
using UnityEngine.TextCore.Text;

namespace PrisonerExchange.Utility;

// Credits to bloody core
public class BuffUtil
{
	public static EntityManager EM = Core.EntityManager;
	public static PrefabGUID electricBuff = new PrefabGUID(1237097606);

	public const int NO_DURATION = 0;
	public const int DEFAULT_DURATION = -1;

	public static bool BuffNPC(Entity character, Entity user, PrefabGUID buff, int duration = DEFAULT_DURATION)
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

		if (!BuffUtility.TryGetBuff(EM, character, buff, out var buffEntity))
		{
			Core.DebugEventsSystem.ApplyBuff(fromCharacter, buffEvent);

			if (!BuffUtility.TryGetBuff(EM, character, buff, out buffEntity))
			{
				Plugin.Logger.Info("BuffUtil", $"Failed to apply buff {buff.GuidHash} to entity {character.Index}.");
				return false;
			}

			buffEntity.TryRemoveComponent<CreateGameplayEventsOnSpawn>();
			buffEntity.TryRemoveComponent<GameplayEventListeners>();

			if (duration > 0 && duration != DEFAULT_DURATION)
			{
				if (buffEntity.Has<LifeTime>())
				{
					var lifetime = buffEntity.Read<LifeTime>();
					lifetime.Duration = duration;
					buffEntity.Write(lifetime);
				}
			}
			else if (duration == NO_DURATION)
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

	public static bool RemoveBuff(Entity Character, PrefabGUID Buff)
	{
		if (BuffUtility.TryGetBuff(EM, Character, Buff, out var buffEntity))
		{
			DestroyUtility.Destroy(EM, buffEntity, DestroyDebugReason.TryRemoveBuff);

			Plugin.Logger.Info("BuffUtil", $"Successfully removed buff {Buff.GuidHash} from entity {Character.Index}.");
			return true;
		}

		return false;
	}
}