using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace PasswordInputProtection
{
    class EncryptUtils
    {
        public static byte[] Sha1(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            SHA1 sha = new SHA1CryptoServiceProvider();
            return sha.ComputeHash(bytes);
        }

        public static string Sha1ByteToString(byte[] bytes, bool upperCase)
        {
            StringBuilder str = new StringBuilder(bytes.Length * 2);
            for (int i = 0; i < bytes.Length; i++)
            {
                str.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));
            }
            return str.ToString();
        }

        public static string SecureStringToString(SecureString value)
        {
            IntPtr valuePtr = IntPtr.Zero;
            try
            {
                valuePtr = Marshal.SecureStringToGlobalAllocUnicode(value);
                return Marshal.PtrToStringUni(valuePtr);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(valuePtr);
            }
        }
    }
}
