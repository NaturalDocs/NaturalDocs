/*
 * Class: CodeClear.NaturalDocs.Engine.SQLite.NativeLibrary
 * ____________________________________________________________________________
 *
 * The functions exported directly from the SQLite native library.  Code should use <SQLite.API> instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

#if !SQLITE_UTF8 && !SQLITE_UTF16
	#define SQLITE_UTF8
#endif

using System;
using System.Runtime.InteropServices;


namespace CodeClear.NaturalDocs.Engine.SQLite
	{
	internal static class NativeLibrary
		{

		#if WINDOWS && X64
			internal const string NativeLibraryPath = "SQLite.Win.x64.dll";
		#elif WINDOWS && ARM64
			internal const string NativeLibraryPath = "SQLite.Win.ARM64.dll";
		#elif MAC && X64
			internal const string NativeLibraryPath = "libSQLite.Mac.x64.dylib";
		#elif MAC && ARM64
			internal const string NativeLibraryPath = "libSQLite.Mac.ARM64.dylib";
		#elif LINUX && X64
			internal const string NativeLibraryPath = "libSQLite.Linux.x64.so";
		#else
			#error No SQLite native library for this platform or platform constants aren't defined.
		#endif

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_initialize ();

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_shutdown ();

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_open_v2 ([MarshalAs(UnmanagedType.LPUTF8Str)] string filename,
																			   out IntPtr connectionHandle, API.OpenOption options, IntPtr vfs);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_close_v2 (IntPtr connectionHandle);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal int sqlite3_limit(IntPtr connectionHandle, API.LimitID id, int newValue);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_extended_result_codes(IntPtr connectionHandle, int onoff);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_busy_timeout(IntPtr connectionHandle, int milliseconds);

		#if SQLITE_UTF16
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_prepare16_v2 (IntPtr connectionHandle,
																					  [MarshalAs(UnmanagedType.LPWStr)] string statementText,
																					  int statementTextByteLength, out IntPtr statementHandle, out IntPtr unusedStatementText);
		#elif SQLITE_UTF8
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_prepare_v2 (IntPtr connectionHandle,
																				   [MarshalAs(UnmanagedType.LPUTF8Str)] string statementText,
																				   int statementTextByteLength, out IntPtr statementHandle, out IntPtr unusedStatementText);
		#endif

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_int (IntPtr statementHandle, int index, int value);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_int64 (IntPtr statementHandle, int index, long value);

		#if SQLITE_UTF16
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_text16 (IntPtr statementHandle, int index,
																				    [MarshalAs(UnmanagedType.LPWStr)] string value,
																				    int valueByteLength, IntPtr destructor);
		#elif SQLITE_UTF8
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_text (IntPtr statementHandle, int index,
																				[MarshalAs(UnmanagedType.LPUTF8Str)] string value,
																				int valueByteLength, IntPtr destructor);
		#endif

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_double (IntPtr statementHandle, int index, double value);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_null (IntPtr  statementHandle, int index);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_step (IntPtr statementHandle);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal int sqlite3_column_int (IntPtr statementHandle, int column);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal long sqlite3_column_int64 (IntPtr statementHandle, int column);

		#if SQLITE_UTF16
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_column_text16 (IntPtr statementHandle, int column);
		#elif SQLITE_UTF8
		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_column_text (IntPtr statementHandle, int column);
		#endif

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal double sqlite3_column_double (IntPtr statementHandle, int column);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_reset (IntPtr statementHandle);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_clear_bindings (IntPtr statementHandle);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_finalize (IntPtr statementHandle);

		[DllImport (NativeLibraryPath, CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_libversion ();

		}
	}
