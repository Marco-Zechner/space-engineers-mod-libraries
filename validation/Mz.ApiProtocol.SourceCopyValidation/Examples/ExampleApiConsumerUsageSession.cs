using Example.EchoApi;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Example.ApiConsumerMod
{
    /// <summary>
    /// Minimal example showing what a downstream modder writes after copying
    /// ExampleEchoApi.cs into their mod.
    ///
    /// This file contains no ApiProtocol discovery or endpoint code.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ExampleApiConsumerUsageSession :
        MySessionComponentBase
    {
        public override void BeforeStart()
        {
            // Subscribe before Init. Discovery can complete synchronously when
            // the provider is already running.
            ExampleEchoApi.Ready += OnApiReady;
            ExampleEchoApi.Unavailable += OnApiUnavailable;

            ExampleEchoApi.Init(
                "example.echo-api-consumer",
                "Example Echo API Consumer",
                1,
                0,
                0
            );

            // IsReady may already be true when Init returns. The Ready event
            // above has already run in that case.
            if (!ExampleEchoApi.IsReady)
            {
                ShowMessage(
                    "Waiting for the Example Echo API provider."
                );
            }
        }

        protected override void UnloadData()
        {
            ExampleEchoApi.Ready -= OnApiReady;
            ExampleEchoApi.Unavailable -= OnApiUnavailable;
            ExampleEchoApi.Close();
        }

        private static void OnApiReady()
        {
            // From this point onward the consuming mod calls normal,
            // strongly-typed methods. It does not deal with discovery,
            // endpoint names, delegates, or compatibility checks.
            string echoResult = ExampleEchoApi.Echo(
                "Hello from the consuming mod"
            );

            int sumResult = ExampleEchoApi.Add(20, 22);

            ShowMessage(
                echoResult
                + ". 20 + 22 = "
                + sumResult
                + "."
            );
        }

        private static void OnApiUnavailable()
        {
            ShowMessage(
                ExampleEchoApi.LastError
                ?? "The Example Echo API is unavailable."
            );
        }

        private static void ShowMessage(string message)
        {
            if (MyAPIGateway.Utilities == null)
                return;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.Utilities.ShowMessage(
                "API Consumer",
                message
            );
        }
    }
}
