using System.Diagnostics.CodeAnalysis;
using TonLibDotNet.Utils;

namespace TonLibDotNet
{
    public static class TonUtilsExtensions
    {
        /// <summary>
        /// Validates address, and returns same address but with 'bounceable' flag to required value.
        /// </summary>
        /// <param name="address">Address to validate and convert.</param>
        /// <param name="bounceable">Required value of 'bounceable' flag.</param>
        /// <param name="result">Updated address.</param>
        /// <returns>Returns <b>true</b> if source <paramref name="address"/> is valid (and <paramref name="result"/> is set), and <b>false</b> if <paramref name="address"/> is not valid.</returns>
        /// <remarks>When source <paramref name="address"/> already have required 'bounceable' flag value - it will be returned unchanged after validation.</remarks>
        public static bool TrySetBounceable(this AddressUtils utils, string address, bool bounceable, [NotNullWhen(true)] out string result)
        {
            if (!AddressValidator.TryParseAddress(address, out var workchainId, out var accountId, out var bounceableOld, out var testnetOnly, out var urlSafe))
            {
                result = string.Empty;
                return false;
            }

            result = bounceable == bounceableOld ? address : AddressValidator.MakeAddress(workchainId, accountId, bounceable, testnetOnly, urlSafe);
            return true;
        }
    }
}
