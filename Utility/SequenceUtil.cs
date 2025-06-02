using PrisonerExchange;
using PrisonerExchange.Extensions;
using ProjectM;
using ProjectM.Sequencer;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

namespace MortiumGames.Utils;

public static class SequenceUtils
{
	public static readonly SequenceGUID Smite = new(-243689524);

	public static void SpawnSequence(Entity target, SequenceGUID guid, int lifetime)
	{
		var pos = target.Read<Translation>().Value;

		if (!Core.PrefabCollection._PrefabGuidToEntityMap.TryGetValue(new(651179295), out var prefab))
		{
			Plugin.Logger.Error("SequenceUtil", "PrefabEntity not found!");
			return;
		}

		var entity = Core.EntityManager.Instantiate(prefab);

		entity.Add<PhysicsCustomTags>();
		entity.Write(new Translation { Value = pos });

		entity.HasWith((ref SpawnSequenceForEntity ss) =>
			{
				ss.SequenceGuid = guid;
				ss.Target = target;
				ss.SecondaryTarget = Entity.Null;
			}
		);

		entity.HasWith((ref LifeTime lf) =>
			{
				lf.Duration = lifetime;
				lf.EndAction = LifeTimeEndAction.Destroy;
			}
		);
	}
}