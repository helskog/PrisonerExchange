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
using static PrisonerExchange.Models.PrisonerModel;
using Unity.Collections;

namespace PrisonerExchange.Commands;

public class PrisonerService
{
	/// <summary>
	/// Gather a list of prisoners owned by a clan / user.
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

	/// <summary>
	/// Spawn new NPC and attach it to a prisoncell.
	/// </summary>
	public static void SpawnPrisonerInCell(PrisonerInformation prisonerInfo, Entity prisoncellEntity)
	{
		PrisonCell prisonCell = prisoncellEntity.Read<PrisonCell>();
		Prisonstation prisonStation = prisoncellEntity.Read<Prisonstation>();

		if (prisoncellEntity.TryGetComponent<LocalToWorld>(out var localToWorld))
		{
			var center = GetPrisonCellCenter(localToWorld.Position);

			Entity spawnedEntity = Services.UnitSpawnerService.SpawnWithCallback(
			Entity.Null,
			prisonerInfo.PrefabGUID,
			new float2(center.x, center.z),
			-1,
			(e) =>
			{
				if (e.Has<BloodConsumeSource>())
				{
					var blood = Core.EntityManager.GetComponentData<BloodConsumeSource>(e);
					blood.UnitBloodType._Value = prisonerInfo.BloodInfo.UnitBloodType;
					blood.BloodQuality = prisonerInfo.BloodInfo.BloodQuality;
					blood.CanBeConsumed = true;
					Core.EntityManager.SetComponentData(e, blood);
				}

				Imprisoned imprisoned = new Imprisoned();
				imprisoned.PrisonCellEntity = prisoncellEntity;

				BehaviourTreeState behaviourTreeState = e.Read<BehaviourTreeState>();
				behaviourTreeState.Value = GenericEnemyState.Imprisoned;
				BehaviourTreeStateMetadata behaviourTreeStateMetadata = e.Read<BehaviourTreeStateMetadata>();
				behaviourTreeStateMetadata.PreviousState = GenericEnemyState.Imprisoned;

				e.Add<Imprisoned>();
				e.Write(imprisoned);
				e.Write(behaviourTreeState);

				prisonCell.ImprisonedEntity = e;
				prisoncellEntity.Write(prisonCell);
				prisonStation.HasPrisoner = true;
				prisoncellEntity.Write(prisonStation);

				// ImprisonedBuff
				BuffUtil.BuffNPC(e, Entity.Null, BuffUtil.ELECTRIC_BUFF, -1);
			},
			center.y
			);
		}
	}

	/// <summary>
	/// Move one prisoner to an empty cell.
	/// </summary>
	public static void MovePrisoner(PrisonerModel prisoner, Entity prisoncell)
	{
		Entity prisonerEntity = prisoner.PrisonerEntity;

		// Gather information about prisoner.
		PrisonerInformation prisonerInformation = prisoner.Info;

		// Kill senderentity
		StatChangeUtility.KillEntity(Core.EntityManager, prisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);
		Plugin.Logger.Info("PrisonerService", $"Killing sender prisoner {prisonerEntity}.");

		// Spawn new NPC for receiver
		SpawnPrisonerInCell(prisonerInformation, prisonerEntity);
	}

	/// <summary>
	/// Swap two prisoners between different prisoncells.
	/// </summary>
	public static bool SwapPrisoner(PrisonerModel PrisonerA, PrisonerModel PrisonerB)
	{
		Entity prisonCellA = GetAttachedPrisonCell(PrisonerA);
		Entity prisonCellB = GetAttachedPrisonCell(PrisonerB);

		if (prisonCellA == Entity.Null || prisonCellB == Entity.Null)
		{
			Plugin.Logger.Error("PrisonerService", $"Could not get either PrisoncellA or PrisoncellB during prisoner swap.");
			return false;
		}

		// Get prisoner information for spawning new ones.
		PrisonerInformation prisonerInformationA = PrisonerA.Info;
		PrisonerInformation prisonerInformationB = PrisonerB.Info;

		// Kill both prisoners
		StatChangeUtility.KillEntity(Core.EntityManager, PrisonerA.PrisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);
		StatChangeUtility.KillEntity(Core.EntityManager, PrisonerB.PrisonerEntity, Entity.Null, 0.0, StatChangeReason.Default);

		// Spawn new prisoners in opposite cells
		return true;
	}

	/// <summary>
	/// Get attached prison cell entity of a prisoner
	/// </summary>
	public static Entity GetAttachedPrisonCell(PrisonerModel prisoner)
	{
		if (!prisoner.PrisonerEntity.Has<Imprisoned>())
			return Entity.Null;

		return prisoner.PrisonerEntity.Read<Imprisoned>().PrisonCellEntity;
	}

	/// <summary>
	/// Calculate center coordinates of a prison cell.
	/// </summary>
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