using System.Security.Cryptography;
using System.Text;

namespace CryptMail.Additional
{
    public static class CoderHelper
    {
        /*private static readonly byte[] IV = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };*/



        /*public static string Encoder1(string message, string key)
         {   //  AES

             key = normalizeAESKey(key);

             byte[] keyBytes = Encoding.UTF8.GetBytes(key);

             // Создание объекта RijndaelManaged
             using (Aes rijndael = Aes.Create())
             {
                 // Настройка параметров RijndaelManaged
                 rijndael.Mode = CipherMode.CBC;
                 rijndael.Padding = PaddingMode.PKCS7;
                 rijndael.KeySize = keyBytes.Length * 8; // Перевод длины ключа в биты
                 rijndael.IV = IV;

                 // Создание объекта ICryptoTransform
                 using (ICryptoTransform encryptor = rijndael.CreateEncryptor(keyBytes, IV))
                 {
                     // Преобразование сообщения в массив байтов
                     byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                     // Шифрование сообщения
                     byte[] encryptedBytes = encryptor.TransformFinalBlock(messageBytes, 0, messageBytes.Length);

                     // Преобразование зашифрованного сообщения в строку
                     return Convert.ToBase64String(encryptedBytes);
                 }
             }
         }
         public static string Decoder1(string message, string key)
         {
             key = normalizeAESKey(key);

             // Преобразование ключа и зашифрованного сообщения в массивы байтов
             byte[] keyBytes = Encoding.UTF8.GetBytes(key);
             byte[] encryptedBytes = Convert.FromBase64String(message);

             // Создание объекта RijndaelManaged
             using (Aes rijndael = Aes.Create())
             {
                 // Настройка параметров RijndaelManaged
                 rijndael.Mode = CipherMode.CBC;
                 rijndael.Padding = PaddingMode.PKCS7;
                 rijndael.KeySize = keyBytes.Length * 8; // Перевод длины ключа в биты
                 rijndael.IV = IV;

                 // Создание объекта ICryptoTransform
                 using (ICryptoTransform decryptor = rijndael.CreateDecryptor(keyBytes, IV))
                 {
                     int blockSize = rijndael.BlockSize / 8; // Размер блока в байтах
                     int decryptedSize = 0;

                     // Расчет размера расшифрованного массива
                     decryptedSize = encryptedBytes.Length;

                     // Проверка, если последний блок неполный
                     if (decryptedSize % blockSize != 0)
                     {
                         // Уменьшаем размер на остаток от деления на размер блока
                         decryptedSize -= decryptedSize % blockSize;
                     }

                     byte[] decryptedBytes = new byte[decryptedSize];

                     // Расшифровка блоков данных
                     int offset = 0;
                     while (offset < encryptedBytes.Length)
                     {
                         int blockSizeToProcess = Math.Min(blockSize, encryptedBytes.Length - offset);
                         decryptor.TransformBlock(encryptedBytes, offset, blockSizeToProcess, decryptedBytes, offset);
                         offset += blockSizeToProcess;
                     }

                     // Преобразование расшифрованного сообщения в строку
                     return Encoding.UTF8.GetString(decryptedBytes);
                 }
             }
         }*/

        public static byte[] ComputeHash(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            }
        }

        public static string Encoder1(string plainText, string key)
        {
            byte[] keyBytes = ComputeHash(key);
            byte[] iv = new byte[16]; // IV length for AES is 16 bytes

            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = keyBytes;
                aesAlg.IV = iv;
                aesAlg.Padding = PaddingMode.PKCS7; // Use PKCS7 padding

                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText); // Convert string to byte array

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(plainBytes, 0, plainBytes.Length);
                    }
                    byte[] encryptedBytes = msEncrypt.ToArray();
                    return Convert.ToBase64String(encryptedBytes); // Convert encrypted byte array to Base64 string
                }
            }
        }

        public static string Decoder1(string cipherText, string key)
        {
            try
            {
                byte[] keyBytes = ComputeHash(key);
                byte[] iv = new byte[16]; // IV length for AES is 16 bytes

                byte[] cipherBytes = Convert.FromBase64String(cipherText); // Convert Base64 string to byte array

                using (Aes aesAlg = Aes.Create())
                {
                    aesAlg.Key = keyBytes;
                    aesAlg.IV = iv;
                    aesAlg.Padding = PaddingMode.PKCS7; // Use PKCS7 padding

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                    using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd(); // Return decrypted string
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                Random random = new Random();
                StringBuilder stringBuilder = new StringBuilder(cipherText.Length);

                for (int i = 0; i < cipherText.Length; i++)
                {
                    stringBuilder.Append(chars[random.Next(chars.Length)]);
                }

                return stringBuilder.ToString();
            }
        }




        public static string Encoder2(string message, string key)
        {   //  RSA
            throw new NullReferenceException();
        }
        public static string Decoder2(string message, string key)
        {
            throw new NullReferenceException();
        }


        public static string Encoder3(string message, string key)
        {   //  BlowFish
            throw new NullReferenceException();
        }
        public static string Decoder3(string message, string key)
        {
            throw new NullReferenceException();
        }
    }
}
