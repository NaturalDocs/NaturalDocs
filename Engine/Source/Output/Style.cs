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
	public class Style
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: Style
		 * Pass the location of the .css file for CSS-only styles.  Pass the location of <Style.txt> for full styles.
		 */
		public Style (Path location)
			{
			this.location = location;
			isCSSOnly = (string.Compare(location.Extension, "css", true) == 0);

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
		public bool Contains (Path file)
			{
			if (IsCSSOnly)
				{  return (file == location);  }
			else
				{  
				if (!Folder.Contains(file))
					{  return false;  }

				string extension = file.Extension;

				return (StyleExtensions.Contains(extension) || Files.Manager.ImageExtensions.Contains(extension));
				}
			}


		/* Function: MakeRelative
		 * Converts the passed filename to one relative to this style.  If this style doesn't contain the file, it will return null.
		 */
		public Path MakeRelative (Path file)
			{
			if (IsCSSOnly)
				{  
				if (file == CSSFile)
					{  return file.NameWithoutPath;  }
				else
					{  return null;  }
				}
			else
				{
				if (Folder.Contains(file))
					{  return file.MakeRelativeTo(Folder);  }
				else
					{  return null;  }
				}
			}


		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		public bool IsSameFundamentalStyle (Style other)
			{
			return (location == other.location);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 */
		public string Name
			{
			get
				{
				if (IsCSSOnly)
					{  return location.NameWithoutPathOrExtension;  }
				else
					{
					string folder = location.ParentFolder.NameWithoutPath;
					if (folder.EndsWith(".HTML", StringComparison.CurrentCultureIgnoreCase))
						{  return folder.Substring(0, folder.Length - 5);  }
					else
						{  return folder;  }
					}
				}
			}

		/* Property: Location
		 * The path to the style.  If <IsCSSOnly> is set, this is the full <Path> to the CSS file.  If not, this is
		 * the full <Path> to the style's <Style.txt>.
		 */
		public Path Location
			{
			get
				{  return location;  }
			}

		/* Property: IsCSSOnly
		 * Whether the style is simply a CSS file, like Blue.css, instead of a collection of files in a folder.
		 */
		public bool IsCSSOnly
			{
			get
				{  return isCSSOnly;  }
			}

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

		/* Property: CSSFile
		 * If <IsCSSOnly> is set, returns a path to the style's CSS file.  Will throw an exception if you attempt to read this
		 * variable when <IsCSSOnly> is not set.
		 */
		public Path CSSFile
			{
			get
				{
				if (IsCSSOnly)
					{  return location;  }
				else
					{  throw new InvalidOperationException();  }
				}
			}

		/* Property: ConfigFile
		 * If <IsCSSOnly> is not set, returns a path to the style's <Style.txt> configuration file.  Will throw an exception if 
		 * you attempt to read this variable while <IsCSSOnly> is set.
		 */
		public Path ConfigFile
			{
			get
				{
				if (IsCSSOnly)
					{  throw new InvalidOperationException();  }
				else
					{  return location;  }
				}
			}

		/* Property: Folder
		 * If <IsCSSOnly> is not set, returns a path to the style's folder.  Will throw an exception if you attempt to read this
		 * variable while <IsCSSOnly> is set.
		 */
		public Path Folder
			{
			get
				{
				if (IsCSSOnly)
					{  throw new InvalidOperationException();  }
				else
					{  return location.ParentFolder;  }
				}
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: location
		 * The path to the style.  If <isCSSOnly> is set, this is the full <Path> to the CSS file.  If not, this is
		 * the full <Path> to the style's <Style.txt>.
		 */
		protected Path location;

		/* var: isCSSOnly
		 * Whether <location> is a path to a CSS file or <Style.txt>.  This can be inferred from <location> itself, but it's checked
		 * so often that it's preferable to have a more efficient means of accessing it.
		 */
		protected bool isCSSOnly;

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


		/* var: StyleExtensions
		 * A <StringSet> of the supported extensions for files associated with styles.  This does not include image extensions; get
		 * those from <Files.Manager.ImageExtensions> instead.
		 */
		static public StringSet StyleExtensions = new StringSet (KeySettings.IgnoreCase, new string[] { "css", "js", "json", "woff", "eot", "svg", "ttf" });

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