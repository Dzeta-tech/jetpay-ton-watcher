using System.Text.Json.Serialization;
using Newtonsoft.Json;
using TonSdk.Client;

namespace JetPay.TonWatcher.Services;

public class OrbsTonClientFactory(ILogger<OrbsTonClientFactory> logger) : ITonClientFactory
{
    readonly List<TonClient> clients = new();

    public async Task Initialize()
    {
        using HttpClient httpClient = new();
        logger.LogInformation("Getting Orbs nodes");
        OrbsNode[]? nodes = await httpClient.GetFromJsonAsync<OrbsNode[]>("https://ton.access.orbs.network/mngr/nodes");

        if (nodes is null)
        {
            logger.LogError("Failed to retrieve Orbs nodes");
            return;
        }

        logger.LogInformation("Orbs nodes retrieved. Got {} nodes", nodes.Length);

        foreach (OrbsNode node in nodes)
        {
            string url = $"https://ton.access.orbs.network/{node.NodeId}/1/mainnet/toncenter-api-v2/jsonRPC";
            TonClient client = new(TonClientType.HTTP_TONCENTERAPIV2, new HttpParameters { Endpoint = url });
            clients.Add(client);
        }
    }

    public ITonClient GetClient()
    {
        return clients[Random.Shared.Next(clients.Count)];
    }
}

public class Health
{
    [JsonProperty("v2-mainnet")]
    [JsonPropertyName("v2-mainnet")]
    public bool V2Mainnet { get; set; }

    [JsonProperty("v2-testnet")]
    [JsonPropertyName("v2-testnet")]
    public bool V2Testnet { get; set; }

    [JsonProperty("v4-mainnet")]
    [JsonPropertyName("v4-mainnet")]
    public bool V4Mainnet { get; set; }

    [JsonProperty("v4-testnet")]
    [JsonPropertyName("v4-testnet")]
    public bool V4Testnet { get; set; }
}

public class Mngr
{
    [JsonProperty("updated")]
    [JsonPropertyName("updated")]
    public string Updated { get; set; }

    [JsonProperty("health")]
    [JsonPropertyName("health")]
    public Health Health { get; set; }

    [JsonProperty("successTS")]
    [JsonPropertyName("successTS")]
    public object SuccessTs { get; set; }

    [JsonProperty("errors")]
    [JsonPropertyName("errors")]
    public List<object> Errors { get; set; }

    [JsonProperty("atleastOneHealthy")]
    [JsonPropertyName("atleastOneHealthy")]
    public bool AtleastOneHealthy { get; set; }

    [JsonProperty("code")]
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonProperty("text")]
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

public class OrbsNode
{
    [JsonProperty("NodeId")]
    [JsonPropertyName("NodeId")]
    public string NodeId { get; set; }

    [JsonProperty("BackendName")]
    [JsonPropertyName("BackendName")]
    public string BackendName { get; set; }

    [JsonProperty("Ip")]
    [JsonPropertyName("Ip")]
    public string Ip { get; set; }

    [JsonProperty("Weight")]
    [JsonPropertyName("Weight")]
    public int Weight { get; set; }

    [JsonProperty("Healthy")]
    [JsonPropertyName("Healthy")]
    public string Healthy { get; set; }

    [JsonProperty("Mngr")]
    [JsonPropertyName("Mngr")]
    public Mngr Mngr { get; set; }
}