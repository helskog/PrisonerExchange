using System;

using PrisonerExchange.Models;

using Unity.Entities;

namespace PrisonerExchange.Services;

public class PendingSwap
{
	public UserModel Initiator { get; }
	public UserModel TargetUser { get; }

	// The type of prisoner the initiator wants from the target
	public string RequestedPrisonerType { get; }

	public string RequestedBloodType { get; }
	public int RequestedBloodQuality { get; }

	// Reference for the prison cell holding the initiator's prisoner
	public Entity InitiatorPrisonCell { get; }

	public DateTime CreatedAt { get; }
	public double LifeTimeSeconds { get; }

	public PendingSwap(
			UserModel initiator,
			UserModel targetUser,
			string requestedPrisonerType,
			string requestedBloodType,
			int requestedBloodQuality,
			Entity initiatorPrisonCell,
			DateTime createdAt,
			double lifeTimeSeconds
	)
	{
		Initiator = initiator;
		TargetUser = targetUser;

		RequestedPrisonerType = requestedPrisonerType;
		RequestedBloodType = requestedBloodType;
		RequestedBloodQuality = requestedBloodQuality;

		InitiatorPrisonCell = initiatorPrisonCell;
		CreatedAt = createdAt;
		LifeTimeSeconds = lifeTimeSeconds;
	}
}