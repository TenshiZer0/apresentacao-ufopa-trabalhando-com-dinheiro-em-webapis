namespace MoneyApi.Requests;

public class ParcelamentoComJurosRequest
{
    public decimal ValorTotal { get; set; }
    public int NumeroParcelas { get; set; }
    public decimal JurosMensal { get; set; }
}
