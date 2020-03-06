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


			
		// Group: Static Variables
		// __________________________________________________________________________

		static private bool ignoreCaseInPaths;
		static private char pathSeparator;

		}
	}