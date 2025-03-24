using System;

using PrisonerExchange.Models;

using Unity.Entities;

namespace PrisonerExchange.Services;

public class PendingSwap
{
	public UserModel Seller { get; }
	public UserModel Buyer { get; }

	public PrisonerModel PrisonerA { get; }
	public PrisonerModel PrisonerB { get; }

	public DateTime CreatedAt { get; }
	public double LifetimeSeconds { get; }

	public PendingSwap(UserModel seller, UserModel buyer, PrisonerModel prisonera, PrisonerModel prisonerb, double lifetimeSeconds = 120)
	{
		Seller = seller;
		Buyer = buyer;
		PrisonerA = prisonera;
		PrisonerB = prisonerb;

		CreatedAt = DateTime.UtcNow;
		LifetimeSeconds = lifetimeSeconds;
	}
}