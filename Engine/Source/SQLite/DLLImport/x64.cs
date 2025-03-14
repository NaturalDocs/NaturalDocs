﻿/*
 * Class: CodeClear.NaturalDocs.Engine.SQLite.DLLImport.x64
 * ____________________________________________________________________________
 *
 * The functions exported directly from the x64 SQLite DLL.  Code should use <SQLite.API> instead.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details

#if !SQLITE_UTF8 && !SQLITE_UTF16
	#define SQLITE_UTF8
#endif

using System;
using System.Runtime.InteropServices;


namespace CodeClear.NaturalDocs.Engine.SQLite.DLLImport
	{
	internal static class x64
		{
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_initialize ();

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_shutdown ();

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_open_v2 ([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8Marshaller))] string filename,
																			   out IntPtr connectionHandle, API.OpenOption options, IntPtr vfs);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_close_v2 (IntPtr connectionHandle);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal int sqlite3_limit(IntPtr connectionHandle, API.LimitID id, int newValue);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_extended_result_codes(IntPtr connectionHandle, int onoff);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_busy_timeout(IntPtr connectionHandle, int milliseconds);

		#if SQLITE_UTF16
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_prepare16_v2 (IntPtr connectionHandle,
																					  [MarshalAs(UnmanagedType.LPWStr)] string statementText,
																					  int statementTextByteLength, out IntPtr statementHandle, out IntPtr unusedStatementText);
		#elif SQLITE_UTF8
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_prepare_v2 (IntPtr connectionHandle,
																				   [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8Marshaller))] string statementText,
																				   int statementTextByteLength, out IntPtr statementHandle, out IntPtr unusedStatementText);
		#endif

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_int (IntPtr statementHandle, int index, int value);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_int64 (IntPtr statementHandle, int index, long value);

		#if SQLITE_UTF16
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_text16 (IntPtr statementHandle, int index,
																				    [MarshalAs(UnmanagedType.LPWStr)] string value,
																				    int valueByteLength, DestructorOption destructor);
		#elif SQLITE_UTF8
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_text (IntPtr statementHandle, int index,
																			    [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef=typeof(UTF8Marshaller))] string value,
																			    int valueByteLength, API.DestructorOption destructor);
		#endif

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_double (IntPtr statementHandle, int index, double value);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_bind_null (IntPtr  statementHandle, int index);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_step (IntPtr statementHandle);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal int sqlite3_column_int (IntPtr statementHandle, int column);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal long sqlite3_column_int64 (IntPtr statementHandle, int column);

		#if SQLITE_UTF16
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_column_text16 (IntPtr statementHandle, int column);
		#elif SQLITE_UTF8
		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_column_text (IntPtr statementHandle, int column);
		#endif

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal double sqlite3_column_double (IntPtr statementHandle, int column);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_reset (IntPtr statementHandle);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_clear_bindings (IntPtr statementHandle);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal API.Result sqlite3_finalize (IntPtr statementHandle);

		[DllImport ("SQLite.Win.x64.dll", CallingConvention = CallingConvention.Cdecl)]
		extern static internal IntPtr sqlite3_libversion ();

		}
	}
