using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Octgn.Core;
using Octgn.Core.Util;
using Org.BouncyCastle.Crypto.Digests;

namespace Octgn.Extentions
{
    using log4net;


    public static partial class StringExtensionMethods
    {
        public static string Decrypt(this string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // Użycie BouncyCastle dla RIPEMD160
            var hash = new RipeMD160Digest();
            var un = (Prefs.Username ?? string.Empty).Clone() as string;
            byte[] input = Encoding.Unicode.GetBytes(un);
        
            // Oblicz hash
            hash.BlockUpdate(input, 0, input.Length);
            byte[] hasher = new byte[hash.GetDigestSize()];
            hash.DoFinal(hasher, 0);

            text = Cryptor.Decrypt(text, BitConverter.ToString(hasher));
            return text;
        }

        public static string Encrypt(this string text)
        {
            // Użycie BouncyCastle dla RIPEMD160
            var hash = new RipeMD160Digest();
            var un = (Prefs.Username ?? string.Empty).Clone() as string;
            byte[] input = Encoding.Unicode.GetBytes(un);
        
            // Oblicz hash
            hash.BlockUpdate(input, 0, input.Length);
            byte[] hasher = new byte[hash.GetDigestSize()];
            hash.DoFinal(hasher, 0);

            return Cryptor.Encrypt(text, BitConverter.ToString(hasher));
        }

		/// <summary>
		/// Provides a cleaner method of string concatenation. (i.e. "Name {0}".With(firstName)
		/// </summary>
		public static string With(this string input, params object[] args)
		{
			return string.Format(input, args);
		}

        public static string Sha1(this string text)
        {
            var buffer = Encoding.Default.GetBytes(text);
            var cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            return BitConverter.ToString(cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");
        }

        public static void SetLastPythonFunction(this ILog log, string function)
        {
            GlobalContext.Properties["lastpythonfunction"] = function;
        }

        public static void SetUserName(this ILog log, string username)
        {
            GlobalContext.Properties["username"] = username;
        }

        public static void SetRunningGame(this ILog log, string gameName, Guid gameId, Version gameVersion)
        {
            GlobalContext.Properties["gameName"] = gameName;
            GlobalContext.Properties["gameId"] = gameId;
            GlobalContext.Properties["gameVersion"] = gameVersion;
        }

        public static int ToInt(this Guid guid)
        {
            return guid.ToByteArray().Aggregate(0, (current, b) => current + b*2);
        }
    }
}
