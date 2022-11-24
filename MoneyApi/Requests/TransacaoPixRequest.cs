namespace MoneyApi.Requests;

public class TransacaoPixRequest
{
    public string ChavePix { get; set; } = string.Empty;
    public decimal Valor { get; set; }
}
