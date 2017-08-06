//--------------------------------------------------------------------
// Copyright (C) Microsoft.  All rights reserved.
// Example of signing a hash

#include "stdafx.h"

#include <stdio.h>
#include <windows.h>
#include <Wincrypt.h>
#include <cryptuiapi.h>
#include "SignHash.h"

#pragma comment(lib, "crypt32.lib")
#pragma comment(lib, "cryptui.lib")

#define MY_ENCODING_TYPE  (PKCS_7_ASN_ENCODING | X509_ASN_ENCODING)

void MyHandleError(char *s);

//----------------------------------------------------------------------------
// FindCertificateBySubjectName
//
//----------------------------------------------------------------------------
HRESULT FindCertificateBySubjectName(
    LPCWSTR			wszStore,
    LPCWSTR			wszSubject,
    PCCERT_CONTEXT	*ppcCert
)
{
    HRESULT hr = S_OK;
    HCERTSTORE  hStoreHandle = NULL;  // The system store handle.

    *ppcCert = NULL;

    //-------------------------------------------------------------------
    // Open the certificate store to be searched.
    hStoreHandle = CertOpenStore(
        CERT_STORE_PROV_SYSTEM,          // the store provider type
        0,                               // the encoding type is not needed
        NULL,                            // use the default HCRYPTPROV
        CERT_SYSTEM_STORE_CURRENT_USER,  // set the store location in a 
                                         // registry location
        wszStore                         // the store name 
    ); 

    if (NULL == hStoreHandle)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        goto CleanUp;
    }

    //-------------------------------------------------------------------
    // Get a certificate that has the specified Subject Name
    *ppcCert = CertFindCertificateInStore(
        hStoreHandle,
        X509_ASN_ENCODING,         // Use X509_ASN_ENCODING
        0,                         // No dwFlags needed
        CERT_FIND_SUBJECT_STR,     // Find a certificate with a
                                   //  subject that matches the 
                                   //  string in the next parameter
        wszSubject,                // The Unicode string to be found
                                   //  in a certificate's subject
        NULL);                     // NULL for the first call to the
                                   //  function; In all subsequent
                                   //  calls, it is the last pointer
                                   //  returned by the function

    if (NULL == *ppcCert)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());

        goto CleanUp;
    }

CleanUp:

    if (NULL != hStoreHandle)
    {
        CertCloseStore(hStoreHandle, 0);
    }

    return hr;
}

void SignMessage(const char*  pszMsg,
    BYTE ** ppszSignature, DWORD* pLength)
{
    //-------------------------------------------------------------------
    // Declare and initialize variables.
    HCRYPTPROV hProv;
    HCRYPTHASH hHash;
    BYTE *pbSignature;
    DWORD dwSigLen;

    // Certificate to be used to sign data
    PCCERT_CONTEXT pCertContext = NULL;
    LPCWSTR pwszStoreName = L"MY"; // by default, MY

    // Subject name string of certificate to be used in signing
	// change it to match your own certificate
    LPCWSTR pwszCName = L"test";

    // Key spec; will be used to determine key type
    DWORD dwKeySpec = 0;

    //-------------------------------------------------------------------
    // Find the test certificate to be validated and obtain a pointer to it
    if (S_OK != FindCertificateBySubjectName(
        pwszStoreName,
        pwszCName,
        &pCertContext
    ))
    {
        MyHandleError("Error during FindCertificateBySubjectName.");
    }

    if (CryptAcquireCertificatePrivateKey(
        pCertContext,
        CRYPT_ACQUIRE_ALLOW_NCRYPT_KEY_FLAG,
        NULL,                                   // Reserved for future use and must be NULL
        &hProv,
        &dwKeySpec,
        NULL))
    {
        printf("Certificate PrivateKey acquired. \n");
    }
    else
    {
        MyHandleError("Error during CryptAcquireCertificatePrivateKey.");
    }

    //-------------------------------------------------------------------
    // Create the hash object.
    if (CryptCreateHash(
        hProv,
        CALG_MD5,
        0,
        0,
        &hHash))
    {
        printf("Hash object created. \n");
    }
    else
    {
        MyHandleError("Error during CryptCreateHash.");
    }

    //-------------------------------------------------------------------
    // Compute the cryptographic hash of the buffer.
    if (CryptHashData(
        hHash,
        (BYTE*)pszMsg,
        strlen(pszMsg) + 1,
        0))
    {
        printf("The data buffer has been hashed.\n");
    }
    else
    {
        MyHandleError("Error during CryptHashData.");
    }

    //-------------------------------------------------------------------
    // Determine the size of the signature and allocate memory.
    dwSigLen = 0;
    if (CryptSignHash(
        hHash,
        dwKeySpec,
        NULL,
        0,
        NULL,
        &dwSigLen))
    {
        printf("Signature length %d found.\n", dwSigLen);
    }
    else
    {
        MyHandleError("Error during CryptSignHash.");
    }

    //-------------------------------------------------------------------
    // Allocate memory for the signature buffer.
    if (pbSignature = (BYTE *)malloc(dwSigLen))
    {
        printf("Memory allocated for the signature.\n");
    }
    else
    {
        MyHandleError("Out of memory.");
    }

    //-------------------------------------------------------------------
    // Sign the hash object.
    if (CryptSignHash(
        hHash,
        dwKeySpec,
        NULL,
        0,
        pbSignature,
        &dwSigLen))
    {
        printf("pbSignature is the hash signature.\n");
    }
    else
    {
        MyHandleError("Error during CryptSignHash.");
    }

    *ppszSignature = pbSignature;

    //-------------------------------------------------------------------
    // Destroy the hash object.
    if (hHash)
    {
        CryptDestroyHash(hHash);
    }

    printf("The hash object has been destroyed.\n");
    printf("The signing phase of this program is completed.\n\n");

    //-------------------------------------------------------------------
    // Release the provider handle.
    if (hProv)
    {
        CryptReleaseContext(hProv, 0);
    }

    if (pCertContext)
    {
        CertFreeCertificateContext(pCertContext);
    }

    *pLength = dwSigLen;
    return;
} //  End of SignMessage

//-------------------------------------------------------------------
//  This example uses the function MyHandleError, a simple error
//  handling function, to print an error message to the  
//  standard error (stderr) file and exit the program. 
//  For most applications, replace this function with one 
//  that does more extensive error reporting.
void MyHandleError(char *s)
{
    fprintf(stderr, "An error occurred in running the program. \n");
    fprintf(stderr, "%s\n", s);
    fprintf(stderr, "Error number %x.\n", GetLastError());
    fprintf(stderr, "Program terminating. \n");
    exit(1);
} // End of MyHandleError