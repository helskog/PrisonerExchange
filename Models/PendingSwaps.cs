using System;

namespace PrisonerExchange.Models;

public class PendingSwap(
	UserModel seller,
	UserModel buyer,
	PrisonerModel prisonera,
	PrisonerModel prisonerb,
	double lifetimeSeconds = 120
)
{
	public UserModel Seller { get; } = seller;
	public UserModel Buyer { get; } = buyer;

	public PrisonerModel PrisonerA { get; } = prisonera;
	public PrisonerModel PrisonerB { get; } = prisonerb;

	public DateTime CreatedAt { get; } = DateTime.UtcNow;
	public double LifetimeSeconds { get; } = lifetimeSeconds;
}