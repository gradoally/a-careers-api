namespace SomeDAO.Backend.Data
{
	public interface IBlockchainEntity
	{
		long Index { get; }

		string Address { get; }

		EntityType EntityType { get; }

		long LastTxLt { get; set; }

		string? LastTxHash { get; set; }

		DateTimeOffset LastSync { get; set; }
	}
}