namespace SomeDAO.Backend.Data
{
	public interface IOrderContent
	{
		string? Category { get; set; }

		string? Language { get; set; }

		string? Name { get; set; }

		decimal Price { get; set; }

		DateTimeOffset Deadline { get; set; }

		string? Description { get; set; }

		string? TechnicalTask { get; set; }
	}
}
