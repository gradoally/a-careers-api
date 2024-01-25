using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
	public class OrderContent
	{
		private static readonly string PropName = EncodePropertyName("name");
		private static readonly string PropDescription = EncodePropertyName("description");
		private static readonly string PropPrice = EncodePropertyName("price");
		private static readonly string PropDeadline = EncodePropertyName("deadline");
		private static readonly string PropTechnicalTask = EncodePropertyName("technical_task");
		private static readonly string PropCategory = EncodePropertyName("category");
		private static readonly string PropLanguage = EncodePropertyName("language");

		public string? Category { get; set; }

		public string? Language { get; set; }

		public string? Name { get; set; }

		public decimal Price { get; set; }

		public DateTimeOffset Deadline { get; set; }

		public string? Description { get; set; }

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
