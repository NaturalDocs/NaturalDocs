/*
 * Class: CodeClear.NaturalDocs.Engine.Styles.FileSource
 * ____________________________________________________________________________
 *
 * A file source that handles monitoring all the style files, both project and system.
 *
 * All output targets must load the styles they use with <Styles.Manager>, since it only submits files from its loaded
 * styles to <Files.Manager>.  Since style references across all output targets are pooled, you cannot rely on file
 * deletion notices when a style is removed from a particular target.  It may be referenced by other targets and
 * thus still be seen as a valid part of the project by <Files.Manager>.  Also, when a new style is added you should tell
 * <Styles.Manager> to reparse everything, since the style may already be in use by another target and thus you
 * won't get the new/change notice automatically.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class FileSource : Engine.Files.FileSource
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: FileSource
		 * Instance constructor.  If the path is relative it will be made absolute using the current working folder.
		 */
		public FileSource (Styles.Manager manager) : base (manager.EngineInstance.Files)
			{
			}


		/* Function: Contains
		 * Returns whether this file source contains the passed file.
		 */
		override public bool Contains (Path file)
			{
			var styles = EngineInstance.Styles.LoadedStyles;

			foreach (var style in styles)
				{
				if (style.Contains(file))
					{  return true;  }
				}

			return false;
			}


		/* Function: MakeRelative
		 * If the passed absolute <Path> is contained by this file source, returns a relative path to it.  Otherwise returns null.
		 */
		override public Path MakeRelative (Path file)
			{
			var styles = EngineInstance.Styles.LoadedStyles;

			foreach (var style in styles)
				{
				if (style.Contains(file))
					{  return style.MakeRelative(file);  }
				}

			return null;
			}


		override public Path MakeAbsolute (Path path)
			{
			throw new InvalidOperationException();
			}



		// Group: Processes
		// __________________________________________________________________________


		/* Function: CreateAdderProcess
		 * Creates a <Files.FileSourceAdder> that can be used with this FileSource.
		 */
		override public Files.FileSourceAdder CreateAdderProcess ()
			{
			return new Styles.FileSourceAdder(this, EngineInstance);
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: UniqueIDString
		 * A string that uniquely identifies this FileSource among all others of its <Type>, including FileSources based on other
		 * classes.
		 */
		override public string UniqueIDString
			{
			get
				{
				// Since we only have one FileSource for all the combined styles in all the output targets, we don't need to append
				// any sort of path or style name information.
				return "Styles:";
				}
			}

		/* Property: Type
		 * The type of files this FileSource provides.
		 */
		override public Files.InputType Type
			{
			get
				{  return Files.InputType.Style;  }
			}

		}
	}
