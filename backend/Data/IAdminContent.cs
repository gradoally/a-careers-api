namespace SomeDAO.Backend.Data
{
	public interface IAdminContent
	{
		string? Category { get; set; }

		bool CanApproveUser { get; set; }

		bool CanRevokeUser { get; set; }

		string? Nickname { get; set; }

		string? About { get; set; }

		string? Website { get; set; }

		string? Portfolio { get; set; }

		string? Resume { get; set; }

		string? Specialization { get; set; }
	}
}
