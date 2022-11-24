using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using MoneyApi.Requests;
using MoneyApi.Responses;

namespace MoneyApi.Controllers;

[ApiController] // informa que a classe é um Controller de API
[Route("[controller]")] // define a rota para o nome da classe sem o sufixo "Controller" (/conta)
public class ContaController : ControllerBase // herda funcionalidades básicas de um controller
{

    // usando cache em memória para simular um banco de dados
    private readonly IMemoryCache _cache;

    // o objeto do cache em memória é recebido via Dependency Injection
    public ContaController(IMemoryCache cache)
    {
        _cache = cache;
    }

    [HttpGet] // utilizando o método HTTP GET
    [Route("[action]")] // define a rota para o nome do método (/conta/saldo)
    public SaldoResponse Saldo()
    {
        // pra esse cenário, não existem casos de erro
        // mas um possível caso de erro pode ser de acesso à base de dados
        return new()
        {
            Sucesso = true,
            Saldo = GetSaldo()
        };
    }

    [HttpPost] // utilizando o método HTTP POST
    [Route("[action]")]
    public TransacaoPixResponse ReceberPix(TransacaoPixRequest request)
    {
        // validações de dados de entrada
        // em uma API real, essas validações devem retornar status HTTP 400 Bad Request
        if (request.ChavePix != "vincentwillian@gmail.com")
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Chave PIX inválida"
            };
        }
        if (request.Valor < 0.01m)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Valor mínimo é de R$ 0,01"
            };
        }

        // é necessário realizar o arredondamento do valor,
        // para que não sejam calculados décimos de centavos
        var novoSaldo = GetSaldo() + Math.Round(request.Valor, 2);
        SetSaldo(novoSaldo);
        return new()
        {
            Sucesso = true
        };
    }

    [HttpPost]
    [Route("[action]")]
    public TransacaoPixResponse TransferirPix(TransacaoPixRequest request)
    {
        if (string.IsNullOrEmpty(request.ChavePix))
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Chave PIX inválida"
            };
        }
        if (request.Valor < 0.01m)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Valor deve ser maior que 0.01"
            };
        }

        var novoSaldo = GetSaldo() - Math.Round(request.Valor, 2);
        SetSaldo(novoSaldo);
        return new()
        {
            Sucesso = true
        };
    }

    [HttpPost]
    [Route("[action]")]
    public ParcelamentoSemJurosResponse ParcelarSemJuros(ParcelamentoSemJurosRequest request)
    {
        if (request.ValorTotal < 0)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Valor total deve ser maior que 0"
            };
        }
        if (request.NumeroParcelas < 0)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Número de parcelas deve ser maior que 0"
            };
        }

        // algoritmo de parcelamento
        // Math.Floor() arredonda o valor para menos
        // multiplicamos por 100 antes de arredondar e dividimos depois para truncar o valor em 2 casas decimais
        var menorValorParcela = Math.Floor(100 * request.ValorTotal / request.NumeroParcelas) / 100;
        var maiorValorParcela = menorValorParcela + 0.01m; // o centavo perdido
        int parcelasComMaiorValor = (int)(request.ValorTotal % request.NumeroParcelas);
        var parcelas = new List<decimal>();
        // as primeiras parcelas receberão o maior valor
        for (int i = 0; i < parcelasComMaiorValor; i++) parcelas.Add(maiorValorParcela);
        for (int i = parcelasComMaiorValor; i < request.NumeroParcelas; i++) parcelas.Add(menorValorParcela);
        // fim algoritmo de parcelamento

        return new()
        {
            Sucesso = true,
            Parcelas = parcelas,
            SomaParcelas = parcelas.Sum()
        };
    }

    [HttpPost]
    [Route("[action]")]
    public ParcelamentoComJurosResponse ParcelarComJuros(ParcelamentoComJurosRequest request)
    {
        if (request.ValorTotal < 0)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Valor total deve ser maior que 0"
            };
        }
        if (request.NumeroParcelas < 0)
        {
            return new()
            {
                Sucesso = false,
                MensagemErro = "Número de parcelas deve ser maior que 0"
            };
        }

        // fórmula da tabela PRICE para calcular valor da parcela
        // taxa = (1 + JurosMensal) ^ NumeroParcelas
        // obs: utilizei o "^" apenas pra ilustrar uma operação de potência,
        // mas em C# o operador "^" é um operador de XOR (ou-exclusivo) bit-a-bit, por isso utilizamos o método Math.Pow()
        var taxa = (decimal)Math.Pow((double)(1 + request.JurosMensal), request.NumeroParcelas);
        var valorParcela = request.ValorTotal * (taxa * request.JurosMensal) / (taxa - 1);

        // cálculo do menor e maior valores pras parcelas (algoritmo de parcelamento)
        var menorValorParcela = Math.Floor(valorParcela * 100) / 100;
        var maiorValorParcela = menorValorParcela + 0.01m;
        var parcelasComMaiorValor = (int)(request.ValorTotal % request.NumeroParcelas);

        var parcelas = new List<ParcelaComJuros>();
        var saldoDevedor = Math.Round(request.ValorTotal, 2); // arredondando os centavos do empréstimo
        var somaParcelas = 0m;
        var somaAmortizacoes = 0m;
        var somaJuros = 0m;
        for (int i = 0; i < request.NumeroParcelas; i++)
        {
            var parcela = new ParcelaComJuros();
            parcela.ValorParcela = i < parcelasComMaiorValor ? menorValorParcela : maiorValorParcela; // as primeiras parcelas receberão o maior valor
            parcela.SaldoDevedorAntes = saldoDevedor; // saldo devedor antes da amortização
            parcela.Juros = Math.Round(saldoDevedor * request.JurosMensal, 2); // cálculo de juros simples
            parcela.Amortizacao = parcela.ValorParcela - parcela.Juros; // amortização é o quanto realmente vai ser pago da dívida
            parcela.SaldoDevedorDepois = saldoDevedor - parcela.Amortizacao; // saldo devedor após a amortização
            saldoDevedor = parcela.SaldoDevedorDepois; // atualização do saldo devedor para a próxima iteração do loop
            parcelas.Add(parcela);
            somaParcelas += parcela.ValorParcela;
            somaAmortizacoes += parcela.Amortizacao;
            somaJuros += parcela.Juros;
        }

        return new()
        {
            Sucesso = true,
            Parcelas = parcelas,
            SomaAmortizacoes = somaAmortizacoes,
            SomaJuros = somaJuros,
            SomaParcelas = somaParcelas
        };
    }

    private decimal GetSaldo()
    {
        // retorna o registro do saldo, criando se ainda não existir
        return _cache.GetOrCreate("saldo", registro =>
        {
            registro.SetValue(0m);
            return 0m;
        });
    }

    private void SetSaldo(decimal valor)
    {
        _cache.Set("saldo", valor);
    }
}
