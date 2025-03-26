using System.Collections.Generic;
using System.Text;

using PrisonerExchange.Models;

using ProjectM;

using VampireCommandFramework;

namespace PrisonerExchange.Services.Chat;

public class StringBuilders
{
	public static void AnnounceSale(PendingSale sale)
	{
		var prisonerInfo = sale.GetPrisonerInformation;

		var msg = $"{Markup.Prefix}{sale.Buyer.CharacterName} has purchased {prisonerInfo.UnitType} with {prisonerInfo.BloodQuality}%" +
						$"{prisonerInfo.BloodType} from {sale.Seller.CharacterName}!";
		ServerChatUtils.SendSystemMessageToAllClients(Core.EntityManager, msg);
	}

	public static string SalesInfoMessage(PendingSale sale)
	{
		var prisonerInfo = sale.GetPrisonerInformation;

		var sb = new StringBuilder();
		sb.AppendLine($"<size=15><color=yellow>Prisoner sale request</color></size>");
		sb.AppendLine($"From user: {Markup.Highlight(sale.Seller.CharacterName)}");
		sb.AppendLine($"Price: {Markup.Highlight(sale.Price)} {Configuration.CurrencyName}");
		sb.AppendLine($"Prisoner type: {prisonerInfo.UnitType}");
		sb.AppendLine($"Prisoner blood: {Markup.Highlight($"{prisonerInfo.BloodQuality}% {prisonerInfo.BloodType}")}");
		sb.AppendLine();
		sb.AppendLine($"Type {Markup.Highlight(".pe acceptsale")} while standing next to an empty prison cell.");

		return sb.ToString();
	}

	public static string SwapInfoMessage(PendingSwap swap)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"<size=15><color=yellow>Prisoner swap request</color></size>");
		sb.AppendLine($"From user: {Markup.Highlight(swap.Seller.CharacterName)}");
		sb.AppendLine($"Offering: {Markup.Highlight($"{swap.PrisonerB.Info.UnitType}")} " + Markup.Secondary($"{swap.PrisonerB.Info.BloodQuality}% {swap.PrisonerB.Info.BloodType}"));
		sb.AppendLine($"For your: {Markup.Highlight($"{swap.PrisonerA.Info.UnitType}")} " + Markup.Secondary($"{swap.PrisonerA.Info.BloodQuality}% {swap.PrisonerA.Info.BloodType}"));
		sb.AppendLine();
		sb.AppendLine($"Type {Markup.Highlight(".pe acceptswap")} to complete the swap.");

		return sb.ToString();
	}

	/// <summary>
	/// im so sorry
	/// </summary>

	public static void SendPrisonerList(ChatCommandContext ctx, List<PrisonerModel> list)
	{
		const int maxBytes = 512;
		var sb = new StringBuilder();
		int currentBytes = 0;
		bool isFirstBatch = true;

		void Flush()
		{
			if (sb.Length > 0)
			{
				ctx.Reply(sb.ToString());
				sb.Clear();
				currentBytes = 0;
				isFirstBatch = false;
			}
		}

		sb.AppendLine();
		currentBytes += Encoding.UTF8.GetByteCount("\n");

		for (int i = 0; i < list.Count; i++)
		{
			var info = list[i].Info;

			string bloodInfo = $"{info.BloodQuality}% {info.BloodType}";

			string line = $"[{Markup.Secondary(i + 1)}] {Markup.Highlight(info.UnitType)} {Markup.Secondary(bloodInfo)}";
			int lineBytes = Encoding.UTF8.GetByteCount(line + "\n");

			if (currentBytes + lineBytes >= maxBytes)
			{
				Flush();
				if (!isFirstBatch)
				{
					sb.AppendLine(); // newline before next batch
					currentBytes += Encoding.UTF8.GetByteCount("\n");
				}
			}

			sb.AppendLine(line);
			currentBytes += lineBytes;
		}

		string footer = $"{Markup.Secondary("Select")}{Markup.Highlight(" a prisoner by typing a number in chat")} ({Markup.Secondary("!s to cancel")})";
		int footerBytes = Encoding.UTF8.GetByteCount(footer + "\n");

		if (currentBytes + footerBytes >= maxBytes)
			Flush();

		sb.AppendLine();
		sb.AppendLine(footer);
		Flush();
	}
}