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

#include "stdafx.h"
#include "SignHash.h"

using namespace std;
using namespace concurrency;
using namespace Platform;
using namespace Windows::ApplicationModel::AppService;
using namespace Windows::ApplicationModel::DataTransfer;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;
using namespace Windows::Security::Cryptography;
using namespace Windows::Storage::Streams;

HANDLE _terminateHandle;
AppServiceConnection^ _connection = nullptr;
void RequestReceived(AppServiceConnection^ connection, AppServiceRequestReceivedEventArgs^ args);
void ServiceClosed(AppServiceConnection^ connection, AppServiceClosedEventArgs^ args);

/// <summary>
/// Creates the app service connection
/// </summary>
IAsyncAction^ ConnectToAppServiceAsync()
{
    return create_async([]
    {
        // Get the package family name
        Windows::ApplicationModel::Package^ package = Windows::ApplicationModel::Package::Current;
        Platform::String^ packageFamilyName = package->Id->FamilyName;

        // Create and set the connection
        auto connection = ref new AppServiceConnection();
        connection->PackageFamilyName = packageFamilyName;
        connection->AppServiceName = "NativeMessagingHostInProcessService"; // Change to "NativeMessagingHostOutOfProcessService" for out-of-proc AppService model
        SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (FOREGROUND_GREEN));
        cout << "Opening connection..." << endl;

        // Open the connection
        create_task(connection->OpenAsync()).then([connection](AppServiceConnectionStatus status)
        {
            if (status == AppServiceConnectionStatus::Success)
            {
                _connection = connection;
                wcout << "Successfully opened connection." << endl;
                _connection->RequestReceived += ref new TypedEventHandler<AppServiceConnection^, AppServiceRequestReceivedEventArgs^>(RequestReceived);
                _connection->ServiceClosed += ref new TypedEventHandler<AppServiceConnection^, AppServiceClosedEventArgs^>(ServiceClosed);
            }
            else if (status == AppServiceConnectionStatus::AppUnavailable || status == AppServiceConnectionStatus::AppServiceUnavailable)
            {
                SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (FOREGROUND_RED));
                cout << "AppService Unavailable" << endl;
            }
        });
    });
}

/// <summary>
/// Creates an app service thread
/// </summary>
int main(Platform::Array<Platform::String^>^ args)
{
    _terminateHandle = CreateEvent(NULL, TRUE, FALSE, NULL);

    SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (FOREGROUND_RED | FOREGROUND_GREEN));
    wcout << "*********************************" << endl;
    wcout << "**** Classic desktop C++ app ****" << endl;
    wcout << "*********************************" << endl << endl;
    wcout << L"Creating app service connection" << endl << endl;

    ConnectToAppServiceAsync();

    WaitForSingleObject(_terminateHandle, INFINITE);
    CloseHandle(_terminateHandle);

    return 0;
}

std::string wstos(std::wstring ws)
{
    std::string s(ws.begin(), ws.end());
    return s;
}

std::string pstos(String^ ps)
{
    return wstos(std::wstring(ps->Data()));
}

/// <summary>
/// Receives message from UWP app and sends a response back
/// </summary>
void RequestReceived(AppServiceConnection^ connection, AppServiceRequestReceivedEventArgs^ args)
{
    auto deferral = args->GetDeferral();
    auto message = args->Request->Message;
    auto method = message->Lookup(L"message")->ToString();

    SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (FOREGROUND_RED | FOREGROUND_GREEN));
    wcout << method->Data();
    wcout << L" - request received" << endl;
    SetConsoleTextAttribute(GetStdHandle(STD_OUTPUT_HANDLE), (FOREGROUND_GREEN));

    std::string transactionText = pstos(method);

    BYTE* pszBase64P1 = nullptr;
    DWORD length = 0;

    create_task([transactionText, &pszBase64P1, &length]
    {
        SignMessage(transactionText.c_str(), &pszBase64P1, &length);
        return *pszBase64P1;
    }).get();

    Array<BYTE>^ arr = ref new Array<BYTE>(pszBase64P1, length);
    IBuffer^ buffer = CryptographicBuffer::CreateFromByteArray(arr);
    String^ signature = CryptographicBuffer::EncodeToBase64String(buffer);
    auto response = ref new ValueSet();
    response->Insert("message", signature);
    wcout << L"Sending response: " << signature->Data() << endl;

    create_task(args->Request->SendResponseAsync(response)).then([deferral](AppServiceResponseStatus status)
    {
        deferral->Complete();
    });
}

/// <summary>
/// Occurs when the other endpoint closes the connection to the app service
/// </summary>
void ServiceClosed(AppServiceConnection^ connection, AppServiceClosedEventArgs^ args)
{
    cout << "ServiceClosed..." << endl;
    SetEvent(_terminateHandle);
}
