using VampireCommandFramework;

namespace PrisonerExchange.Commands;

public class SwapCommands
{
	/// <summary>
	/// Initiate a swap request to another user.
	/// </summary>
	[Command("pe swap", description: "Swap a prisoner for another")]
	public static void ExchangeCommand()
	{
	}

	/// <summary>
	/// Accept swap request
	/// </summary>
	[Command("pe swap accept", description: "Accept incoming prisoner swap request")]
	public static void AcceptSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap decline", description: "Decline incoming prisoner swap request")]
	public static void DeclineSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap cancel", description: "Cancel your outgoing prisoner swap request")]
	public static void CancelSwap(ChatCommandContext ctx)
	{
	}

	[Command("pe swap list", description: "List all active prisoner swaps", adminOnly: true)]
	public void ListSwaps(ChatCommandContext ctx)
	{
	}

	[Command("pe swap remove", description: "Remove a specific user’s swap request", adminOnly: true)]
	public void RemoveSwap(ChatCommandContext ctx, string fromUser = "username")
	{
	}

	[Command("pe swap clear", description: "Clear all active prisoner swaps", adminOnly: true)]
	public void ClearSwaps(ChatCommandContext ctx)
	{
	}
}