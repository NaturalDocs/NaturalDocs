/*
 * Class: CodeClear.NaturalDocs.Engine.UTF8Marshaller
 * ____________________________________________________________________________
 *
 * A custom marshaller because .NET inexplicably doesn't have one for UTF-8.  Also, I refuse to spell it with one L.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Runtime.InteropServices;
using System.Text;


namespace CodeClear.NaturalDocs.Engine
	{
	public class UTF8Marshaller : ICustomMarshaler
		{
		public IntPtr MarshalManagedToNative (object managedObject)
			{
			if (managedObject == null || (managedObject is string) == false)
				{  return IntPtr.Zero;  }

			// Convert string to UTF-8 bytes.  The result is not null terminated.
			byte[] utf8Bytes = Encoding.UTF8.GetBytes((string)managedObject);

			// Create a native buffer.  +1 to the length for the null terminator.
			IntPtr nativeBuffer = Marshal.AllocHGlobal(utf8Bytes.Length + 1);

			Marshal.Copy(utf8Bytes, 0, nativeBuffer, utf8Bytes.Length);

			// Add the null terminator
			Marshal.WriteByte(nativeBuffer, utf8Bytes.Length, 0);

			return nativeBuffer;
			}

		public object MarshalNativeToManaged (IntPtr nativeBuffer)
			{
			if (nativeBuffer == IntPtr.Zero)
				{  return null;  }

			unsafe
				{
				sbyte* start = (sbyte*)nativeBuffer;
				sbyte* end = start;

				// Find the null
				while (*end != 0)
					{  end++;  }

				if (start == end)
					{  return string.Empty;  }
				else
					{
					// End is on the null, but that's okay because we don't want to include it in the string constructor.
					int length = (int)(end - start);
					return new string(start, 0, length, Encoding.UTF8);
					}
				}
			}

		public void CleanUpNativeData (IntPtr nativeBuffer)
			{
			if (nativeBuffer != IntPtr.Zero)
				{  Marshal.FreeHGlobal(nativeBuffer);  }
			}

		public void CleanUpManagedData (object managedObject)
			{
			}

		public int GetNativeDataSize()
			{
			return 0;
			}

		static public ICustomMarshaler GetInstance (string cookie = null)
			{
			return staticInstance;
			}

		private static UTF8Marshaller staticInstance = new UTF8Marshaller();

		}
	}
