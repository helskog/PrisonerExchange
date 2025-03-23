using Il2CppInterop.Runtime;

using PrisonerExchange.Extensions;

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace PrisonerExchange.Utility;

public class EntityUtil
{
	public static Entity FindClosestInRadius<T>(Entity player, float radius)
	{
		float3 playerPos = float3.zero;

		if (player.TryGetComponent<LocalToWorld>(out var localToWorld))
		{
			playerPos = localToWorld.Position;
		}

		var componentQuery = Core.EntityManager.CreateEntityQuery(
				ComponentType.ReadOnly<T>(),
				ComponentType.ReadOnly<LocalToWorld>()
		);

		var entities = componentQuery.ToEntityArray(Allocator.Temp);

		Entity closest = Entity.Null;
		float distanceSq = radius * radius;

		try
		{
			foreach (var entity in entities)
			{
				float3 entityPos = Core.EntityManager.GetComponentData<LocalToWorld>(entity).Position;
				float dsq = math.distancesq(playerPos, entityPos);
				if (dsq < distanceSq)
				{
					distanceSq = dsq;
					closest = entity;
				}
			}
		}
		finally
		{
			entities.Dispose();
		}

		return closest;
	}

	public static NativeArray<Entity> GetEntitiesByComponentType<T1>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		EntityQueryDesc queryDesc = new()
		{
			All = new ComponentType[] { new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite) },
			Options = options
		};

		var query = Core.EntityManager.CreateEntityQuery(queryDesc);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}

	public static NativeArray<Entity> GetEntitiesByComponentTypes<T1, T2>(bool includeAll = false, bool includeDisabled = false, bool includeSpawn = false, bool includePrefab = false, bool includeDestroyed = false)
	{
		EntityQueryOptions options = EntityQueryOptions.Default;
		if (includeAll) options |= EntityQueryOptions.IncludeAll;
		if (includeDisabled) options |= EntityQueryOptions.IncludeDisabled;
		if (includeSpawn) options |= EntityQueryOptions.IncludeSpawnTag;
		if (includePrefab) options |= EntityQueryOptions.IncludePrefab;
		if (includeDestroyed) options |= EntityQueryOptions.IncludeDestroyTag;

		EntityQueryDesc queryDesc = new()
		{
			All = new ComponentType[] { new(Il2CppType.Of<T1>(), ComponentType.AccessMode.ReadWrite), new(Il2CppType.Of<T2>(), ComponentType.AccessMode.ReadWrite) },
			Options = options
		};

		var query = Core.EntityManager.CreateEntityQuery(queryDesc);

		var entities = query.ToEntityArray(Allocator.Temp);
		return entities;
	}
}