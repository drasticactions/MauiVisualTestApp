using MauiVisualTestApp.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit.Runners.Maui;
using System.Security.Cryptography;
using Xunit;
using Drastic.ImageHash.HashAlgorithms;
using Drastic.ImageHash;
using Xunit.Abstractions;

namespace MauiVisualTestApp.Platforms.Android
{
    public class AndroidUnitTests
    {
        readonly ITestOutputHelper _output;
        private Window window;
        private DifferenceHash algorithm = new Drastic.ImageHash.HashAlgorithms.DifferenceHash();
        public AndroidUnitTests(ITestOutputHelper output)
        {
            _output = output;
            this.window = new Window() { Page = new StartingPage() };
            var application = TestServices.Services.GetService<IApplication>();
            application.OpenWindow(window);
        }

        [Xunit.Theory]
        [InlineData(typeof(ButtonPage), 8847833956352)]
        public async void TestPages(Type type, ulong hash)
        {
            var eventFired = new ManualResetEventSlim(false);
            var timeout = TimeSpan.FromSeconds(10);

            var page = (Page)Activator.CreateInstance(type);

            EventHandler handler = (sender, args) =>
            {
                eventFired.Set();
            };

            page.Appearing += handler;
            window.Page = page;

            try
            {
                // Wait for the event to fire or the timeout to elapse
                if (!eventFired.Wait(timeout))
                {
                    throw new TimeoutException("Event did not fire within the specified timeout.");
                }

                await Task.Delay(5000);
                var image = await VisualDiagnostics.CaptureAsPngAsync(window);
                var newHash = algorithm.Hash(image);
                var compare = CompareHash.Similarity(hash, newHash);
                Assert.True(compare >= 99, $"{compare}%");
            }
            finally
            {
                // Unregister the event handler
                page.Appearing -= handler;
            }
        }
    }
}
