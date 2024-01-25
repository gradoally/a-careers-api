using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
	public class UserContent
	{
		private static readonly string PropIsUser = EncodePropertyName("is_user");
		private static readonly string PropIsFreelancer = EncodePropertyName("is_freelancer");
		private static readonly string PropNickname = EncodePropertyName("nickname");
		private static readonly string PropTelegram = EncodePropertyName("telegram");
		private static readonly string PropAbout = EncodePropertyName("about");
		private static readonly string PropWebsite = EncodePropertyName("website");
		private static readonly string PropPortfolio = EncodePropertyName("portfolio");
		private static readonly string PropResume = EncodePropertyName("resume");
		private static readonly string PropSpecialization = EncodePropertyName("specialization");

		public bool IsUser { get; set; }

		public bool IsFreelancer { get; set; }

		public string? Nickname { get; set; }

		public string? Telegram { get; set; }

		public string? About { get; set; }

		public string? Website { get; set; }

		public string? Portfolio { get; set; }

		public string? Resume { get; set; }

		public string? Specialization { get; set; }

		public void FillFrom(Cell dictCell)
		{
			var dict = dictCell.ParseDict(256, x => Convert.ToHexString(x.LoadBitsToBytes(256)), x => x, StringComparer.Ordinal);
			if (dict == null)
			{
				return;
			}

			if (dict.TryGetValue(PropIsUser, out var s))
			{
				IsUser = s.LoadBit();
			}

			if (dict.TryGetValue(PropIsFreelancer, out s))
			{
				IsFreelancer = s.LoadBit();
			}

			if (dict.TryGetValue(PropNickname, out s))
			{
				Nickname = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropTelegram, out s))
			{
				Telegram = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropAbout, out s))
			{
				About = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropWebsite, out s))
			{
				Website = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropPortfolio, out s))
			{
				Portfolio = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropResume, out s))
			{
				Resume = s.LoadStringSnake(true) ?? string.Empty;
			}

			if (dict.TryGetValue(PropSpecialization, out s))
			{
				Specialization = s.LoadStringSnake(true) ?? string.Empty;
			}
		}

		private static string EncodePropertyName(string name)
		{
			return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(name)));
		}
	}
}
