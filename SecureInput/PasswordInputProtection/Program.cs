//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace PasswordInputProtection
{
    class Program
    {
        static AppServiceConnection connection = null;

        private static KeyboardHook mKeyboardHook;
        private static bool mNeedHook = false;
        private static SecureString mSecurePassword;

        /// <summary>
        /// Creates an app service thread
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            mSecurePassword = new SecureString();
            InstallHook();

            Thread appServiceThread = new Thread(new ThreadStart(ThreadProc));
            appServiceThread.Start();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("*****************************");
            Console.WriteLine("**** Classic desktop app ****");
            Console.WriteLine("*****************************");
            Application.Run();
        }

        /// <summary>
        /// Creates the app service connection
        /// </summary>
        static async void ThreadProc()
        {
            connection = new AppServiceConnection();
            connection.AppServiceName = "NativeMessagingHostInProcessService"; // Change to "NativeMessagingHostOutOfProcessService" for out-of-proc AppService model
            connection.PackageFamilyName = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            connection.RequestReceived += Connection_RequestReceived;
            connection.ServiceClosed += Connection_ServiceClosed;

            AppServiceConnectionStatus status = await connection.OpenAsync();
            switch (status)
            {
                case AppServiceConnectionStatus.Success:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Connection established - waiting for requests");
                    Console.WriteLine();
                    break;
                case AppServiceConnectionStatus.AppNotInstalled:
                    UninstallHook();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The app AppServicesProvider is not installed.");
                    return;
                case AppServiceConnectionStatus.AppUnavailable:
                    UninstallHook();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The app AppServicesProvider is not available.");
                    return;
                case AppServiceConnectionStatus.AppServiceUnavailable:
                    UninstallHook();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("The app AppServicesProvider is installed but it does not provide the app service {0}.", connection.AppServiceName));
                    return;
                case AppServiceConnectionStatus.Unknown:
                    UninstallHook();
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Format("An unkown error occurred while we were trying to open an AppServiceConnection."));
                    return;
            }
        }

        /// <summary>
        /// Occurs when the other endpoint closes the connection to the app service
        /// </summary>
        private static void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            UninstallHook();
            Application.Exit();
        }

        /// <summary>
        /// Receives message from UWP app and sends a response back
        /// </summary>
        private static void Connection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            string key = args.Request.Message.First().Key;
            string value = args.Request.Message.First().Value.ToString();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Format("Received message '{0}' with value '{1}'", key, value));

            var json_serializer = new JavaScriptSerializer();
            var value_list = (IDictionary<string, object>)json_serializer.DeserializeObject(value);
            string messageType = value_list["MessageType"].ToString();

            switch (messageType)
            {
                case "focus":
                    Console.WriteLine("focus received");
                    mNeedHook = true;
                    ValueSet focusResponse = new ValueSet();
                    focusResponse.Add("message", "focus received\r\n");
                    args.Request.SendResponseAsync(focusResponse).Completed += delegate { };
                    break;
                case "focusout":
                    Console.WriteLine("focusout received");
                    mNeedHook = false;
                    ValueSet focusoutResponse = new ValueSet();
                    focusoutResponse.Add("message", "focusout received\r\n");
                    args.Request.SendResponseAsync(focusoutResponse).Completed += delegate { };
                    break;
                case "submit":
                    Console.WriteLine("submit received");
                    ValueSet submitResponse = new ValueSet();
                    string encryptPassword = EncryptUtils.Sha1ByteToString(EncryptUtils.Sha1(EncryptUtils.SecureStringToString(mSecurePassword)), false);
                    Console.WriteLine(string.Format("Original password: '{0}', Encrypted password: '{1}'", EncryptUtils.SecureStringToString(mSecurePassword), encryptPassword));
                    mSecurePassword.Clear();
                    submitResponse.Add("message", encryptPassword);
                    args.Request.SendResponseAsync(submitResponse).Completed += delegate { };
                    break;
                case "quit":
                    UninstallHook();
                    Application.Exit();
                    break;
                default:
                    Console.WriteLine(messageType);
                    break;
            }
        }

        private static void HandleErrorException(Exception exception)
        {
            UninstallHook();
        }

        private static void InstallHook()
        {
            if (mKeyboardHook == null)
            {
                mKeyboardHook = new KeyboardHook();
                mKeyboardHook.KeyPressEvent += new KeyPressEventHandler(OnKeyPressed);
                mKeyboardHook.KeyUpEvent += new KeyEventHandler(OnKeyUp);
                mKeyboardHook.Start();
            }
        }

        private static void UninstallHook()
        {
            if (mKeyboardHook != null)
            {
                mKeyboardHook.Stop();
                mKeyboardHook = null;
            }
        }

        private static void OnKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = false;

            if (!mNeedHook)
            {
                return;
            }

            if (Char.IsControl((Char)e.KeyCode))
            {
                if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
                {
                    if (mSecurePassword != null && mSecurePassword.Length > 0)
                    {
                        mSecurePassword.RemoveAt(mSecurePassword.Length - 1);
                        ValueSet valueSet = new ValueSet();
                        valueSet.Add("message", "delete");
                        connection.SendMessageAsync(valueSet).Completed += delegate { };
                        Console.WriteLine(string.Format("send message '{0}'", "delete"));
                    }
                }
            }
        }

        private static void OnKeyPressed(object sender, KeyPressEventArgs e)
        {
            e.Handled = true;

            if (!mNeedHook)
            {
                e.Handled = false;
                return;
            }

            if ((connection != null) && (!Char.IsControl(e.KeyChar)))
            {
                mSecurePassword.AppendChar(e.KeyChar);
                Console.WriteLine(string.Format("Length of SecurePassword: '{0}'", mSecurePassword.Length));
                ValueSet valueSet = new ValueSet();
                valueSet.Add("message", "*");
                connection.SendMessageAsync(valueSet).Completed += delegate { };
                Console.WriteLine(string.Format("send message '{0}' with value '{1}'", e.KeyChar, '*'));
            }
        }
    }
}
