// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Moq;
using NUnit.Framework;
using osu.Game.Online;
using osu.Game.Online.API;

namespace osu.Game.Tests.Online.API
{
    [TestFixture]
    public class APIRequestTest
    {
        private class TestRequest : APIRequest
        {
            protected override string Target => "test";
        }

        private class TestResponseRequest : APIRequest<string>
        {
            protected override string Target => "test";
        }

        private static Mock<IAPIProvider> createApi()
        {
            var api = new Mock<IAPIProvider>();
            api.SetupGet(a => a.Endpoints).Returns(new EndpointConfiguration { APIUrl = "http://api.test" });
            api.Setup(a => a.Schedule(It.IsAny<Action>())).Callback<Action>(a => a());
            return api;
        }

        private static void triggerSuccess(APIRequest request)
            => typeof(APIRequest).GetMethod("TriggerSuccess", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes)!.Invoke(request, null);

        // CF: APIRequest.AttachAPI | API != null && API != apiAccess | FALSE
        [Test]
        public void TestAttachSameApiTwiceIsAllowed()
        {
            var api = createApi().Object;
            var request = new TestRequest();

            request.AttachAPI(api);

            Assert.DoesNotThrow(() => request.AttachAPI(api));
        }

        // CF: APIRequest.AttachAPI | API != null && API != apiAccess | TRUE
        // CF: APIRequest.AttachAPI | attached API changed | throws InvalidOperationException
        [Test]
        public void TestAttachDifferentApiThrows()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);

            Assert.Throws<InvalidOperationException>(() => request.AttachAPI(createApi().Object));
        }

        // CF: APIRequest.Uri | combines endpoint and target | no throw
        [Test]
        public void TestUri() => Assert.That(RequestInspector.GetUri(new TestRequest()), Is.EqualTo("http://api.test/api/v2/test"));

        // CF: APIRequest.Cancel | Fail with OperationCanceledException | no throw
        // CF: APIRequest.Fail | CompletionState != Waiting | FALSE
        // CF: APIRequest.Fail | e is OperationCanceledException | TRUE
        [Test]
        public void TestCancelFailsRequest()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            Exception? failure = null;
            request.Failure += e => failure = e;

            request.Cancel();

            Assert.That(failure, Is.TypeOf<OperationCanceledException>());
            Assert.That(request.CompletionState, Is.EqualTo(APIRequestCompletionState.Failed));
        }

        // CF: APIRequest.Fail | e is OperationCanceledException | FALSE
        // CF: APIRequest.Fail | string.IsNullOrEmpty(responseString) | TRUE
        [Test]
        public void TestFailWithoutWebRequestPassesExceptionThrough()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            Exception? failure = null;
            request.Failure += e => failure = e;
            var error = new InvalidOperationException("boom");

            request.Fail(error);

            Assert.That(failure, Is.SameAs(error));
        }

        // CF: APIRequest.Fail | CompletionState != Waiting | TRUE
        [Test]
        public void TestSecondFailureIsIgnored()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            int failures = 0;
            request.Failure += _ => failures++;

            request.Fail(new Exception());
            request.Fail(new Exception());
            request.Cancel();

            Assert.That(failures, Is.EqualTo(1));
        }

        // CF: APIRequest.TriggerSuccess | CompletionState != Waiting | FALSE
        [Test]
        public void TestTriggerSuccessCompletesAndRaisesEvent()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            int successes = 0;
            request.Success += () => successes++;

            triggerSuccess(request);

            Assert.That(request.CompletionState, Is.EqualTo(APIRequestCompletionState.Completed));
            Assert.That(successes, Is.EqualTo(1));
        }

        // CF: APIRequest.TriggerSuccess | CompletionState != Waiting | TRUE
        [Test]
        public void TestSuccessAfterFailureIsIgnored()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            int successes = 0;
            request.Success += () => successes++;

            request.Cancel();
            triggerSuccess(request);

            Assert.That(successes, Is.EqualTo(0));
            Assert.That(request.CompletionState, Is.EqualTo(APIRequestCompletionState.Failed));
        }

        // CF: APIRequest.TriggerFailure | CompletionState != Waiting | TRUE
        [Test]
        public void TestFailureAfterSuccessIsIgnored()
        {
            var request = new TestRequest();
            request.AttachAPI(createApi().Object);
            int failures = 0;
            request.Failure += _ => failures++;

            triggerSuccess(request);
            request.Fail(new Exception());

            Assert.That(failures, Is.EqualTo(0));
            Assert.That(request.CompletionState, Is.EqualTo(APIRequestCompletionState.Completed));
        }

        // CF: APIRequest<T>.TriggerSuccess(T) | Response != null | FALSE
        [Test]
        public void TestTypedSuccessStoresResponseAndRaisesTypedEvent()
        {
            var request = new TestResponseRequest();
            string? received = null;
            request.Success += r => received = r;

            RequestInspector.Succeed(request, "payload");

            Assert.That(request.Response, Is.EqualTo("payload"));
            Assert.That(received, Is.EqualTo("payload"));
        }

        // CF: APIRequest<T>.TriggerSuccess(T) | Response != null | TRUE
        // CF: APIRequest<T>.TriggerSuccess(T) | success triggered twice | throws InvalidOperationException
        [Test]
        public void TestTypedSuccessTwiceThrows()
        {
            var request = new TestResponseRequest();
            RequestInspector.Succeed(request, "a");

            var ex = Assert.Throws<TargetInvocationException>(() => RequestInspector.Succeed(request, "b"));
            Assert.That(ex!.InnerException, Is.TypeOf<InvalidOperationException>());
        }
    }
}
