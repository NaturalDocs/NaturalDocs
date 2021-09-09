/* 
 * Struct: CodeClear.NaturalDocs.Engine.AbsolutePath
 * ____________________________________________________________________________
 * 
 * A struct encapsulating a file path string which must be absolute.  This is a subset of the <Path> struct
 * which only contains the functionality relevant to absolute paths.  It's good for code clarity and safety to
 * use this instead of <Path> when the value must be absolute.
 * 
 * See <Path> for more details on why these structs are used at all.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine
	{
	public struct AbsolutePath : IComparable
		{
		
		// Group: Constructors
		// __________________________________________________________________________
		
			
		/* Constructor: AbsolutePath (string)
		 */
		public AbsolutePath (string pathString) : this (new Path(pathString))
			{
			}

		/* Constructor: AbsolutePath (Path)
		 */
		public AbsolutePath (Path path)
			{
			if (path != null && path.IsRelative)
				{  throw new Exceptions.PathMustBeAbsolute(path);  }

			this.path = path;
			}



		// Group: Properties
		// __________________________________________________________________________
		
			
		/* Property: Length
		 * The length of the path string.
		 */
		public int Length
			{
			get
				{  return path.Length;  }
			}
			
		/* Property: IsAbsolute
		 * Whether the path is absolute.  This will always be true, it's just for consistency with <Path>.
		 */
		public bool IsAbsolute
			{
			get
				{  return true;  }
			}
			
		/* Property: IsRelative
		 * Whether the path is relative.  This will always be false, it's just for consistency with <Path>.
		 */
		public bool IsRelative
			{
			get
				{  return false;  }
			}
		
		/* Property: Prefix
		 * The prefix of the path.  The prefix can be "C:", "\\server", or "/".
		 */
		public string Prefix
			{
			get
				{  return path.Prefix;  }
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
				{  return path.Extension;  }				
			}
			
		/* Property: ParentFolder
		 * The parent folder of the path.  If the path is to a file, it will be a path to its containing folder.  If the path is to a folder, it
		 * will be the folder above it.  It will stop at the volume, so the parent folder of C: is C:.
		 */
		public AbsolutePath ParentFolder
			{
			get
				{
				// We know the parent of an absolute folder is also absolute, so create it this way to avoid the check in the constructor.
				AbsolutePath parent;
				parent.path = path.ParentFolder;
				return parent;
				}
			}
			
		/* Property: NameWithoutPath
		 * The file name without its path.
		 */
		public string NameWithoutPath
			{
			get
				{  return path.NameWithoutPath;  }
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
				{  return path.NameWithoutPathOrExtension;  }
			}
			
			

		// Group: Functions
		// __________________________________________________________________________
		
			
		/* Function: Contains
		 * Returns whether this path contains the passed one, meaning it's a higher level folder.
		 */
		public bool Contains (Path other)
			{
			return path.Contains(other);
			}
			
			
		/* Function: MakeRelativeTo
		 * Returns the path as one relative to the passed folder, if possible.  If it's not possible (for example, if they're on
		 * different drive letters) it returns null.
		 */
		public RelativePath MakeRelativeTo (Path folder)
			{
			return path.MakeRelativeTo(folder);
			}
			
			
		/* Function: Split
		 * Splits the path into a prefix and a list of strings, each representing a segment of it.  Prefix will be set to something like "C:",
		 * "\\server", or "/".  The sections array will have an entry for each folder name and one for the file name if there was one.  No
		 * separator characters will be included.  It will return an empty list if there's nothing other than the prefix.
		 */
		public void Split (out string prefix, out List<string> sections)
			{
			path.Split(out prefix, out sections);
			}

			
		/* Function: ToURL
		 * Converts the path to an URL string, meaning it will always use slashes as separators, even on Windows.
		 */
		public string ToURL ()
			{
			return path.ToURL();
			}


			
		// Group: Operators
		// __________________________________________________________________________
		
		
		/* Operator: operator string (AbsolutePath)
		 * A cast operator to convert the AbsolutePath to a string.
		 */
		public static implicit operator string (AbsolutePath absolutePath)
			{
			return (string)absolutePath.path;
			}
			
		/* Operator: operator Path (AbsolutePath)
		 * A cast operator to convert the AbsolutePath to a <Path>.
		 */
		public static implicit operator Path (AbsolutePath absolutePath)
			{
			return absolutePath.path;
			}
			
		/* Operator: operator AbsolutePath (string)
		 * A cast operator to convert a string to an AbsolutePath.  The string will be normalized.  It will throw
		 * an exception if it's a relative path.
		 */
		public static implicit operator AbsolutePath (string pathString)
			{
			return new AbsolutePath(pathString);
			}
			
		/* Operator: operator AbsolutePath (Path)
		 * A cast operator to convert a <Path> to an AbsolutePath.  It will throw an exception if it's relative.
		 */
		public static explicit operator AbsolutePath (Path path)
			{
			return new AbsolutePath(path);
			}
			
		/* Operator: operator ==
		 */
		public static bool operator== (AbsolutePath a, AbsolutePath b)
			{
			return (Path.Compare(a, b) == 0);
			}

		/* Operator: operator !=
		 */
		public static bool operator!= (AbsolutePath a, AbsolutePath b)
			{
			return (Path.Compare(a, b) != 0);
			}

		/* Function: ToString
		 * Returns the Path as a string.
		 */
		public override string ToString ()
			{
			return path.ToString();
			}
			
		/* Function: GetHashCode
		 */
		public override int GetHashCode ()
			{
			return path.GetHashCode();
			}

		/* Function: Equals
		 */
		public override bool Equals (object obj)
			{
			if (obj is Path || obj is AbsolutePath)
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
			return path.CompareTo(other);
			}

			
		
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: path
		 * The <Path> struct this encapsulates.
		 */
		private Path path;

		}
	}