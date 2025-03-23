using System;

using Unity.Entities;

using static PrisonerExchange.Models.PrisonerModel;

namespace PrisonerExchange.Models;

public class PendingSale
{
	public UserModel Seller { get; }
	public UserModel Buyer { get; }
	public Entity PrisonerEntity { get; }
	public int Price { get; }

	public DateTime CreatedAt { get; }
	public double LifetimeSeconds { get; }

	public PendingSale(UserModel seller, Entity prisonerEntity, UserModel buyer, int price, double lifetimeSeconds = 120)
	{
		Seller = seller;
		Buyer = buyer;
		PrisonerEntity = prisonerEntity;
		Price = price;

		CreatedAt = DateTime.UtcNow;
		LifetimeSeconds = lifetimeSeconds;
	}

	public PrisonerInformation GetPrisonerInformation
	{
		get
		{
			var prisoner = new PrisonerModel(PrisonerEntity);
			return prisoner.Info;
		}
	}
}