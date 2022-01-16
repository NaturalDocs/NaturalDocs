/*
 * Class: CodeClear.NaturalDocs.Engine.Styles.Advanced
 * ____________________________________________________________________________
 *
 * A class representing an advanced output style using <Style.txt>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class Advanced : Style
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Advanced
		 * Pass the location of <Style.txt>.
		 */
		public Advanced (Path configFile) : base ()
			{
			this.configFile = configFile;
			this.folder = configFile.ParentFolder;
			}


		/* Function: Contains
		 * Returns whether this style contains the passed file.
		 */
		override public bool Contains (Path file)
			{
			if (!folder.Contains(file))
				{  return false;  }

			return (Styles.Manager.FileExtensions.Contains(file.Extension));
			}


		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this style.  If this style doesn't contain the file, it will return null.
		 */
		override public RelativePath MakeRelative (Path file)
			{
			if (folder.Contains(file))
				{  return file.MakeRelativeTo(folder);  }
			else
				{  return null;  }
			}


		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		override public bool IsSameFundamentalStyle (Style other)
			{
			return (other is Styles.Advanced && configFile == (other as Styles.Advanced).configFile);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 */
		override public string Name
			{
			get
				{  return folder.NameWithoutPath;  }
			}


		/* Property: ConfigFile
		 * A path to the style's <Style.txt> configuration file.
		 */
		public Path ConfigFile
			{
			get
				{  return configFile;  }
			}


		/* Property: Folder
		 * A path to the style's folder.
		 */
		public Path Folder
			{
			get
				{  return folder;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: configFile
		 * The path to the style's <Style.txt> file.
		 */
		protected Path configFile;


		/* var: folder
		 *
		 * The path to the style's folder.
		 *
		 * This could technically just be retrieved from <configFile> via <Path.ParentFolder> at runtime, but it's used often enough that
		 * it's worth storing it instead of generating it constantly.
		 */
		protected Path folder;

		}
	}
