namespace SomeDAO.Backend.Data
{
	public interface IUserContent
	{
		bool IsUser { get; set; }

		bool IsFreelancer { get; set; }

		string? Nickname { get; set; }

		string? Telegram { get; set; }

		string? About { get; set; }

		string? Website { get; set; }

		string? Portfolio { get; set; }

		string? Resume { get; set; }

		string? Specialization { get; set; }
	}
}
