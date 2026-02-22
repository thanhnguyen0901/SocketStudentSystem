using System.Security.Cryptography;
using System.Text;

namespace StudentServer.Console.Crypto;

// DES-CBC-PKCS7 encrypt/decrypt. DES is required by the assignment specification.
internal sealed class DesCryptoService
{
    private const int RequiredKeyLength = 8;
    private static readonly Encoding TextEncoding = Encoding.UTF8;

    private readonly byte[] _key;
    private readonly byte[] _iv;

    public DesCryptoService(byte[] key8, byte[] iv8)
    {
        ArgumentNullException.ThrowIfNull(key8, nameof(key8));
        ArgumentNullException.ThrowIfNull(iv8, nameof(iv8));

        if (key8.Length != RequiredKeyLength)
        {
            throw new ArgumentException($"DES key must be exactly {RequiredKeyLength} bytes (got {key8.Length}).", nameof(key8));
        }

        if (iv8.Length != RequiredKeyLength)
        {
            throw new ArgumentException($"DES IV must be exactly {RequiredKeyLength} bytes (got {iv8.Length}).", nameof(iv8));
        }

        // Defensive copies so the caller cannot mutate the key after construction.
        _key = (byte[])key8.Clone();
        _iv = (byte[])iv8.Clone();
    }

    public byte[] EncryptString(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText, nameof(plainText));
        return Encrypt(TextEncoding.GetBytes(plainText));
    }

    public string DecryptToString(byte[] cipherBytes)
    {
        ArgumentNullException.ThrowIfNull(cipherBytes, nameof(cipherBytes));

        if (cipherBytes.Length == 0)
        {
            throw new ArgumentException("Cipher bytes must not be empty.", nameof(cipherBytes));
        }

        return TextEncoding.GetString(Decrypt(cipherBytes));
    }

    // Encrypt -> decrypt roundtrip check. Call once at startup to catch key/IV mismatches.
    public void SelfTest()
    {
        const string probe = "DES-roundtrip-check-2026";
        string result = DecryptToString(EncryptString(probe));

        if (result != probe)
            throw new InvalidOperationException(
                $"DES self-test failed: expected \"{probe}\" but got \"{result}\".");
    }

    // A new DES instance is created per operation; ICryptoTransform is single-use.
    private DES CreateDes()
    {
        var des = DES.Create();
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;
        des.Key = _key;
        des.IV = _iv;
        return des;
    }

    private byte[] Encrypt(byte[] plainBytes)
    {
        using DES des = CreateDes();
        using ICryptoTransform encryptor = des.CreateEncryptor();
        return TransformAllBytes(encryptor, plainBytes);
    }

    private byte[] Decrypt(byte[] cipherBytes)
    {
        using DES des = CreateDes();
        using ICryptoTransform decryptor = des.CreateDecryptor();
        return TransformAllBytes(decryptor, cipherBytes);
    }

    private static byte[] TransformAllBytes(ICryptoTransform transform, byte[] input)
    {
        using var outputStream = new MemoryStream();
        using (var cryptoStream = new CryptoStream(outputStream, transform, CryptoStreamMode.Write))
        {
            cryptoStream.Write(input, 0, input.Length);
            cryptoStream.FlushFinalBlock();
        }

        return outputStream.ToArray();
    }
}
