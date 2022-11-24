namespace MoneyApi.Responses;

public class ParcelamentoComJurosResponse
{
    public bool Sucesso { get; set; }
    public string? MensagemErro { get; set; }
    public List<ParcelaComJuros>? Parcelas { get; set; }
    public decimal? SomaParcelas { get; set; }
    public decimal? SomaJuros { get; set; }
    public decimal? SomaAmortizacoes { get; set; }
}

public class ParcelaComJuros
{
    public decimal ValorParcela { get; set; }
    public decimal Juros { get; set; }
    public decimal Amortizacao { get; set; }
    public decimal SaldoDevedorAntes { get; set; }
    public decimal SaldoDevedorDepois { get; set; }
}
