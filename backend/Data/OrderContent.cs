using System.Text.Json.Serialization;
using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
	public class OrderContent
	{
		private const string PropNameName = "name";
		private const string PropNameDescription = "description";
		private const string PropNamePrice = "price";
		private const string PropNameDeadline = "deadline";
		private const string PropNameTechnicalTask = "technical_task";
		private const string PropNameCategory = "category";
		private const string PropNameLanguage = "language";

		private static readonly string PropName = EncodePropertyName(PropNameName);
		private static readonly string PropDescription = EncodePropertyName(PropNameDescription);
		private static readonly string PropPrice = EncodePropertyName(PropNamePrice);
		private static readonly string PropDeadline = EncodePropertyName(PropNameDeadline);
		private static readonly string PropTechnicalTask = EncodePropertyName(PropNameTechnicalTask);
		private static readonly string PropCategory = EncodePropertyName(PropNameCategory);
		private static readonly string PropLanguage = EncodePropertyName(PropNameLanguage);

		[JsonPropertyName(PropNameCategory)]
		public string? Category { get; set; }

		[JsonPropertyName(PropNameLanguage)]
		public string? Language { get; set; }

		[JsonPropertyName(PropNameName)]
		public string? Name { get; set; }

		[JsonPropertyName(PropNamePrice)]
		public decimal Price { get; set; }

		[JsonPropertyName(PropNameDeadline)]
		public DateTimeOffset Deadline { get; set; }

		[JsonPropertyName(PropNameDescription)]
		public string? Description { get; set; }

		[JsonPropertyName(PropNameTechnicalTask)]
		public string? TechnicalTask { get; set; }

		public void FillFrom(Cell dictCell)
		{
			var dict = dictCell.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
			if (dict == null)
			{
				return;
			}

			if (dict.TryGetValue(PropCategory, out var s))
			{
				Category = "0x" + Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
			}

			if (dict.TryGetValue(PropLanguage, out s))
			{
				Language = "0x" + Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
			}

			if (dict.TryGetValue(PropName, out s))
			{
				Name = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropPrice, out s))
			{
				Price = TonUtils.Coins.FromNano(s.LoadCoins());
			}

			if (dict.TryGetValue(PropDeadline, out s))
			{
				var d = s.LoadUInt(32);
				Deadline = DateTimeOffset.FromUnixTimeSeconds(d);
			}

			if (dict.TryGetValue(PropDescription, out s))
			{
				Description = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropTechnicalTask, out s))
			{
				TechnicalTask = s.LoadStringSnake(true) ?? string.Empty;
			}
		}

		protected static string EncodePropertyName(string name)
		{
			return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(name)));
		}
	}
}
