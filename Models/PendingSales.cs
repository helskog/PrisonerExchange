using System;

using Unity.Entities;

using static PrisonerExchange.Models.PrisonerModel;

namespace PrisonerExchange.Models;

public class PendingSale(
	UserModel seller,
	Entity prisonerEntity,
	UserModel buyer,
	int price,
	double lifetimeSeconds = 120)
{
	public UserModel Seller { get; } = seller;
	public UserModel Buyer { get; } = buyer;
	public Entity PrisonerEntity { get; } = prisonerEntity;
	public int Price { get; } = price;

	public DateTime CreatedAt { get; } = DateTime.UtcNow;
	public double LifetimeSeconds { get; } = lifetimeSeconds;

	public PrisonerInformation GetPrisonerInformation
	{
		get
		{
			var prisoner = new PrisonerModel(PrisonerEntity);
			return prisoner.Info;
		}
	}
}