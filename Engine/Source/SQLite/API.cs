/*
 * Class: CodeClear.NaturalDocs.Engine.SQLite.API
 * ____________________________________________________________________________
 *
 * A C# interface to selected SQLite API functions.  See <http://www.sqlite.org/capi3ref.html> for descriptions
 * of the functions and result codes.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

#if !SQLITE_UTF8 && !SQLITE_UTF16
	#define SQLITE_UTF8
#endif

using System;
using System.Runtime.InteropServices;
using System.Text;


namespace CodeClear.NaturalDocs.Engine.SQLite
	{
	public static class API
		{
		// Group: Types
		// __________________________________________________________________________


		public enum Result : int
			{
			OK = 0,
			Error = 1,
			Internal = 2,
			Perm = 3,
			Abort = 4,
			Busy = 5,
			Locked = 6,
			NoMem = 7,
			ReadOnly = 8,
			Interrupt = 9,
			IOErr = 10,
			Corrupt = 11,
			NotFound = 12,
			Full = 13,
			CantOpen = 14,
			Protocol = 15,
			Empty = 16,
			Schema = 17,
			TooBig = 18,
			Constraint = 19,
			Mismatch = 20,
			Misuse = 21,
			NoLFS = 22,
			Auth = 23,
			Format = 24,
			Range = 25,
			NotADB = 26,
			Row = 100,
			Done = 101
			}

		[Flags]
		public enum OpenOption : int
			{
			ReadOnly = 0x00000001,
			ReadWrite = 0x00000002,
			Create = 0x00000004,
			NoMutex = 0x00008000,
			FullMutex = 0x00010000
			}

		public enum DestructorOption : int
			{
			Static = 0,
			Transient = -1
			}

		public enum LimitID : int
			{
			Length = 0,
			SQLLength = 1,
			Column = 2,
			ExpressionDepth = 3,
			CompoundSelect = 4,
			VDBEOps = 5,
			FunctionArguments = 6,
			Attached = 7,
			LikePatternLength = 8,
			VariableNumber = 9,
			TriggerDepth = 10,
			WorkerThreads = 11
			}



		// Group: Functions
		// __________________________________________________________________________


		static public Result Initialize ()
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_initialize();  }
			else
				{  return DLLImport.x86.sqlite3_initialize();  }
			}

		static public Result ShutDown ()
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_shutdown();  }
			else
				{  return DLLImport.x86.sqlite3_shutdown();  }
			}

		static public Result OpenV2 (string filename, out IntPtr connectionHandle, OpenOption options)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_open_v2(filename, out connectionHandle, options, IntPtr.Zero);  }
			else
				{  return DLLImport.x86.sqlite3_open_v2(filename, out connectionHandle, options, IntPtr.Zero);  }
			}

		static public Result CloseV2 (IntPtr connectionHandle)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_close_v2(connectionHandle);  }
			else
				{  return DLLImport.x86.sqlite3_close_v2(connectionHandle);  }
			}

		static public int Limit (IntPtr connectionHandle, LimitID id, int newLimit)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_limit(connectionHandle, id, newLimit);  }
			else
				{  return DLLImport.x86.sqlite3_limit(connectionHandle, id, newLimit);  }
			}

		static public Result ExtendedResultCodes (IntPtr connectionHandle, bool onoff)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_extended_result_codes (connectionHandle, (onoff ? 1 : 0));  }
			else
				{  return DLLImport.x86.sqlite3_extended_result_codes (connectionHandle, (onoff ? 1 : 0));  }
			}

		static public Result BusyTimeout (IntPtr connectionHandle, int milliseconds)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_busy_timeout(connectionHandle, milliseconds);  }
			else
				{  return DLLImport.x86.sqlite3_busy_timeout(connectionHandle, milliseconds);  }
			}

		static public Result PrepareV2 (IntPtr connectionHandle, string statementText, out IntPtr statementHandle)
			{
			IntPtr ignore;

			#if SQLITE_UTF16

				// It wants the length in bytes, not in characters
				if (SystemInfo.Is64Bit)
					{  return DLLImport.x64.sqlite3_prepare16_v2(connectionHandle, statementText, Encoding.Unicode.GetByteCount(statementText), out statementHandle, out ignore);  }
				else
					{  return DLLImport.x86.sqlite3_prepare16_v2(connectionHandle, statementText, Encoding.Unicode.GetByteCount(statementText), out statementHandle, out ignore);  }

			#elif SQLITE_UTF8

				if (SystemInfo.Is64Bit)
					{  return DLLImport.x64.sqlite3_prepare_v2(connectionHandle, statementText, Encoding.UTF8.GetByteCount(statementText), out statementHandle, out ignore);  }
				else
					{  return DLLImport.x86.sqlite3_prepare_v2(connectionHandle, statementText, Encoding.UTF8.GetByteCount(statementText), out statementHandle, out ignore);  }

			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}

		static public Result BindInt (IntPtr statementHandle, int index, int value)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_bind_int(statementHandle, index, value);  }
			else
				{  return DLLImport.x86.sqlite3_bind_int(statementHandle, index, value);  }
			}

		static public Result BindInt64 (IntPtr statementHandle, int index, long value)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_bind_int64(statementHandle, index, value);  }
			else
				{  return DLLImport.x86.sqlite3_bind_int64(statementHandle, index, value);  }
			}

		static public Result BindText (IntPtr statementHandle, int index, string value)
			{
			#if SQLITE_UTF16

				// It wants the length in bytes, not in characters
				if (SystemInfo.Is64Bit)
					{  return DLLImport.x64.sqlite3_bind_text16(statementHandle, index, value, Encoding.Unicode.GetByteCount(value), DestructorOption.Transient);  }
				else
					{  return DLLImport.x86.sqlite3_bind_text16(statementHandle, index, value, Encoding.Unicode.GetByteCount(value), DestructorOption.Transient);  }

			#elif SQLITE_UTF8

				if (SystemInfo.Is64Bit)
					{  return DLLImport.x64.sqlite3_bind_text(statementHandle, index, value, Encoding.UTF8.GetByteCount(value), DestructorOption.Transient);  }
				else
					{  return DLLImport.x86.sqlite3_bind_text(statementHandle, index, value, Encoding.UTF8.GetByteCount(value), DestructorOption.Transient);  }

			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}

		static public Result BindDouble (IntPtr statementHandle, int index, double value)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_bind_double (statementHandle, index, value);  }
			else
				{  return DLLImport.x86.sqlite3_bind_double (statementHandle, index, value);  }
			}

		static public Result BindNull (IntPtr statementHandle, int index)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_bind_null (statementHandle, index);  }
			else
				{  return DLLImport.x86.sqlite3_bind_null (statementHandle, index);  }
			}

		static public Result Step (IntPtr statementHandle)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_step(statementHandle);  }
			else
				{  return DLLImport.x86.sqlite3_step(statementHandle);  }
			}

		static public int ColumnInt (IntPtr statementHandle, int column)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_column_int (statementHandle, column);  }
			else
				{  return DLLImport.x86.sqlite3_column_int (statementHandle, column);  }
			}

		static public long ColumnInt64 (IntPtr statementHandle, int column)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_column_int64 (statementHandle, column);  }
			else
				{  return DLLImport.x86.sqlite3_column_int64 (statementHandle, column);  }
			}

		static public string ColumnText (IntPtr statementHandle, int column)
			{
			// We can't use the string type as a return value for the API call or else C# will try to deallocate it, which
			// it shouldn't.  It may not crash on .NET but it definitely does in Mono.

			IntPtr nativeResult;

			#if SQLITE_UTF16

				if (SystemInfo.Is64Bit)
					{  nativeResult = DLLImport.x64.sqlite3_column_text16 (statementHandle, column);  }
				else
					{  nativeResult = DLLImport.x86.sqlite3_column_text16 (statementHandle, column);  }

				return Marshal.PtrToStringUni(nativeResult);

			#elif SQLITE_UTF8

				if (SystemInfo.Is64Bit)
					{  nativeResult = DLLImport.x64.sqlite3_column_text (statementHandle, column);  }
				else
					{  nativeResult = DLLImport.x86.sqlite3_column_text (statementHandle, column);  }

				return (string)UTF8Marshaller.GetInstance().MarshalNativeToManaged(nativeResult);

			#else
				throw new Exception("Did not define SQLITE_UTF8 or SQLITE_UTF16");
			#endif
			}

		static public double ColumnDouble (IntPtr statementHandle, int column)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_column_double(statementHandle, column);  }
			else
				{  return DLLImport.x86.sqlite3_column_double(statementHandle, column);  }
			}

		static public Result Reset (IntPtr statementHandle)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_reset(statementHandle);  }
			else
				{  return DLLImport.x86.sqlite3_reset(statementHandle);  }
			}

		static public Result ClearBindings (IntPtr statementHandle)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_clear_bindings(statementHandle);  }
			else
				{  return DLLImport.x86.sqlite3_clear_bindings(statementHandle);  }
			}

		static public Result Finalize (IntPtr statementHandle)
			{
			if (SystemInfo.Is64Bit)
				{  return DLLImport.x64.sqlite3_finalize(statementHandle);  }
			else
				{  return DLLImport.x86.sqlite3_finalize(statementHandle);  }
			}

		static public string LibVersion ()
			{
			IntPtr nativeResult;

			if (SystemInfo.Is64Bit)
				{  nativeResult = DLLImport.x64.sqlite3_libversion();  }
			else
				{  nativeResult = DLLImport.x86.sqlite3_libversion();  }

			return (string)UTF8Marshaller.GetInstance().MarshalNativeToManaged(nativeResult);
			}

		}
	}
