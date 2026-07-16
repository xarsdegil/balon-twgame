using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BalloonGame
{
    public static class TwitchTokenStore
    {
        private const string FileName = "twitch-session.dat";
        private const string ClientIdKey = "BalloonGame.TwitchClientId";

        private static string SessionPath => Path.Combine(Application.persistentDataPath, FileName);

        public static string LoadRememberedClientId()
        {
            return PlayerPrefs.GetString(ClientIdKey, string.Empty);
        }

        public static void RememberClientId(string clientId)
        {
            PlayerPrefs.SetString(ClientIdKey, (clientId ?? string.Empty).Trim());
            PlayerPrefs.Save();
        }

        public static void Save(TwitchAuthSession session)
        {
            if (session == null)
                return;

            string json = JsonUtility.ToJson(session);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);
            byte[] storedBytes = Protect(plainBytes);
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllBytes(SessionPath, storedBytes);
            RememberClientId(session.clientId);
        }

        public static bool TryLoad(out TwitchAuthSession session)
        {
            session = null;
            try
            {
                if (!File.Exists(SessionPath))
                    return false;

                byte[] storedBytes = File.ReadAllBytes(SessionPath);
                byte[] plainBytes = Unprotect(storedBytes);
                session = JsonUtility.FromJson<TwitchAuthSession>(Encoding.UTF8.GetString(plainBytes));
                return session != null && session.HasAccessToken;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Saved Twitch session could not be read: " + exception.Message);
                Delete();
                return false;
            }
        }

        public static void Delete()
        {
            if (File.Exists(SessionPath))
                File.Delete(SessionPath);
        }

        private static byte[] Protect(byte[] value)
        {
            using Aes aes = Aes.Create();
            aes.Key = CreateLocalKey();
            aes.GenerateIV();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] cipher = encryptor.TransformFinalBlock(value, 0, value.Length);
            byte[] result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
            return result;
        }

        private static byte[] Unprotect(byte[] value)
        {
            using Aes aes = Aes.Create();
            aes.Key = CreateLocalKey();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            byte[] iv = new byte[aes.BlockSize / 8];
            if (value.Length <= iv.Length)
                throw new CryptographicException("Saved session is incomplete.");

            Buffer.BlockCopy(value, 0, iv, 0, iv.Length);
            aes.IV = iv;
            using ICryptoTransform decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(value, iv.Length, value.Length - iv.Length);
        }

        private static byte[] CreateLocalKey()
        {
            string material = SystemInfo.deviceUniqueIdentifier + "|" +
                              Application.companyName + "|" +
                              Application.productName + "|BalloonGame.Twitch";
            using SHA256 sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(material));
        }
    }
}
