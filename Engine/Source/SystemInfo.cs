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
					return Type.GetType("Mono.Runtime")?
								.GetMethod("GetDisplayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
								.Invoke(null, null)?.ToString();
					}
				catch
					{  }

				return null;
				}
			}


		/* Property: dotNETVersion
		 * The version of .NET we're running on, or null if it can't be determined.  This will probably return a value for Mono so check
		 * <OnWindows> if you only want it for actual .NET.
		 */
		static public string dotNETVersion
			{
			get
				{
				try
					{
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


			
		// Group: Static Variables
		// __________________________________________________________________________

		static private bool ignoreCaseInPaths;
		static private char pathSeparator;

		}
	}