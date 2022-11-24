namespace MoneyApi.Responses;

public class ParcelamentoSemJurosResponse
{
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public List<decimal>? Parcelas { get; set; }
    public decimal? SomaParcelas { get; set; }
}
