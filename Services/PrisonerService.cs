using PrisonerExchange.Models;
using PrisonerExchange.Utility;
using ProjectM.Behaviours;
using ProjectM;
using Stunlock.Core;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using System.Collections.Generic;
using System;
using PrisonerExchange.Extensions;

namespace PrisonerExchange.Commands;

public class PrisonerService
{
	/// <summary>
	/// identify prisoners by clan name or username
	/// </summary>
	public static List<PrisonerModel> GetPrisonerList(UserModel user)
	{
		var characterName = user.CharacterName;
		var clanEntities = EntityUtil.GetEntitiesByComponentType<ClanTeam>().ToArray();
		var matchingClanEntity = clanEntities.FirstOrDefault(e => e.Read<ClanTeam>().Name.ToString().Equals(characterName, StringComparison.OrdinalIgnoreCase));

		var prisonerList = new List<PrisonerModel>();
		int teamValue = 0;

		if (matchingClanEntity != Entity.Null)
		{
			teamValue = matchingClanEntity.Read<ClanTeam>().TeamValue;
		}
		else
		{
			UserModel playerCharacter = UserUtil.GetUserByCharacterName(characterName);

			if (playerCharacter == null)
				return null;

			teamValue = playerCharacter.Entity.Read<Team>().Value;
		}

		if (teamValue == 0)
			return null;

		foreach (Entity prisonCell in EntityUtil.GetEntitiesByComponentType<PrisonCell>())
		{
			if (!Core.EntityManager.TryGetComponentData<PrisonCell>(prisonCell, out var prisonCellData))
				continue;

			if (prisonCellData.ImprisonedEntity._Entity == Entity.Null)
				continue;

			if (!prisonCell.TryGetComponent<Team>(out var cellTeamInfo) || cellTeamInfo.Value != teamValue)
				continue;

			PrisonerModel prisoner = new PrisonerModel(prisonCellData.ImprisonedEntity._Entity);

			if (!prisonerList.Contains(prisoner))
			{
				prisonerList.Add(prisoner);
			}
		}

		return prisonerList;
	}

	public static void SpawnPrisoner(Entity sender, UserModel receiver, PrefabGUID unit, PrefabGUID bloodtype, int bloodquality, Entity prisonCellEntity)
	{
		PrisonCell prisonCell = prisonCellEntity.Read<PrisonCell>();
		Prisonstation prisonstation = prisonCellEntity.Read<Prisonstation>();

		if (prisonCellEntity.TryGetComponent<LocalToWorld>(out var localToWorld))
		{
			var center = GetPrisonCellCenter(localToWorld.Position);

			Entity spawnedEntity = Services.UnitSpawnerService.SpawnWithCallback(
			sender,
			unit,
			new float2(center.x, center.z),
			-1,
			(e) =>
			{
				if (e.Has<BloodConsumeSource>())
				{
					var blood = Core.EntityManager.GetComponentData<BloodConsumeSource>(e);
					blood.UnitBloodType._Value = bloodtype;
					blood.BloodQuality = bloodquality;
					blood.CanBeConsumed = true;
					Core.EntityManager.SetComponentData(e, blood);
				}

				Imprisoned imprisoned = new Imprisoned();
				imprisoned.PrisonCellEntity = prisonCellEntity;
				BehaviourTreeState behaviourTreeState = e.Read<BehaviourTreeState>();
				behaviourTreeState.Value = GenericEnemyState.Imprisoned;
				BehaviourTreeStateMetadata behaviourTreeStateMetadata = e.Read<BehaviourTreeStateMetadata>();
				behaviourTreeStateMetadata.PreviousState = GenericEnemyState.Imprisoned;

				e.Add<Imprisoned>();
				e.Write(imprisoned);
				e.Write(behaviourTreeState);

				prisonCell.ImprisonedEntity = e;
				prisonCellEntity.Write(prisonCell);
				prisonstation.HasPrisoner = true;
				prisonCellEntity.Write(prisonstation);

				// ImprisonedBuff
				BuffUtil.BuffNPC(e, receiver.Entity, BuffUtil.ELECTRIC_BUFF, -1);
			},
			center.y
			);
		}
	}

	public static void MovePrisoner(Entity unitEntity, UserModel receiverUser, Entity receiverCell)
	{
		// Gather unitEntity values
		PrefabGUID prefab = unitEntity.Read<PrefabGUID>();
		BloodConsumeSource blood = unitEntity.Read<BloodConsumeSource>();

		// Kill senderentity
		StatChangeUtility.KillEntity(Core.EntityManager, unitEntity, Entity.Null, 0.0, StatChangeReason.Default);
		Plugin.Logger.Info("PrisonerService", $"Killing sender prisoner {unitEntity}.");

		// Spawn new NPC for receiver
		SpawnPrisoner(receiverUser.Entity, receiverUser, prefab, blood.UnitBloodType, (int)blood.BloodQuality, receiverCell);
		Plugin.Logger.Info("PrisonerService", $"Spawning new npc for {receiverUser.CharacterName}.");
	}

	public static float3 GetPrisonCellCenter(float3 cellPosition)
	{
		float cellWidth = 2.0f;
		float cellDepth = 2.0f;

		return new float3(
						math.floor(cellPosition.x) + cellWidth / 2f,
						cellPosition.y,
						math.floor(cellPosition.z) + cellDepth / 2f
		);
	}
}