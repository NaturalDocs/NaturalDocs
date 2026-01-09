/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Component
 * ____________________________________________________________________________
 *
 * A base class for all HTML components.
 *
 *
 * Multithreading: Not Thread Safe
 *
 *		This class is only designed to be used by one thread at a time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public class Component
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Component
		 */
		public Component (Context context)
			{
			this.context = context;
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: WriteTextFile
		 * Creates a UTF-8 text file at the specified path, creating any subfolders as required and overwriting the file if
		 * it already exists.
		 */
		static public void WriteTextFile (Path path, string content)
			{
			try
				{
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory(path.ParentFolder);
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)",
																					 path.ParentFolder, e.Message) );
				}

			try
				{
				System.IO.File.WriteAllText(path, content, System.Text.Encoding.UTF8);
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFile(name, exception)",
																					 path, e.Message) );
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: EngineInstance
		 * The <Engine.Instance> associated with this component.
		 */
		public Engine.Instance EngineInstance
			{
			get
				{  return Target.EngineInstance;  }
			}


		/* Property: Target
		 * The <HTML.Target> associated with this component.
		 */
		public HTML.Target Target
			{
			get
				{  return context.Target;  }
			}


		/* Property: Context
		 * The <Context> associated with this component.
		 */
		virtual public Context Context
			{
			get
				{  return context;  }
			set
				{  context = value;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: context
		 * The <Context> this compontent is appearing in.
		 */
		protected Context context;

		}
	}
