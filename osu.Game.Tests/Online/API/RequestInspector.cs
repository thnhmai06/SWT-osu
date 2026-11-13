// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Moq;
using osu.Framework.IO.Network;
using osu.Game.Online;
using osu.Game.Online.API;

namespace osu.Game.Tests.Online.API
{
    /// <summary>
    /// Reflection-based helper to look at the URI and parameters an <see cref="APIRequest"/> would send, without performing it.
    /// </summary>
    public static class RequestInspector
    {
        private const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        private static readonly Mock<IAPIProvider> api = createApi();

        private static Mock<IAPIProvider> createApi()
        {
            var mock = new Mock<IAPIProvider>();
            mock.SetupGet(a => a.Endpoints).Returns(new EndpointConfiguration { APIUrl = "http://api.test", BeatmapSubmissionServiceUrl = "http://sub.test" });
            mock.Setup(a => a.Schedule(It.IsAny<Action>())).Callback<Action>(a => a());
            return mock;
        }

        public static T Attach<T>(T request) where T : APIRequest
        {
            request.AttachAPI(api.Object);
            typeof(APIRequest).GetProperty("User", flags)!.SetValue(request, new osu.Game.Online.API.Requests.Responses.APIUser { Id = 1 });
            return request;
        }

        /// <summary>
        /// Completes the request successfully with the given response, without any network.
        /// </summary>
        public static void Succeed<TResponse>(APIRequest<TResponse> request, TResponse response) where TResponse : class
        {
            Attach(request);
            typeof(APIRequest<TResponse>).GetMethod("TriggerSuccess", flags, new[] { typeof(TResponse) })!.Invoke(request, new object[] { response });
        }

        public static string GetUri(APIRequest request) => (string)typeof(APIRequest).GetProperty("Uri", flags)!.GetValue(Attach(request))!;

        public static string GetTarget(APIRequest request) => (string)typeof(APIRequest).GetProperty("Target", flags)!.GetValue(request)!;

        public static WebRequest CreateWebRequest(APIRequest request)
            => (WebRequest)request.GetType().GetMethod("CreateWebRequest", flags)!.Invoke(Attach(request), null)!;

        public static HttpMethod GetMethod(APIRequest request) => CreateWebRequest(request).Method;

        /// <summary>
        /// All query and form parameters added to the request, in insertion order within each collection.
        /// </summary>
        public static List<KeyValuePair<string, string>> GetParameters(APIRequest request)
        {
            var web = CreateWebRequest(request);
            var result = new List<KeyValuePair<string, string>>();

            foreach (string fieldName in new[] { "queryParameters", "formParameters" })
            {
                object? value = typeof(WebRequest).GetField(fieldName, flags)?.GetValue(web);

                if (value is System.Collections.Specialized.NameValueCollection nvc)
                {
                    foreach (string? key in nvc.AllKeys)
                        foreach (string v in nvc.GetValues(key)!)
                            result.Add(new KeyValuePair<string, string>(key!, v));
                }
                else if (value is IEnumerable<(string, string)> pairs)
                    result.AddRange(pairs.Select(p => new KeyValuePair<string, string>(p.Item1, p.Item2)));
                else if (value != null)
                    throw new InvalidOperationException($"Unsupported parameter storage {value.GetType()}");
            }

            return result;
        }

        public static string[] Values(APIRequest request, string key) => GetParameters(request).Where(p => p.Key == key).Select(p => p.Value).ToArray();
    }
}
