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

// All credits to Odjit, kindred-commands for NPC spawning patch / callback function.
internal static class UnitSpawnerService
{
	private static readonly Entity EmptyEntity = new();

	public static Entity SpawnWithCallback(Entity user, PrefabGUID unit, float2 position, float duration, Action<Entity> postActions, float yPosition = -1)
	{
		if (Mathf.Approximately(yPosition, -1))
		{
			var translation = Core.EntityManager.GetComponentData<Translation>(user);
			yPosition = translation.Value.y;
		}
		var pos = new float3(position.x, yPosition, position.y);
		var usus = Core.Server.GetExistingSystemManaged<UnitSpawnerUpdateSystem>();

		UnitSpawnerReactSystemPatch.Enabled = true;

		var durationKey = NextKey();
		usus.SpawnUnit(EmptyEntity, unit, pos, 1, 0, 0, durationKey);
		PostActions.Add(durationKey, (duration, postActions));

		// Return the newly spawned entity
		return EmptyEntity;
	}

	private static long NextKey()
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

	private static readonly Dictionary<long, (float actualDuration, Action<Entity> Actions)> PostActions = [];

	[HarmonyPatch(typeof(UnitSpawnerReactSystem), nameof(UnitSpawnerReactSystem.OnUpdate))]
	public static class UnitSpawnerReactSystemPatch
	{
		public static bool Enabled { get; set; } = false;

		public static void Prefix(UnitSpawnerReactSystem __instance)
		{
			if (!Enabled) return;

			var entities = __instance.__query_2099432243_0.ToEntityArray(Unity.Collections.Allocator.Temp);

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

			entities.Dispose();
		}
	}
}