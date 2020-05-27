/* 
 * Class: CodeClear.NaturalDocs.Engine.SystemInfo
 * ____________________________________________________________________________
 * 
 * A static class to gather information about the operating system we're running on.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


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
							monoVersion.StartsWith("2.") );
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
					{
					// First try getting the information from the registry, since that will let us make it a lot nicer.  Values are documented here:
					// https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
					if (OnWindows)
						{
						// Check registry for .NET 4.5 and later
						var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full");

						if (key != null)
							{
							string versionString = key.GetValue("Release")?.ToString();
							int versionInt;

							if (versionString != null && Int32.TryParse(versionString, out versionInt))
								{
								if (versionInt == 528040 || versionInt == 528209 || versionInt == 528049)  // known versions of 4.8
									{  return "4.8";  }
								else if (versionInt >= 528040)  // cover future versions
									{  return "4.8 or later (" + versionInt + ")";  }
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

							string win10version = key.GetValue("ReleaseId")?.ToString();
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

				return Environment.OSVersion.VersionString;
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