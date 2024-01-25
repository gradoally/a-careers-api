using System.Globalization;
using System.Numerics;
using System.Text.Json.Serialization;
using TonLibDotNet;
using TonLibDotNet.Cells;

namespace SomeDAO.Backend.Data
{
	public class AdminContent
	{
		private const string PropNameCategory = "category";
		private const string PropNameCanApproveUser = "can_approve_user";
		private const string PropNameCanRevokeUser = "can_revoke_user";
		private const string PropNameNickname = "nickname";
		private const string PropNameAbout = "about";
		private const string PropNameWebsite = "website";
		private const string PropNamePortfolio = "portfolio";
		private const string PropNameResume = "resume";
		private const string PropNameSpecialization = "specialization";

		private static readonly string PropCategory = EncodePropertyName(PropNameCategory);
		private static readonly string PropCanApproveUser = EncodePropertyName(PropNameCanApproveUser);
		private static readonly string PropCanRevokeUser = EncodePropertyName(PropNameCanRevokeUser);
		private static readonly string PropNickname = EncodePropertyName(PropNameNickname);
		private static readonly string PropAbout = EncodePropertyName(PropNameAbout);
		private static readonly string PropWebsite = EncodePropertyName(PropNameWebsite);
		private static readonly string PropPortfolio = EncodePropertyName(PropNamePortfolio);
		private static readonly string PropResume = EncodePropertyName(PropNameResume);
		private static readonly string PropSpecialization = EncodePropertyName(PropNameSpecialization);

		[JsonPropertyName(PropNameCategory)]
		public string? Category { get; set; }

		[JsonPropertyName(PropNameCanApproveUser)]
		public bool CanApproveUser { get; set; }

		[JsonPropertyName(PropNameCanRevokeUser)]
		public bool CanRevokeUser { get; set; }

		[JsonPropertyName(PropNameNickname)]
		public string? Nickname { get; set; }

		[JsonPropertyName(PropNameAbout)]
		public string? About { get; set; }

		[JsonPropertyName(PropNameWebsite)]
		public string? Website { get; set; }

		[JsonPropertyName(PropNamePortfolio)]
		public string? Portfolio { get; set; }

		[JsonPropertyName(PropNameResume)]
		public string? Resume { get; set; }

		[JsonPropertyName(PropNameSpecialization)]
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
