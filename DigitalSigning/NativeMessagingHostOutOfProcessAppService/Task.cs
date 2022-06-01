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
        private BackgroundTaskDeferral centennialAppServiceDeferral = null;
        private AppServiceConnection centennialConnection = null;
        private bool centennialAppLaunched = true;
        private int currentConnectionIndex = 0;

        static int connectionIndex = 0;
        static int centennialConnectionIndex = 0;
        static Dictionary<int, AppServiceConnection> connections = new Dictionary<int, AppServiceConnection>();
        static Dictionary<int, AppServiceConnection> centennialConnections = new Dictionary<int, AppServiceConnection>();
        static Dictionary<int, BackgroundTaskDeferral> appServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        static Dictionary<int, BackgroundTaskDeferral> centennialAppServiceDeferrals = new Dictionary<int, BackgroundTaskDeferral>();
        static Object thisLock = new Object();

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            if (taskInstance.TriggerDetails is AppServiceTriggerDetails)
            {
                AppServiceTriggerDetails appService = taskInstance.TriggerDetails as AppServiceTriggerDetails;
                if (appService.CallerPackageFamilyName == Windows.ApplicationModel.Package.Current.Id.FamilyName) // App service connection from Centennial App
                {
                    this.centennialAppServiceDeferral = taskInstance.GetDeferral(); // Get a deferral so that the service isn't terminated.
                    taskInstance.Canceled += OnCentennialAppServicesCanceled; // Associate a cancellation handler with the background task.
                    this.centennialConnection = appService.AppServiceConnection;
                    this.centennialConnection.RequestReceived += OnCentennialAppServiceRequestReceived;
                    this.centennialConnection.ServiceClosed += CentennialAppServiceConnection_ServiceClosed;

                    this.centennialConnection.AppServiceName = centennialConnectionIndex.ToString();
                    centennialConnections.Add(centennialConnectionIndex, this.centennialConnection);
                    centennialAppServiceDeferrals.Add(centennialConnectionIndex, this.centennialAppServiceDeferral);
                    centennialConnectionIndex++;
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
                        this.centennialAppLaunched = false;
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
                if (this.centennialAppLaunched)
                {
                    this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
                    this.centennialConnection = centennialConnections[this.currentConnectionIndex];

                    // Send message to the Centennial component and wait for response
                    AppServiceResponse centennialResponse = await this.centennialConnection.SendMessageAsync(args.Request.Message);
                    await args.Request.SendResponseAsync(centennialResponse.Message);
                }
                else
                {
                    throw new Exception("Failed to launch Centennial App!");
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
            this.centennialConnection = centennialConnections[this.currentConnectionIndex];
            this.centennialConnection.Dispose();
            centennialConnections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.centennialAppServiceDeferral = centennialAppServiceDeferrals[this.currentConnectionIndex];
            centennialAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.centennialAppServiceDeferral != null)
            {
                this.centennialAppServiceDeferral.Complete();
                this.centennialAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
            this.centennialConnection = centennialConnections[this.currentConnectionIndex];
            this.centennialConnection.Dispose();
            centennialConnections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.centennialAppServiceDeferral = centennialAppServiceDeferrals[this.currentConnectionIndex];
            centennialAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.centennialAppServiceDeferral != null)
            {
                this.centennialAppServiceDeferral.Complete();
                this.centennialAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Receives message from Centennial App
        /// </summary>
        private async void OnCentennialAppServiceRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
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
        private void OnCentennialAppServicesCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            AppServiceTriggerDetails appService = sender.TriggerDetails as AppServiceTriggerDetails;
            this.currentConnectionIndex = Int32.Parse(appService.AppServiceConnection.AppServiceName);
            this.connection = connections[this.currentConnectionIndex];
            this.connection.Dispose();
            connections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.centennialAppServiceDeferral = centennialAppServiceDeferrals[this.currentConnectionIndex];
            centennialAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.centennialAppServiceDeferral != null)
            {
                this.centennialAppServiceDeferral.Complete();
                this.centennialAppServiceDeferral = null;
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private void CentennialAppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            this.currentConnectionIndex = Int32.Parse(sender.AppServiceName);
            this.connection = connections[this.currentConnectionIndex];
            this.connection.Dispose();
            connections.Remove(this.currentConnectionIndex);

            this.appServiceDeferral = appServiceDeferrals[this.currentConnectionIndex];
            appServiceDeferrals.Remove(this.currentConnectionIndex);
            this.centennialAppServiceDeferral = centennialAppServiceDeferrals[this.currentConnectionIndex];
            centennialAppServiceDeferrals.Remove(this.currentConnectionIndex);
            if (this.appServiceDeferral != null)
            {
                this.appServiceDeferral.Complete();
                this.appServiceDeferral = null;
            }
            if (this.centennialAppServiceDeferral != null)
            {
                this.centennialAppServiceDeferral.Complete();
                this.centennialAppServiceDeferral = null;
            }
        }
    }
}
