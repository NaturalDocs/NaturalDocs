/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSources.Folder
 * ____________________________________________________________________________
 *
 * A base class for folder-based file sources.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;


namespace CodeClear.NaturalDocs.Engine.Files.FileSources
	{
	abstract public class Folder : FileSource
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Folder
		 */
		public Folder (Files.Manager manager) : base (manager)
			{
			}


		/* Function: Validate
		 * Makes sure the folder exists and adds an error if not.
		 */
		override public bool Validate (Errors.ErrorList errors)
			{
			if (System.IO.Directory.Exists(Path))
				{  return true;  }
			else
				{
				errors.Add(
					Locale.Get("NaturalDocs.Engine", "Error.FolderDoesntExist(type, name)", Type.ToString().ToLower(CultureInfo.InvariantCulture), Path)
					);

				return false;
				}
			}


		/* Function: Contains
		 * Returns whether this folder contains the passed file.
		 */
		override public bool Contains (Path file)
			{
			return Path.Contains(file);
			}


		/* Function: MakeRelative
		 * Converts the passed absolute path to one relative to this source.  If this source doesn't contain the path, it will
		 * return null.
		 */
		override public Path MakeRelative (Path path)
			{
			if (this.Path.Contains(path))
				{  return path.MakeRelativeTo(this.Path);  }
			else
				{  return null;  }
			}


		/* Function: MakeAbsolute
		 * Converts the passed relative path to an absolute one based on this source.  This may or may not result in a path
		 * that actually maps to an existing file.
		 */
		override public Path MakeAbsolute (Path path)
			{
			return (this.Path + "/" + path);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Path
		 * The path to the FileSource's folder.
		 */
		abstract public Path Path
			{  get;  }

		}
	}
