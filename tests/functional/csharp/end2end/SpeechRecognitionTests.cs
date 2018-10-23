//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech.Audio;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.CognitiveServices.Speech.Tests.EndToEnd
{
    using static AssertHelpers;
    using static SpeechRecognitionTestsHelper;

    [TestClass]
    public class SpeechRecognitionTests : RecognitionTestBase
    {
        private static string deploymentId;
        private SpeechRecognitionTestsHelper helper;

        [ClassInitialize]
        public static void TestClassinitialize(TestContext context)
        {
            BaseClassInit(context);
            deploymentId = Config.GetSettingByKey<String>(context, "DeploymentId");
        }

        [TestInitialize]
        public void Initialize()
        {
            helper = new SpeechRecognitionTestsHelper();
        }

        [TestMethod]
        public async Task ValidBaselineRecognition()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                helper.SubscribeToCounterEventHandlers(recognizer);
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.IsTrue(result.Duration.Ticks > 0, result.Reason.ToString(), "Duration == 0");
                Assert.AreEqual(200000, result.OffsetInTicks, "Offset not zero");
                AssertMatching(TestData.English.Weather.Utterance, result.Text);
            }
        }

        [TestMethod] //TODO: Remove Test after Bug 1269097 is fixed
        public async Task ContinuousValidBaselineRecognition()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                AssertMatching(
                    TestData.English.Weather.Utterance,
                    await helper.GetFirstRecognizerResult(recognizer));
            }
        }

        [TestMethod]
        public async Task ValidCustomRecognition()
        {
            this.config.EndpointId = deploymentId;
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(config, audioInput)))
            {
                helper.SubscribeToCounterEventHandlers(recognizer);
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                AssertMatching(TestData.English.Weather.Utterance, result.Text);
            }
        }

        [TestMethod] //TODO: Remove Test after Bug 1269097 is fixed
        public async Task ContinuousValidCustomRecognition()
        {
            this.config.EndpointId = deploymentId;
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(config, audioInput)))
            {
                AssertMatching(TestData.English.Weather.Utterance,
                    await helper.GetFirstRecognizerResult(recognizer));
            }
        }

        [TestMethod]
        public async Task InvalidKeyHandledProperly()
        {
            var config = SpeechConfig.FromSubscription("invalidKey", region);
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.AreEqual(ResultReason.Canceled, result.Reason);

                var cancellation = CancellationDetails.FromResult(result);
                Assert.AreEqual(cancellation.Reason, CancellationReason.Error);
                AssertStringContains(cancellation.ErrorDetails, "WebSocket Upgrade failed with an authentication error (401)");
            }
        }

        [TestMethod]
        public async Task InvalidRegionHandledProperly()
        {
            var config = SpeechConfig.FromSubscription(subscriptionKey, "invalidRegion");
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.AreEqual(ResultReason.Canceled, result.Reason);

                var cancellation = CancellationDetails.FromResult(result);
                Assert.AreEqual(cancellation.Reason, CancellationReason.Error);
                AssertStringContains(cancellation.ErrorDetails, "Connection failed");
            }
        }

        [TestMethod]
        public void InvalidInputFileHandledProperly()
        {
            var audioInput = AudioConfig.FromWavFileInput("invalidFile.wav");
            Assert.ThrowsException<ApplicationException>(() => new SpeechRecognizer(this.config, audioInput));
        }

        [TestMethod]
        public async Task InvalidDeploymentIdHandledProperly()
        {
            this.config.EndpointId = "invalidDeploymentId";
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {

                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.AreEqual(ResultReason.Canceled, result.Reason);

                var cancellation = CancellationDetails.FromResult(result);
                Assert.AreEqual(cancellation.Reason, CancellationReason.Error);
                AssertStringContains(cancellation.ErrorDetails, "WebSocket Upgrade failed with a bad request (400)");
            }
        }

        [TestMethod]
        public async Task InvalidLanguageHandledProperly()
        {
            this.config.SpeechRecognitionLanguage = "InvalidLang";
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.AreEqual(ResultReason.Canceled, result.Reason);

                var cancellation = CancellationDetails.FromResult(result);
                AssertStringContains(cancellation.ErrorDetails, "WebSocket Upgrade failed with a bad request (400)");
            }
        }

        [TestMethod]
        public async Task GermanRecognition()
        {
            this.config.SpeechRecognitionLanguage = Language.DE_DE;
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, AudioConfig.FromWavFileInput(TestData.German.FirstOne.AudioFile))))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.IsFalse(string.IsNullOrEmpty(result.Text), result.Reason.ToString());
                AssertMatching(TestData.German.FirstOne.Utterance, result.Text);
            }
        }

        [TestMethod] //TODO: Remove Test after Bug 1269097 is fixed
        public async Task ContinuousGermanRecognition()
        {
            this.config.SpeechRecognitionLanguage = Language.DE_DE;
            var audioInput = AudioConfig.FromWavFileInput(TestData.German.FirstOne.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                var result = await helper.GetFirstRecognizerResult(recognizer);
                AssertMatching(TestData.German.FirstOne.Utterance, result);
            }
        }

        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task ContinuousRecognitionOnLongFileInput()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Batman.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                List<string> recognizedText = new List<string>();
                helper.SubscribeToCounterEventHandlers(recognizer);
                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Text.Length > 0)
                    {
                        recognizedText.Add(e.Result.Text);
                    }
                };

                await helper.CompleteContinuousRecognition(recognizer);

                Assert.IsTrue(helper.RecognizedEventCount > 0, $"Invalid number of final result events {helper.RecognizedEventCount}");
                Assert.AreEqual(0, helper.ErrorEventCount, AssertOutput.WrongErrorCount);
                Assert.AreEqual(1, helper.SpeechStartedEventCount, AssertOutput.WrongSpeechStartedCount);
                Assert.IsTrue(recognizedText.Count > 0, $"Invalid number of text messages {recognizedText.Count}");

                AssertMatching(TestData.English.Batman.Utterances[0], recognizedText[0]);
                AssertMatching(TestData.English.Batman.Utterances.Last(), recognizedText.Last());
            }
        }

        [TestMethod]
        public async Task SubscribeToManyEventHandlers()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                const int numLoops = 7;
                for (int i = 0; i < numLoops; i++)
                {
                    helper.SubscribeToCounterEventHandlers(recognizer);
                }
                await helper.CompleteContinuousRecognition(recognizer);

                Assert.AreEqual(numLoops, helper.RecognizedEventCount, AssertOutput.WrongFinalResultCount);
                Assert.AreEqual(numLoops, helper.SpeechStartedEventCount, AssertOutput.WrongSpeechStartedCount);
                Assert.AreEqual(numLoops, helper.SessionStartedEventCount, AssertOutput.WrongSessionStartedCount);
                Assert.AreEqual(0, helper.ErrorEventCount, AssertOutput.WrongErrorCount);
            }
        }

        [TestMethod]
        public async Task UnsubscribeFromEventHandlers()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                helper.SubscribeToCounterEventHandlers(recognizer);
                helper.UnsubscribeFromCounterEventHandlers(recognizer);

                await helper.CompleteContinuousRecognition(recognizer);

                Assert.AreEqual(0, helper.RecognizedEventCount, AssertOutput.WrongFinalResultCount);
                Assert.AreEqual(0, helper.SpeechStartedEventCount, AssertOutput.WrongSpeechStartedCount);
                Assert.AreEqual(0, helper.SessionStartedEventCount, AssertOutput.WrongSessionStartedCount);
                Assert.AreEqual(0, helper.ErrorEventCount, AssertOutput.WrongErrorCount);
            }
        }

        [TestMethod]
        public async Task ResubscribeToEventHandlers()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                const int numSubscriptions = 3;
                const int numUnsubscriptions = 2;
                const int diff = numSubscriptions - numUnsubscriptions + numSubscriptions;

                for (int i = 0; i < numSubscriptions; i++)
                {
                    helper.SubscribeToCounterEventHandlers(recognizer);
                }

                for (int i = 0; i < numUnsubscriptions; i++)
                {
                    helper.UnsubscribeFromCounterEventHandlers(recognizer);
                }

                for (int i = 0; i < numSubscriptions; i++)
                {
                    helper.SubscribeToCounterEventHandlers(recognizer);
                }

                await helper.CompleteContinuousRecognition(recognizer);

                Assert.AreEqual(diff, helper.RecognizedEventCount, AssertOutput.WrongFinalResultCount);
                Assert.AreEqual(diff, helper.SpeechStartedEventCount, AssertOutput.WrongSpeechStartedCount);
                Assert.AreEqual(diff, helper.SessionStartedEventCount, AssertOutput.WrongSessionStartedCount);
                Assert.AreEqual(0, helper.ErrorEventCount, AssertOutput.WrongErrorCount);
            }
        }

        [TestMethod]
        public async Task ChangeSubscriptionDuringRecognition()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                helper.SubscribeToCounterEventHandlers(recognizer);
                recognizer.SessionStarted += (s, e) =>
                {
                    helper.UnsubscribeFromCounterEventHandlers(recognizer);
                };

                await helper.CompleteContinuousRecognition(recognizer);
                Assert.AreEqual(1, helper.SessionStartedEventCount, AssertOutput.WrongSessionStartedCount);
                Assert.AreEqual(0, helper.RecognizedEventCount, AssertOutput.WrongFinalResultCount);
                Assert.AreEqual(0, helper.SpeechStartedEventCount, AssertOutput.WrongSpeechStartedCount);
                Assert.AreEqual(0, helper.ErrorEventCount, AssertOutput.WrongErrorCount);
            }
        }

        [TestMethod]
        public void TestGetters()
        {
            this.config.EndpointId = deploymentId;
            var audioInput = AudioConfig.FromWavFileInput(TestData.German.FirstOne.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                Assert.IsTrue(string.IsNullOrEmpty(recognizer.SpeechRecognitionLanguage));
                Assert.AreEqual(recognizer.SpeechRecognitionLanguage, recognizer.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage));
                Assert.AreEqual(deploymentId, recognizer.Properties.GetProperty(PropertyId.SpeechServiceConnection_EndpointId));
                Assert.AreEqual(OutputFormat.Simple, recognizer.OutputFormat);
            }

            this.config.SpeechRecognitionLanguage = Language.DE_DE;
            this.config.EndpointId = string.Empty;
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                Assert.AreEqual(Language.DE_DE, recognizer.SpeechRecognitionLanguage);
                Assert.AreEqual(Language.DE_DE, recognizer.Properties.GetProperty(PropertyId.SpeechServiceConnection_RecoLanguage));
                Assert.AreEqual(OutputFormat.Simple, recognizer.OutputFormat);
            }

            this.config.OutputFormat = OutputFormat.Simple;
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                Assert.AreEqual(OutputFormat.Simple, recognizer.OutputFormat);
            }

            this.config.OutputFormat = OutputFormat.Detailed;
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                Assert.AreEqual(OutputFormat.Detailed, recognizer.OutputFormat);
            }
        }

        [TestMethod]
        public async Task TestExceptionSwitchFromSingleShotToContinuous()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                var ex = await Assert.ThrowsExceptionAsync<ApplicationException>(() => recognizer.StartContinuousRecognitionAsync());
                AssertStringContains(ex.Message, "Exception with an error code: 0x1e");
            }
        }

        [TestMethod]
        public async Task TestExceptionSwitchFromContinuousToSingleShot()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Weather.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                recognizer.Canceled += (s, e) => { Console.WriteLine($"Recognition Canceled: {e.ToString()}"); };
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                var ex = await Assert.ThrowsExceptionAsync<ApplicationException>(() => recognizer.RecognizeOnceAsync());
                AssertStringContains(ex.Message, "Exception with an error code: 0x1e");
            }
        }

        [TestMethod]
        [Ignore] // Service is being investigated
        public async Task TestSingleShotTwice()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Batman.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.IsTrue(result.Duration.Ticks > 0, result.Reason.ToString(), "First result duration should be greater than 0");
                var offset = result.OffsetInTicks;
                var expectedNextOffset = offset + result.Duration.Ticks;

                var result2 = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                var offset2 = result2.OffsetInTicks;

                Console.WriteLine($"Offset1 {offset}, offset1 + duration {expectedNextOffset}");
                Console.WriteLine($"Offset2 {offset2}, its duration {result2.Duration.Ticks}");
                Console.WriteLine($"Result1: {result.ToString()}");
                Console.WriteLine($"Result2: {result2.ToString()}");

                Assert.AreEqual(result.Reason, ResultReason.RecognizedSpeech);
                AssertStringContains(result.Text, "detective skills");
                Assert.IsTrue(result.Duration.Ticks > 0, $"Result duration {result.Duration.Ticks} in {result.ToString()} should be greater than 0");

                Assert.AreEqual(result2.Reason, ResultReason.RecognizedSpeech);
                Assert.IsTrue(offset2 >= expectedNextOffset, $"Offset of the second recognition {offset2} should be greater or equal than offset of the first plus duration {expectedNextOffset}.");
            }
        }

        [TestMethod]
        public async Task TestStartStopManyTimes()
        {
            const int NumberOfIterations = 100;
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Batman.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                string canceled = string.Empty;
                recognizer.Canceled += (s, e) => { canceled = e.ErrorDetails; };
                for (int i = 0; i < NumberOfIterations; ++i)
                {
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(canceled))
                {
                    Assert.Fail($"Recognition Canceled: {canceled}");
                }
            }
        }


        [TestMethod]
        public async Task TestContinuousRecognitionTwice()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Batman.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                string canceled = string.Empty;
                recognizer.Canceled += (s, e) => { canceled = e.ErrorDetails; };
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);
                await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                if (!string.IsNullOrEmpty(canceled))
                {
                    Assert.Fail($"Recognition Canceled: {canceled}");
                }
            }
        }

        [TestMethod]
        public async Task TestInitialSilenceTimeout()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Silence.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                Assert.AreEqual(ResultReason.NoMatch, result.Reason);
                Assert.IsTrue(result.OffsetInTicks > 0, result.OffsetInTicks.ToString());
                Assert.IsTrue(String.IsNullOrEmpty(result.Text), result.Text);

                var noMatch = NoMatchDetails.FromResult(result);
                Assert.AreEqual(NoMatchReason.InitialSilenceTimeout, noMatch.Reason);
            }
        }

        [TestMethod]
        public void TestPropertyCollectionWithoutRecognizer()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Silence.AudioFile);
            PropertyCollection properties;
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                properties = recognizer.Properties;
            }
            Assert.AreEqual("", properties.GetProperty(PropertyId.SpeechServiceAuthorization_Token));
        }

        [TestMethod, TestCategory(TestCategory.LongRunning)]
        public async Task TestContinuous44KHz()
        {
            var audioInput = AudioConfig.FromWavFileInput(TestData.English.Margarita.AudioFile);
            using (var recognizer = TrackSessionId(new SpeechRecognizer(this.config, audioInput)))
            {
                List<string> recognizedText = new List<string>();
                helper.SubscribeToCounterEventHandlers(recognizer);
                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Text.Length > 0)
                    {
                        recognizedText.Add(e.Result.Text);
                    }
                };

                await helper.CompleteContinuousRecognition(recognizer);

                Assert.AreEqual(1, recognizedText.Count);
                AssertMatching(TestData.English.Margarita.Utterance, recognizedText[0]);
            }
        }
    }
}