/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Security.Cryptography;
using Aes = System.Security.Cryptography.Aes;

namespace UVtools.Core.Extensions;

public static class CryptExtensions
{
    public static readonly SHA1 SHA1Instance = SHA1.Create();
    public static readonly MD5 MD5Instance = MD5.Create();
    public static string ComputeSHA1Hash(byte[] input)
    {
        return Convert.ToBase64String(SHA1Instance.ComputeHash(input));
    }

    public static string ComputeMD5Hash(byte[] input)
    {
        return Convert.ToBase64String(MD5Instance.ComputeHash(input));
    }

    public static string ComputeCRC32Hash(byte[] input)
    {
        return Convert.ToBase64String(MD5Instance.ComputeHash(input));
    }

    public static readonly SHA256 SHA256Instance = SHA256.Create();
    public static byte[] ComputeSHA256Hash(byte[] input)
    {
        return SHA256Instance.ComputeHash(input);
    }

    public static byte[] AesCryptBytes(byte[] data, byte[] key, CipherMode mode, PaddingMode paddingMode, bool encrypt, byte[]? iv = null)
    {
        if (data.Length % 16 != 0)
        {
            Array.Resize(ref data, ((data.Length / 16) + 1) * 16);
        }

        var aes = Aes.Create();
        aes.KeySize = key.Length * 8;
        aes.Key = key;
        aes.Padding = paddingMode;
        aes.Mode = mode;

        if (iv != null)
        {
            aes.IV = iv;
        }

        var cryptor = encrypt ? aes.CreateEncryptor() : aes.CreateDecryptor();
        
        using var msDecrypt = new MemoryStream(data);
        using var csDecrypt = new CryptoStream(msDecrypt, cryptor, CryptoStreamMode.Read);
        var outputBuffer = new byte[data.Length];
        csDecrypt.ReadExactly(outputBuffer.AsSpan());

        return outputBuffer;
    }

    public static MemoryStream AesCryptMemoryStream(byte[] data, byte[] key, CipherMode mode, PaddingMode paddingMode, bool encrypt, byte[]? iv = null)
        => new(AesCryptBytes(data, key, mode, paddingMode, encrypt, iv));

    public static string Base64EncodeString(string plainText)
    {
        var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        return Convert.ToBase64String(plainTextBytes);
    }

    public static string Base64DecodeString(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
    }

    public static string XORCipherString(string text, string key)
    {
        var output = new char[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            output[i] = (char)(text[i] ^ key[i % key.Length]);
        }

        return new string(output);
    }

    public static string XORCipherString(byte[] bytes, string key)
    {
        var output = new char[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
        {
            output[i] = (char)(bytes[i] ^ key[i % key.Length]);
        }

        return new string(output);
    }

    public static byte[] XORCipher(string text, string key)
    {
        var output = new byte[text.Length];

        for (int i = 0; i < text.Length; i++)
        {
            output[i] = (byte)(text[i] ^ key[i % key.Length]);
        }

        return output;
    }

    public static byte[] XORCipher(byte[] bytes, string key)
    {
        var output = new byte[bytes.Length];

        for (int i = 0; i < bytes.Length; i++)
        {
            output[i] = (byte)(bytes[i] ^ key[i % key.Length]);
        }

        return output;
    }
}