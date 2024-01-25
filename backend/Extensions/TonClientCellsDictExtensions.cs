using TonLibDotNet.Cells;
using TonLibDotNet.Internal;

namespace TonLibDotNet
{
	/// <summary>
	/// LoadDict and StoreDict implementations.
	/// </summary>
	/// <remarks>
	/// TL-B schema for Hashmap (HashmapE) from https://docs.ton.org/develop/data-formats/tl-b-types#hashmap:
	/// <![CDATA[
	///
	///    hm_edge#_ {n:#} {X:Type} {l:#} {m:#} label:(HmLabel ~l n)
	///                {n = (~m) + l} node:(HashmapNode m X) = Hashmap n X;
	///
	///    hmn_leaf#_ {X:Type} value:X = HashmapNode 0 X;
	///    hmn_fork#_ {n:#} {X:Type} left:^(Hashmap n X)
	///                right:^(Hashmap n X) = HashmapNode (n + 1) X;
	///
	///    hml_short$0 {m:#} {n:#} len:(Unary ~n) {n <= m} s:(n * Bit) = HmLabel ~n m;
	///    hml_long$10 {m:#} n:(#<= m) s:(n * Bit) = HmLabel ~n m;
	///    hml_same$11 {m:#} v:Bit n:(#<= m) = HmLabel ~n m;
	///
	///    unary_zero$0 = Unary ~0;
	///    unary_succ$1 {n:#} x:(Unary ~n) = Unary ~(n + 1);
	///
	///    hme_empty$0 {n:#} {X:Type} = HashmapE n X;
	///    hme_root$1 {n:#} {X:Type} root:^(Hashmap n X) = HashmapE n X;
	///
	/// ]]>
	/// </remarks>
	/// <seealso href="https://docs.ton.org/develop/data-formats/tl-b-types#hashmap">Hashmap (HashmapE) type in TL-B.</seealso>
	public static class TonClientCellsDictExtensions
	{
		/// <summary>
		/// Chains <see cref="LoadDict"/> and <see cref="ParseDict"/> calls and returns actual dict with data.
		/// </summary>
		/// <remarks>
		/// Sometimes <see href="https://github.com/ton-blockchain/dns-contract/blob/main/func/nft-item.fc#L21">arbitrary cells are stored using store_dict()</see>, so not every <see cref="LoadDict"/> result should be parsed.
		/// </remarks>
		/// <exception cref="InvalidOperationException">Dictionary is empty</exception>
		public static Dictionary<TKey, TValue> LoadAndParseDict<TKey, TValue>(this Slice slice, int keyBitLength, Func<Slice, TKey> keyReader, Func<Slice, TValue> valueReader, IEqualityComparer<TKey>? comparer = null)
			where TKey : notnull
		{
			var cell = slice.LoadDict();
			return ParseDict(cell, keyBitLength, keyReader, valueReader, comparer);
		}

		/// <summary>
		/// Chains <see cref="TryLoadDict"/> and <see cref="ParseDict"/> calls and returns actual dict with data (or null).
		/// </summary>
		/// <remarks>
		/// Sometimes <see href="https://github.com/ton-blockchain/dns-contract/blob/main/func/nft-item.fc#L21">arbitrary cells are stored using store_dict()</see>, so not every <see cref="TryLoadDict"/> result should be parsed.
		/// </remarks>
		public static Dictionary<TKey, TValue>? TryLoadAndParseDict<TKey, TValue>(this Slice slice, int keyBitLength, Func<Slice, TKey> keyReader, Func<Slice, TValue> valueReader, IEqualityComparer<TKey>? comparer = null)
			where TKey : notnull
		{
			var cell = slice.TryLoadDict();
			return cell == null ? null : ParseDict(cell, keyBitLength, keyReader, valueReader, comparer);
		}

		/// <summary>
		/// Parses dictionary from Cell (previously loaded by <see cref="LoadDict(Slice)"/>).
		/// </summary>
		public static Dictionary<TKey, TValue> ParseDict<TKey, TValue>(this Cell cell, int keyBitLength, Func<Slice, TKey> keyReader, Func<Slice, TValue> valueReader, IEqualityComparer<TKey>? comparer = null)
			where TKey : notnull
		{
			var items = new List<(Slice key, Slice value)>();
			ParseDictImpl(cell, Array.Empty<bool>(), keyBitLength, items);

			return items.ToDictionary(
				x =>
				{
					var val = keyReader(x.key);
					x.key.EndRead();
					return val;
				},
				x =>
				{
					var valueSrc = x.value.TryCanLoad(1) ? x.value : x.value.LoadRef().BeginRead();
					var val = valueReader(valueSrc);
					if (val is not Slice)
					{
						x.value.EndRead();
					}
					return val;
				},
				comparer);
		}

		private static void ParseDictImpl(Cell edge, ReadOnlySpan<bool> keySoFar, int maxBitsForKey, List<(Slice key, Slice value)> items)
		{
			var source = edge.BeginRead();

			Span<bool> label = stackalloc bool[0];

			var lbl1 = source.LoadBit();
			if (!lbl1) // htl_short$0
			{
				var len = 0;
				while (source.LoadBit())
				{
					len++;
				}

				if (len > 0)
				{
					label = stackalloc bool[len];
					source.LoadBitsTo(label);
				}
			}
			else
			{
				var n_len = (int)Math.Ceiling(Math.Log2(maxBitsForKey + 1));
				var lbl2 = source.LoadBit();
				if (lbl2) // hml_same$11
				{
					var v = source.LoadBit();
					var n = source.LoadInt(n_len);
					label = stackalloc bool[n];
					label.Fill(v);
				}
				else // hml_long$10
				{
					var n = source.LoadInt(n_len);
					label = stackalloc bool[n];
					source.LoadBitsTo(label);
				}
			}

			var label_left = maxBitsForKey - label.Length;

			if (label_left == 0)
			{
				var key = new bool[keySoFar.Length + maxBitsForKey];
				keySoFar.CopyTo(key);
				label.CopyTo(key.AsSpan(keySoFar.Length));
				items.Add((new Slice(key), source));
			}
			else
			{
				Span<bool> key = stackalloc bool[keySoFar.Length + label.Length + 1];
				keySoFar.CopyTo(key);
				label.CopyTo(key[keySoFar.Length..]);
				key[^1] = false;
				ParseDictImpl(source.LoadRef(), key, label_left - 1, items);

				key[^1] = true;
				ParseDictImpl(source.LoadRef(), key, label_left - 1, items);
			}
		}
	}
}
