/*
 * Class: CodeClear.NaturalDocs.Engine.SystemInfo
 * ____________________________________________________________________________
 *
 * A static class to gather information about the operating system we're running on.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Runtime.Versioning;

namespace CodeClear.NaturalDocs.Engine
	{
	public static class SystemInfo
		{

		// Group: Native Functions
		// __________________________________________________________________________


		#if MAC || LINUX
		/* Function: sysctlbyname
		 * Used to get OS properties on macOS and Linux.  The library will only be loaded on the first usage attempt,
		 * so it's safe to have this in Windows as long as it's not called.
		 */
		[DllImport ("libc")]
		static private extern int sysctlbyname ( [MarshalAs(UnmanagedType.LPStr)] string property, byte[] valueBuffer, ref Int64 valueBufferLength, IntPtr newValueBuffer, uint newValueBufferLength);


		/* Function: SysCtlByName
		 * A version of <sysctlbyname> that encapsulates all the native conversions.  It will return null if it can't retrieve
		 * a value or you're not on macOS or Linux.
		 */
		static private string SysCtlByName (string property)
			{
		    try
				{
				byte[] valueBuffer;
				Int64 valueBufferLength = 0;

				// First pass with valueBuffer as null just retrieves the value length
				if (sysctlbyname(property, null, ref valueBufferLength, IntPtr.Zero, 0) == 0)
					{
					valueBuffer = new byte[valueBufferLength];

					// Second pass gets the actual value
					if (sysctlbyname(property, valueBuffer, ref valueBufferLength, IntPtr.Zero, 0) == 0)
						{
						return Encoding.UTF8.GetString(valueBuffer);
						}
					}
				}
			catch
				{  	}

			return null;
			}
		#endif



		// Group: Properties
		// __________________________________________________________________________


		/* Property: PathSeparatorCharacter
		 * The path separator character for the current platform, such as slash or backslash.
		 */
		static public char PathSeparatorCharacter
			{
			get
				{
				#if WINDOWS
					return '\\';
				#elif MAC || LINUX
					return '/';
				#else
					throw new Exception("Unsupported platform");
				#endif
				}
			}


		/* Property: IgnoreCaseInPaths
		 * Whether paths are case sensitive on the current platform.
		 */
		static public bool IgnoreCaseInPaths
			{
			get
				{
				#if WINDOWS
					return true;
				#elif MAC || LINUX
					return false;
				#else
					throw new Exception("Unsupported platform");
				#endif
				}
			}


		/* Property: dotNETVersion
		 * The version of .NET we're running on, or null if it can't be determined.  This will probably return a value for Mono so check
		 * <MonoVersion> first if you only want it for actual .NET.
		 */
		static public string dotNETVersion
			{
			get
				{
				try
					{  return Environment.Version.ToString();  }
				catch
					{  return null;  }
				}
			}


		/* Property: OSNameAndVersion
		 * Returns the full OS name and version, such as "Windows 10 Home version 1909".  Works for both Windows and Unix.
		 */
		static public string OSNameAndVersion
			{
			get
				{
				#pragma warning disable CA1416
					#if WINDOWS
						return WindowsNameAndVersion;
					#elif MAC || LINUX
						return UnixNameAndVersion;
					#else
						throw new Exception("Unsupported platform");
					#endif
				#pragma warning restore CA1416
				}
			}


		/* Property: SQLiteVersion
		 * Returns the version of SQLite we're using, or null if it can't be determined.
		 */
		static public string SQLiteVersion
			{
			get
				{
				try
					{  return Engine.SQLite.API.LibVersion();  }
				catch
					{  return null;  }
				}
			}



		// Group: Native Properties
		// __________________________________________________________________________


		#if WINDOWS
		/* Property: WindowsNameAndVersion
		 * Returns the full Windows name and version, such as "Windows 10 Home 1909" or "Windows 7 Professional with Service Pack 1".
		 */
		[SupportedOSPlatform("Windows")]
		static public string WindowsNameAndVersion
			{
			get
				{
				string result = null;

				// First try getting the information from the registry, since that's a lot nicer.  We can build a string like "Windows 10 Home 1909"
				// from it.
				try
					{
					var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion");

					if (key != null)
						{
						string productName = key.GetValue("ProductName")?.ToString();
						if (productName != null)
							{
							result = productName;

							// Get the build number, as it's the only way to distinguish between Windows 10 and 11.  ProductName will actually still
							// say "Windows 10" on 11.
							string buildNumberString = key.GetValue("CurrentBuildNumber")?.ToString();
							int buildNumber = 0;

							if (!String.IsNullOrEmpty(buildNumberString) &&
								int.TryParse(buildNumberString,out buildNumber))
								{
								// Windows 11 will have build numbers starting at 22000 and Windows 10's should always be below it.
								if (buildNumber >= 22000)
									{
									result = result.Replace("Windows 10", "Windows 11");
									}
								}

							// Now get the extended version.  First try the newer registry key that will say things like "20H2".  It doesn't exist on
							// older Windows versions.
							string extendedVersion = key.GetValue("DisplayVersion")?.ToString();

							// Next try the older registry key that will say things like "1909".  It still exists on newer versions but will say "2009"
							// instead of "20H2".
							if (extendedVersion == null)
								{  extendedVersion = key.GetValue("ReleaseId")?.ToString();  }

							if (extendedVersion != null)
								{  result += " " + extendedVersion;  }

							string servicePack = key.GetValue("CSDVersion")?.ToString();
							if (servicePack != null)
								{  result += " with " + servicePack;  }
							}
						}
					}
				catch
					{  }

				// If that doesn't work fall back to this version, which isn't as nice because it tends to be like "Microsoft Windows NT 6.2.9200.0"
				// which isn't clear.
				if (result == null)
					{  result = Environment.OSVersion.VersionString;  }

				// Some things return "Microsoft Windows", some things just "Windows", so let's be consistent
				if (result.StartsWith("Microsoft "))
					{  result = result.Substring(10);  }

				return result;
				}
			}
		#endif


		#if MAC || LINUX
		/* Property: UnixNameAndVersion
		 * Returns the Unix name and version to the degree that it can be determined.
		 */
		[SupportedOSPlatform("macOS")]
		[SupportedOSPlatform("Linux")]
		static public string UnixNameAndVersion
			{
			get
				{
				string result = null;

		        try
					{
					// Are we on a Mac?
					if (SysCtlByName("kern.ostype").Contains("Darwin"))
						{
						string osVersion = SysCtlByName("kern.osproductversion");  // may be null
						string compatOSVersion = SysCtlByName("kern.osproductversioncompat");  // if osVersion is the same, we may be getting a fake compatibility value
						string darwinVersion = SysCtlByName("kern.osrelease");
						string cpu = SysCtlByName("machdep.cpu.brand_string");

						if (cpu.Contains("Intel"))
							{  cpu = "Intel " + (Is64Bit ? "x64" : "x86");  }
						// Apple silicon already returns a clean value so we can use it unedited

						result = "macOS";

						if (osVersion != null && (compatOSVersion == null || osVersion != compatOSVersion))
							{
							result += " " + osVersion;

							if (cpu != null)
								{  result += " (" + cpu + ")";  }
							}
						else // no osVersion
							{
							result += " (Darwin " + darwinVersion;

							if (cpu != null)
								{  result += ", " + cpu;  }

							result += ")";
							}
						}
					}
				catch
					{  	result = null; }

				// Fallback value
				return result ?? Environment.OSVersion.VersionString;
				}
			}
		#endif

		}
	}
