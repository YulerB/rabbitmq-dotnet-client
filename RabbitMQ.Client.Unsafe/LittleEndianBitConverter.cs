namespace RabbitMQ.Client.Unsafe
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Runtime.CompilerServices;
    using System.Security;

    // The BitConverter class contains methods for
    // converting an array of bytes to one of the base data 
    // types, as well as for converting a base data type to an
    // array of bytes.
    // 
    // Only statics, does not need to be marked with the serializable attribute
    public static class LittleEndianBitConverter
    {
        // Converts an array of bytes into a char.  
        public static char ToChar(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if ((uint)startIndex >= value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (startIndex > value.Length - 2)
            {
                throw new ArgumentException("Array Pluss Off Too Small");
            }
            Contract.EndContractBlock();

            return (char)ToInt16(value, startIndex);
        }

        // Converts an array of bytes into a short.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short ToInt16(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if ((uint)startIndex >= value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (startIndex > value.Length - 2)
            {
                throw new ArgumentException("Array Plus Off Too Small");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                return (short)((*pbyte << 8) | (*(pbyte + 1)));
            }

        }

        // Converts an array of bytes into an int.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int ToInt32(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if ((uint)startIndex >= value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (startIndex > value.Length - 4)
            {
                throw new ArgumentException("Array Plus Off Too Small");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                return (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
            }
        }

        // Converts an array of bytes into a long.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe long ToInt64(byte[] value, int startIndex)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if ((uint)startIndex >= value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            }

            if (startIndex > value.Length - 8)
            {
                throw new ArgumentException("Array Plus Off Too Small");
            }
            Contract.EndContractBlock();

            fixed (byte* pbyte = &value[startIndex])
            {
                int i1 = (*pbyte << 24) | (*(pbyte + 1) << 16) | (*(pbyte + 2) << 8) | (*(pbyte + 3));
                int i2 = (*(pbyte + 4) << 24) | (*(pbyte + 5) << 16) | (*(pbyte + 6) << 8) | (*(pbyte + 7));
                return (uint)i2 | ((long)i1 << 32);
            }
        }


        // Converts an array of bytes into an ushort.
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex >= value.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > value.Length - 2)
                throw new ArgumentException("Array Plus Off Too Small");
            Contract.EndContractBlock();

            return (ushort)ToInt16(value, startIndex);
        }

        // Converts an array of bytes into an uint.
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ToUInt32(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex >= value.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > value.Length - 4)
                throw new ArgumentException("Array Plus Off Too Small");
            Contract.EndContractBlock();

            return (uint)ToInt32(value, startIndex);
        }

        // Converts an array of bytes into an unsigned long.
        // 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ToUInt64(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex >= value.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > value.Length - 8)
                throw new ArgumentException("Array Plus Off Too Small");
            Contract.EndContractBlock();

            return (ulong)ToInt64(value, startIndex);
        }

        // Converts an array of bytes into a float.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static float ToSingle(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex >= value.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > value.Length - 4)
                throw new ArgumentException("Array Plus Off Too Small");
            Contract.EndContractBlock();

            int val = ToInt32(value, startIndex);
            return *(float*)&val;
        }

        // Converts an array of bytes into a double.  
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe public static double ToDouble(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if ((uint)startIndex >= value.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));
            if (startIndex > value.Length - 8)
                throw new ArgumentException("Array Plus Off Too Small");
            Contract.EndContractBlock();

            long val = ToInt64(value, startIndex);
            return *(double*)&val;
        }

        private static char GetHexValue(int i)
        {
            Contract.Assert(i >= 0 && i < 16, "i is out of range.");
            if (i < 10)
            {
                return (char)(i + '0');
            }

            return (char)(i - 10 + 'A');
        }


        /*==================================ToBoolean===================================
        **Action:  Convert an array of bytes to a boolean value.  We treat this array 
        **         as if the first 4 bytes were an Int4 an operate on this value.
        **Returns: True if the Int4 value of the first 4 bytes is non-zero.
        **Arguments: value -- The byte array
        **           startIndex -- The position within the array.
        **Exceptions: See ToInt4.
        ==============================================================================*/
        // Converts an array of bytes into a boolean.  
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBoolean(byte[] value, int startIndex)
        {
            if (value == null)
                throw new ArgumentNullException("value");
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", "Need Non Neg Num");
            if (startIndex > value.Length - 1)
                throw new ArgumentOutOfRangeException("startIndex");
            Contract.EndContractBlock();

            return (value[startIndex] == 0) ? false : true;
        }


    }

}
