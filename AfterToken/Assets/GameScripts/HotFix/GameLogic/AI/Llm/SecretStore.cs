using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TEngine;
using UnityEngine;

namespace GameLogic.AI.Llm
{
    /// <summary>
    /// 本地密钥存储：AES-256-CBC 加密，密钥由 PBKDF2 派生
    /// （SystemInfo.deviceUniqueIdentifier + 应用盐 + productName）。
    /// 单套代码跨 Windows/Android——不用 DPAPI/Keychain 等平台 API。
    /// 密文格式：Magic + Base64( salt[16] | iv[16] | ciphertext )。
    /// deviceUniqueIdentifier 失效（刷机/换机）时解密失败按"未配置"处理，不闪退。
    /// </summary>
    public static class SecretStore
    {
        /// <summary>密文行首魔数，用于区分密文与旧明文 JSON。</summary>
        public const string Magic = "ATENC1:";

        /// <summary>应用盐：公开常量，仅用于把设备 ID 与别的应用隔离，不构成安全边界。</summary>
        private const string AppSalt = "AfterToken.LlmConfig.v1";
        private const int Pbkdf2Iterations = 10000;
        private const int KeySize = 32;
        private const int SaltSize = 16;
        private const int IvSize = 16;

        /// <summary>当前设备能否派生密钥（编辑器/真机一般都可用）。</summary>
        public static bool IsAvailable
        {
            get
            {
                try
                {
                    return !string.IsNullOrEmpty(SystemInfo.deviceUniqueIdentifier)
                           && SystemInfo.deviceUniqueIdentifier != SystemInfo.unsupportedIdentifier;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>加密一段文本为带魔数的密文行。失败返回 null（不抛异常）。</summary>
        public static string Encrypt(string plainText)
        {
            if (plainText == null || !IsAvailable)
            {
                return null;
            }

            try
            {
                byte[] key = DeriveKey();
                byte[] salt = RandomBytes(SaltSize);
                byte[] iv = RandomBytes(IvSize);

                using var aes = Aes.Create();
                aes.Key = XorKey(key, salt); // salt 混入密钥，等效 per-record 盐
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor();
                byte[] cipher = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(plainText), 0, Encoding.UTF8.GetByteCount(plainText));

                byte[] packed = new byte[SaltSize + IvSize + cipher.Length];
                Buffer.BlockCopy(salt, 0, packed, 0, SaltSize);
                Buffer.BlockCopy(iv, 0, packed, SaltSize, IvSize);
                Buffer.BlockCopy(cipher, 0, packed, SaltSize + IvSize, cipher.Length);
                return Magic + Convert.ToBase64String(packed);
            }
            catch (Exception e)
            {
                Log.Warning($"[SecretStore] 加密失败: {e.Message}");
                return null;
            }
        }

        /// <summary>解密密文行（须带魔数）。失败返回 null（不抛异常）。</summary>
        public static string Decrypt(string stored)
        {
            if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Magic, StringComparison.Ordinal) || !IsAvailable)
            {
                return null;
            }

            try
            {
                byte[] packed = Convert.FromBase64String(stored.Substring(Magic.Length));
                if (packed.Length < SaltSize + IvSize + 16)
                {
                    return null;
                }

                byte[] salt = new byte[SaltSize];
                byte[] iv = new byte[IvSize];
                byte[] cipher = new byte[packed.Length - SaltSize - IvSize];
                Buffer.BlockCopy(packed, 0, salt, 0, SaltSize);
                Buffer.BlockCopy(packed, SaltSize, iv, 0, IvSize);
                Buffer.BlockCopy(packed, SaltSize + IvSize, cipher, 0, cipher.Length);

                byte[] key = DeriveKey();
                using var aes = Aes.Create();
                aes.Key = XorKey(key, salt);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor();
                byte[] plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
                return Encoding.UTF8.GetString(plain);
            }
            catch (Exception)
            {
                // 换机/刷机/数据损坏统一按解密失败处理
                return null;
            }
        }

        /// <summary>判断存储内容是否为密文。</summary>
        public static bool IsEncrypted(string stored) =>
            !string.IsNullOrEmpty(stored) && stored.StartsWith(Magic, StringComparison.Ordinal);

        private static byte[] DeriveKey()
        {
            string material = SystemInfo.deviceUniqueIdentifier + "|" + AppSalt + "|" + Application.productName;
            using var pbkdf2 = new Rfc2898DeriveBytes(
                material,
                Encoding.UTF8.GetBytes(AppSalt),
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(KeySize);
        }

        private static byte[] XorKey(byte[] key, byte[] salt)
        {
            // 把 per-record salt 混进派生密钥：同一设备不同记录密钥不同
            byte[] mixed = new byte[key.Length];
            for (int i = 0; i < key.Length; i++)
            {
                mixed[i] = (byte)(key[i] ^ salt[i % salt.Length]);
            }
            return mixed;
        }

        private static byte[] RandomBytes(int count)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] bytes = new byte[count];
            rng.GetBytes(bytes);
            return bytes;
        }
    }
}
