using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Werkflow.OpcUaSimulator.Core.PhysicalSimulation.Evaluation.Export;

public static class EvaluationHashUtility
{
	public static string ComputeSha256(object value)
	{
		string json = JsonSerializer.Serialize(value);
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}

	public static string ComputeSha256FromText(string text)
	{
		byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
		return Convert.ToHexString(hash).ToLowerInvariant();
	}
}
