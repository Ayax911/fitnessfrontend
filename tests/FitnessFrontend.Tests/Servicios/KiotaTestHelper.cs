using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FitnessFrontend.ApiCliente;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace FitnessFrontend.Tests.Servicios;

public static class KiotaTestHelper
{
    public sealed class ManejadorHttpSimulado : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public ManejadorHttpSimulado(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public ManejadorHttpSimulado(HttpStatusCode status, string? json = null)
        {
            _responder = _ => new HttpResponseMessage(status)
            {
                Content = json != null ? new StringContent(json, Encoding.UTF8, "application/json") : null
            };
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    public static FitnessApiClient CrearCliente(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var http = new HttpClient(new ManejadorHttpSimulado(responder)) { BaseAddress = new Uri("http://localhost:5224") };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http) { BaseUrl = "http://localhost:5224" };
        return new FitnessApiClient(adapter);
    }

    public static FitnessApiClient CrearCliente(HttpStatusCode status, string? json = null)
    {
        var http = new HttpClient(new ManejadorHttpSimulado(status, json)) { BaseAddress = new Uri("http://localhost:5224") };
        var adapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: http) { BaseUrl = "http://localhost:5224" };
        return new FitnessApiClient(adapter);
    }
}
