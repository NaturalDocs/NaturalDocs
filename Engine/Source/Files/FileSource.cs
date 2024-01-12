/*
 * Class: CodeClear.NaturalDocs.Engine.Files.FileSource
 * ____________________________________________________________________________
 *
 * The base class for a file source.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2024 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Files
	{
	abstract public class FileSource
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: FileSource
		 */
		public FileSource (Files.Manager manager)
			{
			this.manager = manager;
			}


		/* Function: Validate
		 * Checks whether the file source is valid and adds an error to the list if not.  The default implementation simply
		 * returns true.
		 */
		virtual public bool Validate (Errors.ErrorList errors)
			{
			return true;
			}


		/* Function: Contains
		 * Returns whether this file source contains the passed file.
		 */
		abstract public bool Contains (Path file);


		/* Function: MakeRelative
		 * Converts the passed absolute path to one relative to this source.  If this source doesn't contain the path, it will
		 * return null.
		 */
		abstract public Path MakeRelative (Path path);


		/* Function: MakeAbsolute
		 * Converts the passed relative path to an absolute one based on this source.  This may or may not result in a path
		 * that actually maps to an existing file.
		 */
		abstract public Path MakeAbsolute (Path path);


		/* Function: CharacterEncodingID
		 * Returns the character encoding ID of the passed file.  Zero means it's not a text file or use Unicode auto-detection,
		 * which will handle all forms of UTF-8, UTF-16, and UTF-32.  It's assumed that the file belongs to this file source.
		 */
		virtual public int CharacterEncodingID (Path file)
			{
			#if DEBUG
			if (!Contains(file))
				{  throw new Exception("Tried to call FileSource.CharacterEncodingID with a file that didn't belong to it.");  }
			#endif

			return 0;
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateAdderProcess
		 * Creates a new <FileSourceAdder> that can be used with this FileSource.
		 */
		abstract public FileSourceAdder CreateAdderProcess ();



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Manager
		 * The <Files.Manager> associated with this object.
		 */
		public Files.Manager Manager
			{
			get
				{  return manager;  }
			}

		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this object.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return Manager.EngineInstance;  }
			}

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including ones based on different
		 * classes.  For example, "Folder:[path]" or "VSProject:[file]".
		 */
		abstract public string UniqueIDString
			{  get;  }

		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		abstract public InputType Type
			{  get;  }

		/* Property: Number
		 * The number assigned to this FileSource.  Only necessary to implement if <Type> is <InputType.Source> or
		 * <InputType.Image>.
		 */
		virtual public int Number
			{
			get
				{
				if (Type == InputType.Source || Type == InputType.Image)
					{  throw new NotImplementedException();  }
				else
					{  return 0;  }
				}
			}

		/* Property: Name
		 * The name assigned to this FileSource, or null if one hasn't been set.  Only relevant if <Type> is <InputType.Source>.
		 */
		virtual public string Name
			{
			get
				{  return null;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		protected Files.Manager manager;

		}
	}
