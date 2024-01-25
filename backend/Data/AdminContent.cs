using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
	public class AdminContent
	{
		private static readonly string PropCategory = EncodePropertyName("category");
		private static readonly string PropCanApproveUser = EncodePropertyName("can_approve_user");
		private static readonly string PropCanRevokeUser = EncodePropertyName("can_revoke_user");
		private static readonly string PropNickname = EncodePropertyName("nickname");
		private static readonly string PropAbout = EncodePropertyName("about");
		private static readonly string PropWebsite = EncodePropertyName("website");
		private static readonly string PropPortfolio = EncodePropertyName("portfolio");
		private static readonly string PropResume = EncodePropertyName("resume");
		private static readonly string PropSpecialization = EncodePropertyName("specialization");

		public string? Category { get; set; }

		public bool CanApproveUser { get; set; }

		public bool CanRevokeUser { get; set; }

		public string? Nickname { get; set; }

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

			if (dict.TryGetValue(PropCategory, out var s))
			{
				Category = "0x" + Convert.ToHexString(s.LoadBitsToBytes(256)).ToLowerInvariant();
			}

			if (dict.TryGetValue(PropCanApproveUser, out s))
			{
				CanApproveUser = s.LoadBit();
			}

			if (dict.TryGetValue(PropCanRevokeUser, out s))
			{
				CanRevokeUser = s.LoadBit();
			}

			if (dict.TryGetValue(PropNickname, out s))
			{
				Nickname = s.LoadStringSnake(true) ?? string.Empty;
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
