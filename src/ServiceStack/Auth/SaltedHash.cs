using System;
using System.Security.Cryptography;
using System.Text;
using ServiceStack.Text;

namespace ServiceStack.Auth
{
    public interface IHashProvider
    {
        void GetHashAndSaltString(string data, out string hash, out string salt);
        bool VerifyHashString(string data, string hash, string salt);
    }

    /// <summary>
    /// Thank you Martijn
    /// http://www.dijksterhuis.org/creating-salted-hash-values-in-c/
    /// 
    /// Stronger/Slower Alternative: 
    /// https://github.com/defuse/password-hashing/blob/master/PasswordStorage.cs
    /// </summary>
    public class SaltedHash : IHashProvider
    {
        public readonly HashAlgorithm HashProvider;
        public readonly int SalthLength;

        public SaltedHash(HashAlgorithm hashAlgorithm, int saltLength)
        {
            HashProvider = hashAlgorithm;
            SalthLength = saltLength;
        }

        public SaltedHash() : this(SHA256.Create(), 4) { }

        private byte[] ComputeHash(byte[] data, byte[] salt)
        {
            var dataAndSalt = new byte[data.Length + SalthLength];
            Array.Copy(data, dataAndSalt, data.Length);
            Array.Copy(salt, 0, dataAndSalt, data.Length, SalthLength);

            return HashProvider.ComputeHash(dataAndSalt);
        }

        public void GetHashAndSalt(byte[] data, out byte[] hash, out byte[] salt)
        {
            salt = new byte[SalthLength];

            var random = RandomNumberGenerator.Create();
#if !NETSTANDARD2_0
            random.GetNonZeroBytes(salt);
#else
            random.GetBytes(salt);
#endif

            hash = ComputeHash(data, salt);
        }

        public void GetHashAndSaltString(string data, out string hash, out string salt)
        {
            GetHashAndSalt(Encoding.UTF8.GetBytes(data), out var hashOut, out var saltOut);

            hash = Convert.ToBase64String(hashOut);
            salt = Convert.ToBase64String(saltOut);
        }

        public bool VerifyHash(byte[] data, byte[] hash, byte[] salt)
        {
            var newHash = ComputeHash(data, salt);

            if (newHash.Length != hash.Length) return false;

            for (int Lp = 0; Lp < hash.Length; Lp++)
                if (!hash[Lp].Equals(newHash[Lp]))
                    return false;

            return true;
        }

        public bool VerifyHashString(string data, string hash, string salt)
        {
            if (hash == null || salt == null)
                return false;
            
            byte[] HashToVerify = Convert.FromBase64String(hash);
            byte[] SaltToVerify = Convert.FromBase64String(salt);
            byte[] DataToVerify = Encoding.UTF8.GetBytes(data);
            return VerifyHash(DataToVerify, HashToVerify, SaltToVerify);
        }
    }

    public static class HashExtensions
    {
        public static string ToSha256Hash(this string value)
        {
            var sb = new StringBuilder();
            using (var hash = SHA256.Create())
            {
                var result = hash.ComputeHash(value.ToUtf8Bytes());
                foreach (var b in result)
                {
                    sb.Append(b.ToString("x2"));
                }
            }
            return sb.ToString();
        }

        public static byte[] ToSha256HashBytes(this byte[] bytes)
        {
            using (var hash = SHA256.Create())
            {
                return hash.ComputeHash(bytes);
            }
        }

        public static byte[] ToSha512HashBytes(this byte[] bytes)
        {
            using (var hash = SHA512.Create())
            {
                return hash.ComputeHash(bytes);
            }
        }
    }

    /*
    /// <summary>
    /// This little demo code shows how to encode a users password.
    /// </summary>
    class SaltedHashDemo
    {
        public static void Main(string[] args)
        {
            // We use the default SHA-256 & 4 byte length
            SaltedHash demo = new SaltedHash();

            // We have a password, which will generate a Hash and Salt
            string Password = "MyGlook234";
            string Hash;
            string Salt;

            demo.GetHashAndSaltString(Password, out Hash, out Salt);
            Console.WriteLine("Password = {0} , Hash = {1} , Salt = {2}", Password, Hash, Salt);

            // Password validation
            //
            // We need to pass both the earlier calculated Hash and Salt (we need to store this somewhere safe between sessions)

            // First check if a wrong password passes
            string WrongPassword = "OopsOops";
            Console.WriteLine("Verifying {0} = {1}", WrongPassword, demo.VerifyHashString(WrongPassword, Hash, Salt));

            // Check if the correct password passes
            Console.WriteLine("Verifying {0} = {1}", Password, demo.VerifyHashString(Password, Hash, Salt));
        }	 
    }
 */

}