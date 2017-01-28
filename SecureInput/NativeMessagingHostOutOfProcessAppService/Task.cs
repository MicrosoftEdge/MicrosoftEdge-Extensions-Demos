using System;
using System.Collections.Generic;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.Data.Json;
using Windows.UI.Popups;

namespace NativeMessagingHostOutOfProcessAppService
{
    public sealed class Task : IBackgroundTask
    {
        private BackgroundTaskDeferral appServiceDeferral = null;
        private AppServiceConnection connection = null;
        private BackgroundTaskDeferral desktopBridgeAppServiceDeferral = null;
        private AppServiceConnection desktopBridgeConnection = null;
        private bool desktopBridgeAppLaunched = true;
        private int currentConnectionIndex = 0;

        static int connectionIndex = 0;
        static int desktopBridgeConnectionIndex = 0;
        static Dictionary<int, AppServiceConnection> connections = new Dictionary<int, AppServiceConnection>();
        static Dictionary<int, AppServiceConnection> desktopBridgeConnections = new Dictionary<int, AppServiceConnection>();
        static Dictionary<int, BackgroundTaskDeferral> appServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        static Dictionary<int, BackgroundTaskDeferral> desktopBridgeAppServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        static Object thisLock = new Object();

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
                if (appService.CallerPackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName) // App service connection from desktopBridge App
                {
                    this.desktopBridgeAppServiceDeferral = taskInstance.GetDeferral(); // Get a deferral so that the service isn't terminated.
                    taskInstance.Canceled += OndesktopBridgeAppServicesCanceled; // Associate a cancellation handler with the background task.
                    this.desktopBridgeConnection = appService.AppServiceConnection;
                    this.desktopBridgeConnection.RequestReceived += OndesktopBridgeAppServiceRequestReceived;
                    this.desktopBridgeConnection.ServiceClosed += desktopBridgeAppServiceConnection_ServiceClosed;

                    this.desktopBridgeConnection.AppServiceName = desktopBridgeConnectionIndex.ToString();
                    desktopBridgeConnections.Add(desktopBridgeConnectionIndex, this.desktopBridgeConnection);
                    desktopBridgeAppServiceDeferrals.Add(desktopBridgeConnectionIndex, this.desktopBridgeAppServiceDeferral);
                    desktopBridgeConnectionIndex++;
                }
                else // App service connection from Edge browser
                {
                    this.appServiceDeferral = taskInstance.GetDeferral(); // Get a deferral so that the service isn't terminated.
                    taskInstance.Canceled += OnAppServicesCanceled; // Associate a cancellation handler with the background task.
                    this.connection = appService.AppServiceConnection;
                    this.connection.RequestReceived += OnAppServiceRequestReceived;
                    this.connection.ServiceClosed += AppServiceConnection_ServiceClosed;

                    this.connection.AppServiceName = connectionIndex.ToString();
                    connections.Add(connectionIndex, this.connection);
                    appServiceDeferrals.Add(connectionIndex, this.appServiceDeferral);
                    connectionIndex++;

                    try
                    {
                        // Make sure the BackgroundProcess is in your AppX folder, if not rebuild the solution
                        await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync();
                    }
                    catch (Exception)
                    {
                        this.desktopBridgeAppLaunched = false;
                        MessageDialog dialog = new MessageDialog("Rebuild the solution and make sure the BackgroundProcess is in your AppX folder");
                        await dialog.ShowAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Receives message from Extension (via Edge)
        /// </summary>
        private async void OnAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            try
            {
                if (this.desktopBridgeAppLaunched)
                {
                    this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
                    this.desktopBridgeConnection = desktopBridgeConnections[this.currentConnectionIndex];

                    // Send message to the desktopBridge component and wait for response
                    AppServiceResponse desktopBridgeResponse = await this.desktopBridgeConnection.SendMessageAsync(args.Request.Message);
                    await args.Request.SendResponseAsync(desktopBridgeResponse.Message);
                }
                else
                {
                    throw new Exception("Failed to launch desktopBridge App!");
                }
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Associate the cancellation handler with the background task 
        /// </summary>
        private void OnAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            AppServiceTriggerDetails appService = sender.TriggerDetails as AppServiceTriggerDetails;
            this.currentConnectionIndex = Int32.Parse(appService.AppServiceConnection.AppServiceName);
            this.desktopBridgeConnection = desktopBridgeConnections[this.currentConnectionIndex];
            this.desktopBridgeConnection.Dispose();
            desktopBridgeConnections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.desktopBridgeAppServiceDeferral = desktopBridgeAppServiceDeferrals[this.currentConnectionIndex];
            desktopBridgeAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.desktopBridgeAppServiceDeferral != null)
            {
                this.desktopBridgeAppServiceDeferral.Complete();
                this.desktopBridgeAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
            this.desktopBridgeConnection = desktopBridgeConnections[this.currentConnectionIndex];
            this.desktopBridgeConnection.Dispose();
            desktopBridgeConnections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.desktopBridgeAppServiceDeferral = desktopBridgeAppServiceDeferrals[this.currentConnectionIndex];
            desktopBridgeAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.desktopBridgeAppServiceDeferral != null)
            {
                this.desktopBridgeAppServiceDeferral.Complete();
                this.desktopBridgeAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Receives message from desktopBridge App
        /// </summary>
        private async void OndesktopBridgeAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            try
            {
                this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
                this.connection = connections[this.currentConnectionIndex];

                await this.connection.SendMessageAsync(args.Request.Message);
            }
            finally
            {
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Associate the cancellation handler with the background task 
        /// </summary>
        private void OndesktopBridgeAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            AppServiceTriggerDetails appService = sender.TriggerDetails as AppServiceTriggerDetails;
            this.currentConnectionIndex = Int32.Parse(appService.AppServiceConnection.AppServiceName);
            this.connection = connections[this.currentConnectionIndex];
            this.connection.Dispose();
            connections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.desktopBridgeAppServiceDeferral = desktopBridgeAppServiceDeferrals[this.currentConnectionIndex];
            desktopBridgeAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.desktopBridgeAppServiceDeferral != null)
            {
                this.desktopBridgeAppServiceDeferral.Complete();
                this.desktopBridgeAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void desktopBridgeAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
            this.connection = connections[this.currentConnectionIndex];
            this.connection.Dispose();
            connections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.desktopBridgeAppServiceDeferral = desktopBridgeAppServiceDeferrals[this.currentConnectionIndex];
            desktopBridgeAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.desktopBridgeAppServiceDeferral != null)
            {
                this.desktopBridgeAppServiceDeferral.Complete();
                this.desktopBridgeAppServiceDeferral = null;
            }
        }
    }
}
