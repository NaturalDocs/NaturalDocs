/*
 * Class: CodeClear.NaturalDocs.Engine.SystemInfo
 * ____________________________________________________________________________
 *
 * A static class to gather information about the operating system we're running on.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Runtime.InteropServices;
using System.Text;

namespace CodeClear.NaturalDocs.Engine
	{
	public static class SystemInfo
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: SystemInfo
		 */
		static SystemInfo ()
			{
			// These two values are used so often that they're calculated here and cached instead of calculated on each use.
			ignoreCaseInPaths = OnWindows;
			pathSeparator = (OnWindows ? '\\' : '/');
			}



		// Group: Native Functions
		// __________________________________________________________________________


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
			if (!OnUnix)
				{  return null;  }

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



		// Group: Properties
		// __________________________________________________________________________


		/* Property: OnWindows
		 * Whether the program is running in Windows.
		 */
		static public bool OnWindows
			{
			get
				{
				// Other Windows-like values are no longer used so we don't have to check them.
				return (System.Environment.OSVersion.Platform == PlatformID.Win32NT);
				}
			}


		/* Property: OnUnix
		 * Whether the program is running in Unix.
		 */
		static public bool OnUnix
			{
			get
				{
				// Early versions of Mono returned 128 as the value, whereas PlatformID.Unix is 4.
				// There's now also OS X which is 6, but .NET Core returns Unix so we don't try to distinguish.
				return ( System.Environment.OSVersion.Platform == PlatformID.Unix ||
							System.Environment.OSVersion.Platform == PlatformID.MacOSX ||
							(int)System.Environment.OSVersion.Platform == 128 );
				}
			}


		/* Property: Is64Bit
		 * Whether the program is running in 64-bit mode.  This is not the same as whether the processor or the operating system
		 * is 64-bit, it is whether .NET or Mono is executing the program as 64-bit.
		 */
		static public bool Is64Bit
			{
			get
				{
				// There's also Environment.Is64BitProcess but that wasn't introduced until .NET 4.
				return (IntPtr.Size == 8);
				}
			}


		/* Property: PathSeparatorCharacter
		 * The path separator character for the current platform, such as slash or backslash.
		 */
		static public char PathSeparatorCharacter
			{
			get
				{  return pathSeparator;  }
			}


		/* Property: IgnoreCaseInPaths
		 * Whether paths are case sensitive on the current platform.
		 */
		static public bool IgnoreCaseInPaths
			{
			get
				{  return ignoreCaseInPaths;  }
			}


		/* Property: MonoVersion
		 * The version of Mono we're running on, or null if we're not or it can't be determined.
		 */
		static public string MonoVersion
			{
			get
				{
				try
					{
					string monoString =
						Type.GetType("Mono.Runtime")?
						.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
						.Invoke(null, null)?.ToString();

					if (monoString != null)
						{
						// Can return something like "Mono 5.2.0.215 (tarball Mon Aug 14 15:57:49 UTC 2017)".  Strip off the parentheses if present.
						int parenIndex = monoString.IndexOf('(');

						if (parenIndex > 0)
							{  monoString = monoString.Substring(0, parenIndex).TrimEnd();  }
						}

					return monoString;
					}
				catch
					{  }

				return null;
				}
			}


		/* Property: MonoVersionTooOld
		 * Returns whether the version of Mono we're running on is too told and known to cause problems.  Will return false when not running on Mono.
		 */
		static public bool MonoVersionTooOld
			{
			get
				{
				string monoVersion = MonoVersion;

				if (monoVersion == null)
					{  return false;  }

				return ( monoVersion.StartsWith("0.") ||
							monoVersion.StartsWith("1.") ||
							monoVersion.StartsWith("2.") ||
							monoVersion.StartsWith("3.") );
				}
			}


		/* Property: MinimumMonoVersion
		 * Returns the minimum version of Mono required by Natural Docs.
		 */
		static public string MinimumMonoVersion
			{
			get
				{  return "4.0";  }
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
					{
					// First try getting the information from the registry, since that will let us make it a lot nicer.
					if (OnWindows)
						{
						// Check registry for .NET 4.5 and later
						var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full");

						if (key != null)
							{
							string versionString = key.GetValue("Release")?.ToString();
							int versionInt;

							// Values are documented here:
							// https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
							if (versionString != null && Int32.TryParse(versionString, out versionInt))
								{
								if (versionInt > 533325)  // cover future versions
									{  return "4.8.1 or later (" + versionInt + ")";  }
								else if (versionInt >= 533320)
									{  return "4.8.1";  }
								else if (versionInt == 528040)
									{  return "4.8";  }
								else if (versionInt >= 461808)
									{  return "4.7.2";  }
								else if (versionInt >= 461308)
									{  return "4.7.1";  }
								else if (versionInt >= 460798)
									{  return "4.7";  }
								else if (versionInt >= 394802)
									{  return "4.6.2";  }
								else if (versionInt >= 394254)
									{  return "4.6.1";  }
								else if (versionInt >= 393295)
									{  return "4.6";  }
								else if (versionInt >= 379893)
									{  return "4.5.2";  }
								else if (versionInt >= 378675)
									{  return "4.5.1";  }
								else if (versionInt >= 378389)
									{  return "4.5";  }
								}
							}

						// If that didn't work, check for earlier versions of .NET
						string[][] netVersions = new string[][]
							{
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4.0\\Full", "4.0", null },
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4.0\\Client", "4.0", "client profile" },
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v3.5", "3.5", null },
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v3.0", "3.0", null },
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v3.0\\Setup", "3.0", null },
							new string[] { "SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v2.0.50727", "2.0", null }
							};

						foreach (var netVersion in netVersions)
							{
							key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(netVersion[0]);
							if (key != null && (key.GetValue("Install")?.ToString() == "1" || key.GetValue("InstallSuccess")?.ToString() == "1"))
								{
								string spString = key.GetValue("SP")?.ToString();
								return netVersion[1] + (spString != null ? " SP" + spString : "") + (netVersion[2] != null ? " " + netVersion[2] : "");
								}
							}
						}

					// Fallback
					return System.Diagnostics.FileVersionInfo.GetVersionInfo(typeof(int).Assembly.Location).ProductVersion;
					}
				catch
					{  }

				return null;
				}
			}


		/* Property: OSNameAndVersion
		 * Returns the full OS name and version, such as "Windows 10 Home version 1909".  Works for both Windows and Unix.
		 */
		static public string OSNameAndVersion
			{
			get
				{
				if (OnWindows)
					{  return WindowsNameAndVersion;  }
				else if (OnUnix)
					{  return UnixNameAndVersion;  }
				else
					{  return null;  }
				}
			}


		/* Property: WindowsNameAndVersion
		 * Returns the full Windows name and version, such as "Windows 10 Home version 1909" or "Windows 7 Professional with Service Pack 1".
		 */
		static public string WindowsNameAndVersion
			{
			get
				{
				if (!OnWindows)
					{  return null;  }

				string result = null;

				// First try getting the information from the registry, since that's a lot nicer.  We can build a string like "Windows 10 Home version 1909"
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

							// Newer key that will say things like "20H2".  Doesn't exist on older versions.
							string win10version = key.GetValue("DisplayVersion")?.ToString();

							// Older key that will say things like "1909".  Still exists on newer versions but will say "2009" instead of "20H2".
							if (win10version == null)
								{  win10version = key.GetValue("ReleaseId")?.ToString();  }

							if (win10version != null)
								{  result += " version " + win10version;  }

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


		/* Property: UnixNameAndVersion
		 * Returns the Unix name and version to the degree that it can be determined.
		 */
		static public string UnixNameAndVersion
			{
			get
				{
				if (!OnUnix)
					{  return null;  }

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



		// Group: Static Variables
		// __________________________________________________________________________

		static private bool ignoreCaseInPaths;
		static private char pathSeparator;

		}
	}
