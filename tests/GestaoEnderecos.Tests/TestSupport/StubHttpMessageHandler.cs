using System.Net;

namespace GestaoEnderecos.Tests.TestSupport;

/// <summary>
/// Handler HTTP de teste: responde a partir de uma função (ou lança), permitindo simular
/// sucesso, erro de aplicação, status 500, timeout e indisponibilidade — sem rede real.
/// </summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    private StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        _responder = responder;

    public static StubHttpMessageHandler ComJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

    public static StubHttpMessageHandler ComStatus(HttpStatusCode status) =>
        new(_ => new HttpResponseMessage(status));

    public static StubHttpMessageHandler Lancando(Exception excecao) =>
        new(_ => throw excecao);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(_responder(request));
}
