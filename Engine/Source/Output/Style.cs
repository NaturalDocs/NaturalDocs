/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Style
 * ____________________________________________________________________________
 * 
 * A class representing an output style.
 *			
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;


namespace CodeClear.NaturalDocs.Engine.Output
	{
	public abstract class Style
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Style
		 * Pass the location of the .css file for CSS-only styles.  Pass the location of <Style.txt> for full styles.
		 */
		public Style ()
			{
			inherits = null;
			onLoad = null;
			links = null;
			}


		/* Function: AddInheritedStyle
		 */
		public void AddInheritedStyle (string name)
			{
			if (inherits == null)
				{  inherits = new List<string>();  }

			inherits.Add(name);
			}


		/* Function: AddLinkedFile
		 */
		public void AddLinkedFile (Path file, Builders.HTML.PageType type = Builders.HTML.PageType.All)
			{
			#if DEBUG
			if (file.IsRelative)
				{  throw new Exception("Paths passed to AddLinkedFile() must be absolute.");  }
			#endif

			if (links == null)
				{  links = new List<StyleFileLink>();  }

			StyleFileLink entry = new StyleFileLink();
			entry.Type = type;
			entry.File = file;

			links.Add(entry);
			}


		/* Function: AddOnLoad
		 */
		public void AddOnLoad (string onLoadString, Builders.HTML.PageType type = Builders.HTML.PageType.All)
			{
			if (onLoad == null)
				{  onLoad = new List<StyleOnLoadStatement>();  }

			StyleOnLoadStatement entry = new StyleOnLoadStatement();
			entry.Type = type;
			entry.Statement = onLoadString;

			onLoad.Add(entry);
			}


		/* Function: Contains
		 * Returns whether this style contains the passed file.
		 */
		abstract public bool Contains (Path file);


		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this style.  If this style doesn't contain the file, it will return null.
		 */
		abstract public Path MakeRelative (Path file);


		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		abstract public bool IsSameFundamentalStyle (Style other);



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 */
		public abstract string Name
			{  get;  }


		/* Property: Inherits
		 * A list of style names this one inherits, or null if none.  Do not change.
		 */
		public List<string> Inherits
			{
			get
				{  return inherits;  }
			}


		/* Property: OnLoad
		 * A list of JavaScript OnLoad code statements associated with this style, or null if none.  Do not change.
		 */
		public List<StyleOnLoadStatement> OnLoad
			{
			get
				{  return onLoad;  }
			}


		/* Property: Links
		 * A list of files to link to each output file, or null if none.  They can be .js, .json, or .css.  Each file is relative to 
		 * <Folder>.  Do not change.
		 */
		public List<StyleFileLink> Links
			{
			get
				{  return links;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: inherits
		 * A list of style names this one inherits.  Null if none.
		 */
		protected List<string> inherits;

		/* var: onLoad
		 * A list of OnLoad code statements associated with this style, or null if none.
		 */
		protected List<StyleOnLoadStatement> onLoad;

		/* var: links
		 * A list of files to link to each output file, which can be CSS, JS, or JSON.  Null if none.
		 */
		protected List<StyleFileLink> links;



		// Group: Static Variables
		// __________________________________________________________________________


		/* var: FileExtensions
		 * A <StringSet> of the supported extensions for files associated with styles.  This does not include image extensions; get
		 * those from <Files.Manager.ImageExtensions> instead.
		 */
		static public StringSet FileExtensions = new StringSet (KeySettings.IgnoreCase, new string[] { "css", "js", "json", "woff", "eot", "svg", "ttf" });

		}


	/* Struct: CodeClear.NaturalDocs.Engine.Output.StyleFileLink
	 * ___________________________________________________________________________
	 */
	public struct StyleFileLink
		{
		public Builders.HTML.PageType Type;
		public Path File;
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Output.StyleOnLoadStatement
	 * ___________________________________________________________________________
	 */
	public struct StyleOnLoadStatement
		{
		public Builders.HTML.PageType Type;
		public string Statement;
		}
	}