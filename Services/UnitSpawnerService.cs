using System;
using System.Collections.Generic;

using HarmonyLib;

using PrisonerExchange.Extensions;

using ProjectM;

using Stunlock.Core;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

using UnityEngine;

namespace PrisonerExchange.Services;

// All credits to Odjit, kindred-commands for the post-actions fix and patch to apply properties on spawn.
internal class UnitSpawnerService
{
	private static Entity empty_entity = new();

	internal const int DEFAULT_MINRANGE = 1;
	internal const int DEFAULT_MAXRANGE = 1;

	public static void Spawn(Entity user, PrefabGUID unit, int count, float2 position, float minRange = 1, float maxRange = 2, float duration = -1)
	{
		var translation = Core.EntityManager.GetComponentData<Translation>(user);
		var pos = new float3(position.x, translation.Value.y, position.y);
		var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();
		usus.SpawnUnit(empty_entity, unit, pos, count, minRange, maxRange, duration);
	}

	public static Entity SpawnWithCallback(Entity user, PrefabGUID unit, float2 position, float duration, Action<Entity> postActions, float yPosition = -1)
	{
		if (yPosition == -1)
		{
			var translation = Core.EntityManager.GetComponentData<Translation>(user);
			yPosition = translation.Value.y;
		}
		var pos = new float3(position.x, yPosition, position.y);
		var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();

		UnitSpawnerReactSystem_Patch.Enabled = true;

		var durationKey = NextKey();
		usus.SpawnUnit(empty_entity, unit, pos, 1, DEFAULT_MINRANGE, DEFAULT_MAXRANGE, durationKey);
		PostActions.Add(durationKey, (duration, postActions));

		// Return the newly spawned entity
		return empty_entity;
	}

	internal static long NextKey()
	{
		System.Random r = new();
		long key;
		int breaker = 5;
		do
		{
			key = r.NextInt64(10000) * 3;
			breaker--;
			if (breaker < 0)
			{
				throw new Exception($"Failed to generate a unique key for UnitSpawnerService");
			}
		} while (PostActions.ContainsKey(key));
		return key;
	}

	internal static Dictionary<long, (float actualDuration, Action<Entity> Actions)> PostActions = [];

	[HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
	public static class UnitSpawnerReactSystem_Patch
	{
		public static bool Enabled { get; set; } = false;

		public static void Prefix(UnitSpawnerReactSystem __instance)
		{
			if (!Enabled) return;

			var entities = __instance.__query_2099432189_0.ToEntityArray(Unity.Collections.Allocator.Temp);

			foreach (var entity in entities)
			{
				if (!Core.EntityManager.HasComponent<LifeTime>(entity)) return;

				var lifetimeComp = Core.EntityManager.GetComponentData<LifeTime>(entity);
				var durationKey = (long)Mathf.Round(lifetimeComp.Duration);

				if (PostActions.TryGetValue(durationKey, out var unitData))
				{
					Plugin.Logger.Info("UnitSpawnerService", $"Spawn callback triggered for entity={entity}, key={durationKey}");

					var (actualDuration, actions) = unitData;
					PostActions.Remove(durationKey);

					var endAction = actualDuration < 0 ? LifeTimeEndAction.None : LifeTimeEndAction.Destroy;

					var newLifeTime = new LifeTime()
					{
						Duration = actualDuration,
						EndAction = endAction
					};

					Core.EntityManager.SetComponentData(entity, newLifeTime);

					actions(entity);
				}
			}
		}
	}
}