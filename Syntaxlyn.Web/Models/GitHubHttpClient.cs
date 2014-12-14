using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using Octokit.Internal;

namespace Syntaxlyn.Web.Models
{
    public class GitHubHttpClient : IHttpClient
    {
        public GitHubHttpClient(string clientId, string clientSecret)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
        }

        private readonly string clientId;
        private readonly string clientSecret;

        private class HttpClientAdapterAdapter : HttpClientAdapter
        {
            public HttpRequestMessage CreateRequestMessage(IRequest request)
            {
                return this.BuildRequestMessage(request);
            }
        }
        private static readonly HttpClientAdapterAdapter adapter = new HttpClientAdapterAdapter();

        private IRequest CreateRequest(IRequest baseReq)
        {
            var newReq = new Request()
            {
                AllowAutoRedirect = baseReq.AllowAutoRedirect,
                BaseAddress = baseReq.BaseAddress,
                Body = baseReq.Body,
                ContentType = baseReq.ContentType,
                Endpoint = baseReq.Endpoint.ApplyParameters(new Dictionary<string, string>()
                {
                    {"client_id", this.clientId }, { "client_secret", this.clientSecret }
                }),
                Method = baseReq.Method,
                Timeout = baseReq.Timeout
            };

            foreach (var kvp in baseReq.Headers)
                newReq.Headers.Add(kvp.Key, kvp.Value);

            foreach (var kvp in baseReq.Parameters)
                newReq.Parameters.Add(kvp.Key, kvp.Value);

            return newReq;
        }

        public async Task<IResponse<T>> Send<T>(IRequest request, CancellationToken cancellationToken)
        {
            request = this.CreateRequest(request);

            if (request.Method != HttpMethod.Get || typeof(T) == typeof(Stream))
                return await this.SendImpl<T>(request, cancellationToken).ConfigureAwait(false);

            var key = request.Endpoint.ToString();
            var cache = GetCache<T>(key);

            string etag;
            if (cache != null && cache.Headers.TryGetValue("ETag", out etag))
                request.Headers.Add("If-None-Match", etag);

            var res = await this.SendImpl<T>(request, cancellationToken).ConfigureAwait(false);
            if (res.StatusCode == HttpStatusCode.NotModified)
                return cache;
            if (res.Headers.ContainsKey("ETag"))
                SetCache(key, res);
            return res;
        }

        private async Task<IResponse<T>> SendImpl<T>(IRequest request, CancellationToken cancellationToken)
        {
            var isStream = typeof(T) == typeof(Stream);

            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = request.AllowAutoRedirect,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            var hc = new HttpClient() { BaseAddress = request.BaseAddress, Timeout = request.Timeout };
            using (var msg = adapter.CreateRequestMessage(request))
            {
                var resMsg = await hc.SendAsync(
                    msg,
                    isStream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead,
                    cancellationToken
                ).ConfigureAwait(false);

                var content = resMsg.Content;
                var res = new ApiResponse<T>()
                {
                    StatusCode = resMsg.StatusCode,
                    ContentType = content.Headers.ContentType?.MediaType
                };
                foreach (var h in content.Headers)
                    res.Headers.Add(h.Key, h.Value.FirstOrDefault());

                if (isStream)
                    res.BodyAsObject = (T)(object)await resMsg.Content.ReadAsStreamAsync().ConfigureAwait(false);
                else if (typeof(T) == typeof(byte[]))
                    res.BodyAsObject = (T)(object)await resMsg.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                else
                    res.Body = await resMsg.Content.ReadAsStringAsync().ConfigureAwait(false);

                return res;
            }
        }

        private static readonly CacheItemPolicy cacheItemPolicy = new CacheItemPolicy()
        {
            SlidingExpiration = TimeSpan.FromHours(1)
        };

        private static void SetCache<T>(string key, IResponse<T> value)
        {
            MemoryCache.Default.Set(key, value, cacheItemPolicy);
        }

        private static IResponse<T> GetCache<T>(string key)
        {
            return (IResponse<T>)MemoryCache.Default.Get(key);
        }
    }
}