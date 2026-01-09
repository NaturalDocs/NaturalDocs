/*
 * Class: CodeClear.NaturalDocs.Engine.SystemInfo
 * ____________________________________________________________________________
 *
 * A static class to gather information about the operating system we're running on.  Since it's important to collect this data in
 * the event of a crash or Natural Docs not functioning on a system, none of these properties will throw an exception.  They will
 * all either return a value or null.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace CodeClear.NaturalDocs.Engine
	{
	public static class SystemInfo
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildDiagnosticSummary
		 * Builds a multi-line summary of Natural Docs and system properties to include in crash reports and similar things.  It will
		 * be multiple lines and end with a line break.  You can optionally include a string to appear at the beginning of each line if
		 * you need it to be indented.
		 */
		static public string BuildDiagnosticSummary (string linePrefix = null)
			{
			StringBuilder summary = new StringBuilder();

			if (linePrefix == null)
				{  linePrefix = "";  }


			// Natural Docs version

			summary.AppendLine(linePrefix + "Natural Docs " + SystemInfo.NaturalDocsVersion.ToString());


			// Natural Docs OS and architecture build

			summary.Append(linePrefix + " - ");

			summary.AppendLine(
				Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NaturalDocsBuild(os, architecture)", "{0} {1} build",
									  SystemInfo.NaturalDocsOSBuild,
									  SystemInfo.NaturalDocsProcessorArchitectureBuild));


			// .NET version

			string dotNetVersion = SystemInfo.dotNETVersion;
			summary.Append(linePrefix + " - ");

			if (dotNetVersion != null)
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.dotNETVersion(version)", ".NET {0}", dotNetVersion));
				}
			else
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NoDotNETVersion", "Couldn't get .NET version"));
				}


			// SQLite version

			string sqliteVersion = SystemInfo.SQLiteVersion;
			summary.Append(linePrefix + " - ");

			if (sqliteVersion != null)
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.SQLiteVersion(version)", "SQLite {0}", sqliteVersion));
				}
			else
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NoSQLiteVersion", "Couldn't get SQLite version"));
				}


			summary.AppendLine();


			// Operating system version

			string osVersion = SystemInfo.OSNameAndVersion;
			summary.Append(linePrefix);

			if (osVersion != null)
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.OSNameAndVersion(value)", "{0}", osVersion));
				}
			else
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NoOSNameAndVersion", "Couldn't get OS name and version"));
				}


			#if LINUX
				#pragma warning disable CA1416

				// glibc version

				string glibcVersion = SystemInfo.LinuxGLibCVersion;
				summary.Append(linePrefix + " - ");

				if (glibcVersion != null)
					{
					summary.AppendLine(
						Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.glibcVersion(version)", "glibc {0}", glibcVersion));
					}
				else
					{
					summary.AppendLine(
						Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NoGLibCVersion", "Couldn't get glibc version"));
					}

				#pragma warning restore CA1416
			#endif


			// Processor name

			string processor = SystemInfo.ProcessorName;
			summary.Append(linePrefix + " - ");

			if (processor != null)
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.Processor(name)", "{0}", processor));
				}
			else
				{
				summary.AppendLine(
					Locale.SafeGet("NaturalDocs.Engine", "Diagnostics.NoProcessor", "Couldn't get processor name"));
				}

			return summary.ToString();
			}



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
					#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
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
					#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
				#endif
				}
			}


		/* Property: NaturalDocsVersion
		 * The current version of Natural Docs.
		 */
		static public Version NaturalDocsVersion
			{
			get
				{  return Engine.Instance.Version;  }
			}


		/* Property: NaturalDocsOSBuild
		 * The operating system this version of Natural Docs was built for.
		 */
		static public string NaturalDocsOSBuild
			{
			get
				{
				#if WINDOWS
					return "Windows";
				#elif MAC
					return "macOS";
				#elif LINUX
					return "Linux";
				#else
					#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
				#endif
				}
			}


		/* Property: NaturalDocsProcessorArchitectureBuild
		 * The processor architecture this version of Natural Docs was built for, such as "x64" or "ARM64".
		 */
		static public string NaturalDocsProcessorArchitectureBuild
			{
			get
				{
				#if X64
					return "x64";
				#elif ARM64
					return "ARM64";
				#else
					#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
				#endif
				}
			}


		/* Property: dotNETVersion
		 * The version of .NET we're running on, or null if it can't be determined.
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
		 * Returns the full OS name and version, such as "Windows 10 Home version 1909", to the extent that it can be determined,
		 * or null if it cannot.
		 */
		static public string OSNameAndVersion
			{
			get
				{
				#pragma warning disable CA1416
					#if WINDOWS
						return WindowsNameAndVersion;
					#elif MAC
						return macOSNameAndVersion;
					#elif LINUX
						return LinuxNameAndVersion;
					#else
						#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
					#endif
				#pragma warning restore CA1416
				}
			}


		/* Property: ProcessorName
		 * Returns the processor name, such as "Apple M1", to the extent that it can be determined, or null if it cannot.
		 */
		static public string ProcessorName
			{
			get
				{
				#pragma warning disable CA1416
					#if WINDOWS
						string processorName = WindowsProcessorName;
					#elif MAC
						string processorName = macOSProcessorName;
					#elif LINUX
						string processorName = LinuxProcessorName;
					#else
						#error SystemInfo needs to be updated for this platform or platform constants aren't defined.
					#endif
				#pragma warning restore CA1416

				if (processorName != null)
					{
					// Clean up the value to be nicer
					processorName = processorName.Replace("(r)", "", StringComparison.InvariantCultureIgnoreCase);
					processorName = processorName.Replace("(tm)", "", StringComparison.InvariantCultureIgnoreCase);
					processorName = processorName.CondenseWhitespace();

					// Strip the end off of "Intel Core i7-6700 CPU @ 3.40GHz"
					int cutPoint = processorName.IndexOf(" CPU @");

					if (cutPoint != -1)
						{  processorName = processorName.Substring(0, cutPoint);  }

					// Strip the end off of "AMD Ryzen 9 9950X 16-Core Processor"
					processorName = System.Text.RegularExpressions.Regex.Replace(processorName, " [0-9]+-Core Processor.*$", "");
					}

				return processorName;
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



		// Group: Windows Properties
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


		/* Property: WindowsProcessorName
		 * Returns the processor name to the extent that it can be determined, or null if it cannot.
		 */
		[SupportedOSPlatform("Windows")]
		static public string WindowsProcessorName
			{
			get
				{
				string result = null;

				try
					{
					var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");

					if (key != null)
						{
						result = key.GetValue("ProcessorNameString")?.ToString();
						}
					}
				catch
					{  }

				return result;
				}
			}

		#endif



		// Group: macOS Properties and Functions
		// __________________________________________________________________________

		#if MAC

		/* Function: sysctlbyname
		 * Used to get OS properties on macOS.
		 */
		[DllImport ("libc")]
		static private extern int sysctlbyname ( [MarshalAs(UnmanagedType.LPStr)] string property, byte[] valueBuffer, ref Int64 valueBufferLength, IntPtr newValueBuffer, uint newValueBufferLength);


		/* Function: SysCtlByName
		 * A version of <sysctlbyname> that encapsulates all the native conversions.  It will return null if it can't retrieve a value.
		 */
		[SupportedOSPlatform("macOS")]
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


		/* Property: macOSNameAndVersion
		 * Returns the macOS name and version to the extent that it can be determined.
		 */
		[SupportedOSPlatform("macOS")]
		static public string macOSNameAndVersion
			{
			get
				{
				string result = null;

		        try
					{
					string osVersion = SysCtlByName("kern.osproductversion");  // may be null
					string compatOSVersion = SysCtlByName("kern.osproductversioncompat");  // if osVersion is the same, we may be getting a fake compatibility value
					string darwinVersion = SysCtlByName("kern.osrelease");

					result = "macOS";

					if (osVersion != null && (compatOSVersion == null || osVersion != compatOSVersion))
						{
						result += " " + osVersion;

						// There's no way to get the marketing name from an API, but we can add the known ones ourself
						if (osVersion.StartsWith("10.15."))
							{  result += " Catalina";  }
						else if (osVersion.StartsWith("11."))
							{  result += " Big Sur";  }
						else if (osVersion.StartsWith("12."))
							{  result += " Monterey";  }
						else if (osVersion.StartsWith("13."))
							{  result += " Ventura";  }
						else if (osVersion.StartsWith("14."))
							{  result += " Sonoma";  }
						else if (osVersion.StartsWith("15."))
							{  result += " Sequoia";  }
						else if (osVersion.StartsWith("26."))
							{  result += " Tahoe";  }
						}
					else // no osVersion
						{
						result += " (Darwin " + darwinVersion + ")";
						}
					}
				catch
					{  	result = null; }

				// Fallback value
				return result ?? Environment.OSVersion.VersionString;
				}
			}


		/* Property: macOSProcessorName
		 * Returns the processor name to the extent that it can be determined.
		 */
		[SupportedOSPlatform("macOS")]
		static public string macOSProcessorName
			{
			get
				{
				string result = null;

		        try
					{
					result = SysCtlByName("machdep.cpu.brand_string");
					}
				catch
					{  	}

				return result;
				}
			}

		#endif



		// Group: Linux Properties and Functions
		// __________________________________________________________________________

		#if LINUX

		/* Function: gnu_get_libc_version
		 * Used to get the glibc version on Linux.
		 */
		[DllImport ("c")]
		static private extern IntPtr gnu_get_libc_version();


		/* Function: GNUGetLibCVersion
		 * A version of <gnu_get_libc_version> that encapsulates all the native conversions.  It will return null if it can't retrieve a value.
		 */
		[SupportedOSPlatform("Linux")]
		static private string GNUGetLibCVersion ()
			{
			return Marshal.PtrToStringAnsi(gnu_get_libc_version());
			}


		/* Property: LinuxNameAndVersion
		 * Returns the Linux name and version to the extent that it can be determined.
		 */
		[SupportedOSPlatform("Linux")]
		static public string LinuxNameAndVersion
			{
			get
				{
				string result = null;

				try
					{
					var lines = System.IO.File.ReadAllLines("/etc/os-release");

					foreach (var line in lines)
						{
						if (line.StartsWith("PRETTY_NAME=\""))
							{
							result = line.Substring(13, line.Length - 14);
							break;
							}
						}
					}
				catch
					{  }

				return result;
				}
			}


		/* Property: LinuxGLibCVersion
		 * Returns the Linux glibc version to the extent that it can be determined.
		 */
		[SupportedOSPlatform("Linux")]
		static public string LinuxGLibCVersion
			{
			get
				{
				try
					{  return GNUGetLibCVersion();  }
				catch
					{  return null;  }
				}
			}


		/* Property: LinuxProcessorName
		 * Returns the processor name to the extent that it can be determined.
		 */
		[SupportedOSPlatform("Linux")]
		static public string LinuxProcessorName
			{
			get
				{
				string result = null;

				try
					{
					var lines = System.IO.File.ReadAllLines("/proc/cpuinfo");

					foreach (var line in lines)
						{
						if (line.StartsWith("model name"))
							{
							int cutPoint = line.IndexOf(':');
							result = line.Substring(cutPoint + 1);
							result = result.Trim();
							break;
							}
						}
					}
				catch
					{  }

				return result;
				}
			}

		#endif

		}
	}
