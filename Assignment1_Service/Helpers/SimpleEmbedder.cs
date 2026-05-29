using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Assignment1_Service.Helpers;

public static class SimpleEmbedder
{
    public static string GenerateVector(string text, int dimension)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var vector = new float[dimension];

        for (var i = 0; i < dimension; i++)
            vector[i] = hash[i % hash.Length] / 127.5f - 1f;

        return JsonSerializer.Serialize(vector);
    }
}
