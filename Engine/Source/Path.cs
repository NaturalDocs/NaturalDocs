/* 
 * Struct: CodeClear.NaturalDocs.Engine.Path
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a file path string, which can be relative or absolute.  This allows Natural Docs to 
 * handle file paths in a platform-independent way.  It also enables it to handle paths originating on any 
 * platform regardless of the platform it's currently running on, so Natural Docs on Windows can read a 
 * configuration file from Natural Docs on Linux and vice versa.  
 * 
 * Also, using Paths in place of strings enforces normalization.  Any time a Path is set to a raw string it is
 * automatically normalized.  Because of this, you can actually be sloppy when joining strings.  For example:
 *		
 *	> Path path = currentFolder + "/" + fileName;
 *		
 *	You don't have to check whether currentFolder has a trailing slash or not because the duplicate would be
 *	removed.  You don't have to use the native separator character because it would be converted if that's not
 *	it.  You do, however, have to check whether currentFolder is empty in the above case because a Linux path
 *	starting with a slash becomes absolute.  However, if currentFolder was already a Path that's not a worry
 *	because it would be "." in that case.
 *	
 *	If you know a particular path will be relative or absolute, or want to require it to be, you can use
 *	<RelativePath> and <AbsolutePath> instead.  These make it clear to the code using them what type of
 *	paths they are, and they enforce those requirements.  You cannot accidentally set one type to the other, 
 *	and converting a Path or a string of the opposite type to them will throw an exception.
 * 
 *	Supported:
 *	
 *		- Windows paths.  C:\My Folder\My File.txt
 *		- Windows UNC paths.  \\ServerName\Share Name\My File.txt
 *		- Linux paths.  /My Folder/My File.txt
 *		- Linux tilde paths.  ~/My Folder/My File.txt.  Converted to absolute paths by normalization.
 *		- Classic Mac paths.  My Folder:My File.txt.  Converted to Linux paths by normalization.
 *		
 * Unsupported:
 * 
 *		- Environment variables.  %userprofile%\My Folder\My File.txt
 *		- File URLs.  file:///C|/My Folder/My File.txt
 *		- These characters as part of file or folder names, regardless of the native operating system.  / \ " :
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine
	{
	public struct Path : IComparable
		{
		
		// Group: Constructors
		// __________________________________________________________________________
		
			
		/* Constructor: Path (string)
		 * Creates a new Path from the passed string.
		 */
		public Path (string pathString)
			{
			this.pathString = pathString;
			Normalize();
			}



		// Group: Properties
		// __________________________________________________________________________
		
			
		/* Property: Length
		 * The length of the path string.
		 */
		public int Length
			{
			get
				{  return pathString.Length;  }
			}
			
		/* Property: IsAbsolute
		 * Whether the path is absolute.
		 */
		public bool IsAbsolute
			{
			get
				{
				// We do this by hand instead of using the regex for efficiency, especially since this may be called a lot more
				// with conversions to AbsolutePath and RelativePath.

				if (pathString == null || pathString.Length == 0)
					{  return false;  }

				char first = pathString[0];

				if (first == '/')
					{  return true;  }

				if (pathString.Length >= 2)
					{
					char second = pathString[1];

					if (second == ':' &&
						( (first >= 'a' && first <= 'z') ||
						  (first >= 'A' && first <= 'Z') )
						)
						{  return true;  }

					if (first == '\\' && second == '\\')
						{  return true;  }
					}

				return false;
				}
			}
			
		/* Property: IsRelative
		 * Whether the path is relative.
		 */
		public bool IsRelative
			{
			get
				{  return (IsAbsolute == false);  }
			}
		
		
		/* Property: Prefix
		 * The prefix of the path if it's absolute, or null if it's relative.  The prefix can be "C:", "\\server", or "/".
		 */
		public string Prefix
			{
			get
				{
				Match match = pathPrefixRegex.Match(pathString);
				
				if (match.Success)
					{  return match.Groups[0].ToString();  }
				else
					{  return null;  }
				}
			}

			
		/* Property: Extension
		 * 
		 * The file extension of the path, or null if there is none.  
		 * 
		 * Files that start with a dot are not considered to have extensions unless there is another dot in their name, so ".file" 
		 * has no extension but ".file.txt" has a "txt" extension.
		 */
		public string Extension
			{
			get
				{
				int dotIndex = pathString.LastIndexOf('.');
				
				// "filename" or "filename."
				if (dotIndex == -1 || dotIndex == pathString.Length - 1)
					{  return null;  }
					
				int separatorIndex = pathString.LastIndexOfAny(normalizedSeparators);
				
				if (separatorIndex == -1)
					{
					// "." or ".filename"
					if (dotIndex == 0)
						{  return null;  }
					else
						{  return pathString.Substring(dotIndex + 1);  }
					}
				else // separatorIndex != -1
					{
					// "folder.ext/filename" or "folder/.filename"
					if (dotIndex < separatorIndex || dotIndex == separatorIndex + 1)
						{  return null;  }
					else
						{  return pathString.Substring(dotIndex + 1);  }
					}				
				}
			}
			
			
		/* Property: ParentFolder
		 * The parent folder of the path.  If the path is to a file, it will be a path to its containing folder.  If the path is to a folder, it
		 * will be the folder above it.  If the path is relative it will start using ".." once the visible path is exhausted.  If the path is
		 * absolute it will stop at the volume, so the parent folder of C: is C:.
		 */
		public Path ParentFolder
			{
			get
				{
				// I was hoping to build a smart function here that could take a substring and not go through another normalization, 
				// but the code get's hairier than you'd think so we'll just take the easy way out and let normalization fix it.
				return pathString + "/..";
				}
			}
			
			
		/* Property: NameWithoutPath
		 * The file name without its path.
		 */
		public string NameWithoutPath
			{
			get
				{
				int separatorIndex = pathString.LastIndexOfAny(normalizedSeparators);
				
				if (separatorIndex == -1)
					{  return pathString;  }
				else
					{  return pathString.Substring(separatorIndex + 1);  }
				}
			}
			
			
		/* Property: NameWithoutPathOrExtension
		 * 
		 * The file name without its path or extension.
		 * 
		 * Files that start with a dot are not considered to have extensions unless there is another dot in their name, so ".file" 
		 * has no extension but ".file.txt" has a "txt" extension.
		 */
		public string NameWithoutPathOrExtension
			{
			get
				{
				int dotIndex = pathString.LastIndexOf('.');
				int separatorIndex = pathString.LastIndexOfAny(normalizedSeparators);
				
				if (separatorIndex == -1)
					{
					// "filename" or ".filename" or "."
					if (dotIndex <= 0)
						{  return pathString;  }
					else
						{  return pathString.Substring(0, dotIndex);  }
					}
					
				else
					{
					// "folder/filename" or "folder/.filename" or "folder.ext/filename"
					if (dotIndex == -1 || dotIndex == separatorIndex + 1 || dotIndex < separatorIndex)
						{  return pathString.Substring(separatorIndex + 1);  }
					else
						{  return pathString.Substring(separatorIndex + 1, dotIndex - (separatorIndex + 1));  }
					}
				}
			}



		// Group: Functions
		// __________________________________________________________________________
		
			
		/* Function: Contains
		 * Returns whether this path contains the passed one, meaning it's a higher level folder.
		 */
		public bool Contains (Path other)
			{
			// +2 to account for the separator character and at least one other character beyond it.  Normalization would
			// have removed any trailing separator on the base.
			if (other.pathString.Length >= pathString.Length + 2)
				{
				// The first char beyond the base has to be a separator char, as we don't want "C:\Something" to be seen
				// as contained in "C:\Some", only "C:\Some\File".
				char breakChar = other.pathString[ pathString.Length ];
				
				if (breakChar != '/' && breakChar != '\\')
					{  return false;  }
					
				// Since they're both normalized, we can just do a simple compare now.
				return other.pathString.StartsWith(pathString, Engine.SystemInfo.IgnoreCaseInPaths, null);
				}
			else
				{  return false;  }
			}
			
			
		/* Function: MakeRelativeTo
		 * Returns the path as one relative to the passed folder, if possible.  If it's not possible (for example, if they're on
		 * different drive letters) it returns null.
		 */
		public RelativePath MakeRelativeTo (Path folder)
			{
			if (folder.Contains(this))
				{
				// +1 to get rid of the separator character.  Contains() performed all the necessary checks, so we can just
				// do something really simple here.  This should not require the result to be normalized again.
				Path result;
				result.pathString = pathString.Substring( folder.pathString.Length + 1 );
				return (RelativePath)result;
				}
			else
				{
				string thisPrefix, folderPrefix;
				List<string> thisSections, folderSections;
				
				Split(out thisPrefix, out thisSections);
				folder.Split(out folderPrefix, out folderSections);
				
				if (String.Compare(thisPrefix, folderPrefix, Engine.SystemInfo.IgnoreCaseInPaths) != 0)
					{  return null;  }
					
				while (thisSections.Count > 0 && folderSections.Count > 0 &&
						 String.Compare(thisSections[0], folderSections[0], Engine.SystemInfo.IgnoreCaseInPaths) == 0)
					{
					thisSections.RemoveAt(0);
					folderSections.RemoveAt(0);
					}
					
				StringBuilder resultString = new StringBuilder(".");  // In case they're exactly equal.  If not, normalization will remove it.
				
				for (int i = 0; i < folderSections.Count; i++)
					{  resultString.Append("/..");  }
					
				for (int i = 0; i < thisSections.Count; i++)
					{  
					resultString.Append('/');  
					resultString.Append(thisSections[i]);
					}
					
				return new RelativePath( resultString.ToString() );
				}
			}
			
			
		/* Function: Split
		 * Splits the path into a prefix and a list of strings, each representing a segment of it.  If the path was absolute, prefix will be
		 * set to something like "C:", "\\server", or "/".  Otherwise it will be null.  The sections array will have an entry for each folder
		 * name and one for the file name if there was one.  No separator characters will be included.  It will return an empty list if there's
		 * nothing other than the prefix.
		 */
		public void Split (out string prefix, out List<string> sections)
			{
			// Note that this function is used by Normalize() so it has to handle classic Mac paths as well.
			
			prefix = null;
			sections = new List<string>();
			int index = 0;

			
			// Split off the prefix, if available.
			
			Match match = pathPrefixRegex.Match(pathString);
			
			if (match.Success)
				{
				prefix = match.Groups[0].ToString();
				index = match.Groups[0].Length;
				}
				
			// If there's no prefix, slashes, or backslashes but there is a colon that isn't the first character, that means
			// its a classic Mac absolute path.
			else if (pathString.IndexOfAny(normalizedSeparators) == -1 && pathString.IndexOf(':') > 0)
				{
				prefix = "/";
				}
				
				
			// Go through the sections.
			
			while (index < pathString.Length)
				{
				int newIndex = pathString.IndexOfAny(unnormalizedSeparators, index);
				
				// Handle the section
				if (newIndex != index)
					{
					string section;
					
					if (newIndex == -1)
						{  section = pathString.Substring(index);  }
					else
						{  section = pathString.Substring(index, newIndex - index);  }
						
					sections.Add(section);
					}
					
				// Handle multiple consecutive colons, which are the equivalent of .. on classic Mac paths.
				if (newIndex != -1 && pathString[newIndex] == ':')
					{
					while (newIndex + 1 < pathString.Length && pathString[newIndex + 1] == ':')
						{
						sections.Add("..");
						newIndex++;
						}
					}
					
				if (newIndex == -1)
					{  break;  }
				else
					{  index = newIndex + 1;  }
				}
			}
			
			
		/* Function: FromCommandLine
		 * Retrieves a file path from the command line, returning it and advancing the index.  If the first string starts with a
		 * quote it continues until it reaches an end quote.  Otherwise it continues until it reaches a string that starts with a
		 * dash.
		 */
		static public Path FromCommandLine (string[] commandLine, ref int index)
			{
			System.Text.StringBuilder result = new System.Text.StringBuilder(64);
			
			// Quoted paths go to the next quote, regardless of dashes.
			if (index < commandLine.Length && commandLine[index][0] == '"')
				{
				if (commandLine[index].Length > 1)
					{  result.Append(commandLine[index], 1, commandLine[index].Length - 1);  }
				index++;
				
				while (index < commandLine.Length && !commandLine[index].EndsWith("\""))
					{
					if (result.Length != 0)
						{  result.Append(' ');  }
						
					result.Append(commandLine[index]);
					index++;
					}
					
				if (index < commandLine.Length && commandLine[index].Length > 1)
					{
					if (result.Length != 0)
						{  result.Append(' ');  }
						
					result.Append(commandLine[index], 0, commandLine[index].Length - 1);
					}
				}
			
			// Unquoted paths just go to the next space-dash.
			else
				{ 
				while (index < commandLine.Length && commandLine[index][0] != '-')
					{
					if (result.Length != 0)
						{  result.Append(' ');  }
						
					result.Append(commandLine[index]);
					index++;
					}
				}

			return result.ToString();
			}


		/* Function: FromAssembly
		 * 
		 * Returns a path to the passed .NET assembly.
		 * 
		 * This is preferable to using .NET's version because when executing under NUnit, GetExecutingAssembly().Location 
		 * will return a path to the shadow copy.  This isn't useful because this path is used to get things like the Config folder 
		 * which will only be available relative to the original file.  .NET's GetExecutingAssembly().CodeBase returns the
		 * correct path but in a weird format, so this function abstracts away the conversion from that to a normal Path.
		 */
		static public AbsolutePath FromAssembly (System.Reflection.Assembly assembly)
			{
			string codeBase = assembly.CodeBase;
			UriBuilder uri = new UriBuilder(codeBase);
			string assemblyPath = Uri.UnescapeDataString(uri.Path);

			// The above code always returns a path with slashes, even on Windows.  However, we can assume it's otherwise
			// properly normalized so just do a quick search and replace if it's needed.
			if (Engine.SystemInfo.PathSeparatorCharacter != '/')
				{  assemblyPath = assemblyPath.Replace('/', Engine.SystemInfo.PathSeparatorCharacter);  }

			// Actually, let's check that assumption in debug builds.
			#if DEBUG
			Path test = new Path(assemblyPath);
			if (test.pathString != assemblyPath)
				{  throw new Exception("Path wasn't properly normalized in Path.FromAssembly.");  }
			#endif

			Path result;
			result.pathString = assemblyPath;

			return (AbsolutePath)result;
			}


		/* Function: ToURL
		 * Converts the path to an URL string, meaning it will always use slashes as separators, even on Windows.
		 */
		public string ToURL ()
			{
			if (Engine.SystemInfo.PathSeparatorCharacter == '/')
				{  return pathString;  }
			else
				{  return pathString.Replace(Engine.SystemInfo.PathSeparatorCharacter, '/');  }
			}


			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Operator: operator string (Path)
		 * A cast operator to covert the Path to a string.
		 */
		public static implicit operator string (Path filePath)
			{
			return filePath.pathString;
			}
			
		/* Operator: operator Path (string)
		 * A cast operator to convert a string to a Path.  The string will be normalized.
		 */
		public static implicit operator Path (string newString)
			{
			return new Path(newString);
			}
			
		/* Operator: operator ==
		 */
		public static bool operator== (Path a, Path b)
			{
			return (Compare(a, b) == 0);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (Path a, Path b)
			{
			return (Compare(a, b) != 0);
			}

		/* Function: Compare
		 */
		public static int Compare (Path a, Path b)
			{
			// Since these are structs we don't have to worry about null objects.
			return String.Compare(a.pathString, b.pathString, Engine.SystemInfo.IgnoreCaseInPaths);
			}

		/* Function: ToString
		 * Returns the Path as a string.
		 */
		public override string ToString ()
			{
			return pathString;
			}
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return pathString.GetHashCode();
			}

		/* Function: Equals
		 */
		public override bool Equals (object obj)
			{
			if (obj is Path)
				{  return (this == (Path)obj);  }
			else if (obj is string)
				{  return (this == (string)obj);  }
			else
				{  return false;  }
			}
			
		/* Function: CompareTo
		 */
		public int CompareTo (object other)
			{
			return pathString.CompareTo(other.ToString());
			}
		
			
			
		// Group: Private Functions
		// __________________________________________________________________________
		
		
		/* Function: Normalize
		 * 
		 * Normalizes <pathString>.
		 * 
		 *		- Removes quotes on all platforms.
		 *		- Converts separators to the local platform.  Classic Mac separators are converted to /.
		 *		- Removes multiple consecutive separators.  Multiple classic Mac separators are converted to .. instead.
		 *		  The leading \\ on UNC paths is preserved.
		 *		- Removes . sections unless it would otherwise result in an empty string.
		 *		- Collapses .. sections unless they begin the path.
		 *		- Removes trailing separators.
		 *		- Converts tilde paths to the home directory, even on Windows.
		 */
		private void Normalize ()
			{
			if (pathString == null)
				{  return;  }
			

			// First strip all quotes.
			
			pathString = pathString.Replace ("\"", "");
			
			
			// Replace the tilde with the home directory.

			if ( pathString.Length >= 1 && pathString[0] == '~' && 
				 (pathString.Length == 1 || pathString[1] == '/' || pathString[1] == '\\' || pathString[1] == ':') )
				{
				string home = System.Environment.GetEnvironmentVariable("HOME");  // Unix, must be in all caps
				
				if (String.IsNullOrEmpty(home))
					{
					// Windows, capitalization doesn't matter
  					home = System.Environment.GetEnvironmentVariable("HOMEDRIVE") + 
  								  System.Environment.GetEnvironmentVariable("HOMEPATH");
  					}

				if (String.IsNullOrEmpty(home))
					{
					throw new System.Exception("Couldn't make tilde path absolute.  No home directory environment variables are defined.");
					}

				if (pathString.Length > 2)
					{  pathString = home + "/" + pathString.Substring(2);  }
				else
					{  pathString = home;  }
				}
			

			// Split the string.  Split() is designed to work with unnormalized strings as well.

			string prefix;
			List<string> sections;
			
			Split(out prefix, out sections);

			
			// Go through the sections.  Simpify out ".", and unless it's at the beginning of a relative path, "..".

			int i = 0;			
			while (i < sections.Count)
				{
				if (sections[i] == ".")
					{  sections.RemoveAt(i);  }
				else if (sections[i] == "..")
					{
					if (i == 0)
						{
						if (prefix == null)
							{  i++;  }
						else
							{  sections.RemoveAt(i);  }
						}
					else if (sections[i - 1] == "..")
						{  i++;  }
					else
						{
						sections.RemoveAt(i);
						sections.RemoveAt(i - 1);
						i--;
						}
					}
				else
					{  i++;  }
				}
				
				
			// Build our final string.
			
			char separator = Engine.SystemInfo.PathSeparatorCharacter;
			StringBuilder newPathString = new StringBuilder(pathString.Length);
			
			if (prefix == null)
				{  }
			else if (prefix == "/")
				{  newPathString.Append('/');  }
			else
				{  
				newPathString.Append(prefix);
				newPathString.Append(separator);
				}
			
			for (i = 0; i < sections.Count; i++)
				{
				if (i != 0)
					{  newPathString.Append(separator);  }
					
				newPathString.Append(sections[i]);
				}
			
				
			if (newPathString.Length == 0)
				{  pathString = ".";  }
			else
				{  pathString = newPathString.ToString();  }
			}


		

			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* string: pathString
		 * The path, _always_ in normalized form.  See <Normalize()> for what that means specifically.
		 */
		private string pathString;



		// Group: Static Variables
		// __________________________________________________________________________

		
		/* var: normalizedSeparators
		 * An array of the available normalized path separators.
		 */
		static private char[] normalizedSeparators = { '\\', '/' };
		
		/* var: unnormalizedSeparators
		 * An array of all the possible path separators before normalization.
		 */
		static private char[] unnormalizedSeparators = { '\\', '/', ':' };

		/* var: pathPrefixRegex
		 * The regular expression that capture the path prefix, such as "/" on Unix and "\\" or "c:" on Windows.
		 */
		static private Regex.Path.PathPrefix pathPrefixRegex = new Regex.Path.PathPrefix();
		}
	}