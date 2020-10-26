/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.Style
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


namespace CodeClear.NaturalDocs.Engine.Styles
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
			homePage = default;
			}


		/* Function: AddInheritedStyle
		 */
		public void AddInheritedStyle (string name, Config.PropertyLocation propertyLocation, Style styleObject = null)
			{
			if (inherits == null)
				{  inherits = new List<InheritStatement>();  }

			InheritStatement entry = new InheritStatement();
			entry.Name = name;
			entry.Style = styleObject;
			entry.PropertyLocation = propertyLocation;

			inherits.Add(entry);
			}


		/* Function: AddInheritedStyle
		 */
		public void AddInheritedStyle (string name, Config.PropertySource propertySource, Style styleObject = null)
			{
			AddInheritedStyle(name, new Config.PropertyLocation(propertySource), styleObject);
			}


		/* Function: AddLinkedFile
		 */
		public void AddLinkedFile (Path file, Config.PropertyLocation propertyLocation, PageType type = PageType.All)
			{
			#if DEBUG
			if (file.IsRelative)
				{  throw new Exception("Paths passed to AddLinkedFile() must be absolute.");  }
			#endif

			if (links == null)
				{  links = new List<LinkStatement>();  }

			LinkStatement entry = new LinkStatement();
			entry.Type = type;
			entry.File = file;
			entry.PropertyLocation = propertyLocation;

			links.Add(entry);
			}


		/* Function: AddLinkedFile
		 */
		public void AddLinkedFile (Path file, Config.PropertySource propertySource, PageType type = PageType.All)
			{
			AddLinkedFile(file, new Config.PropertyLocation(propertySource), type);
			}


		/* Function: AddOnLoad
		 */
		public void AddOnLoad (string onLoadString, Config.PropertyLocation propertyLocation, PageType type = PageType.All)
			{
			if (onLoad == null)
				{  onLoad = new List<OnLoadStatement>();  }

			OnLoadStatement entry = new OnLoadStatement();
			entry.Type = type;
			entry.Statement = onLoadString;
			entry.PropertyLocation = propertyLocation;

			onLoad.Add(entry);
			}


		/* Function: AddOnLoad
		 */
		public void AddOnLoad (string onLoadString, Config.PropertySource propertySource, PageType type = PageType.All)
			{
			AddOnLoad(onLoadString, new Config.PropertyLocation(propertySource), type);
			}


		/* Function: SetHomePage
		 */
		public void SetHomePage (Path file, Config.PropertyLocation propertyLocation)
			{
			#if DEBUG
			if (file.IsRelative)
				{  throw new Exception("Paths passed to SetHomePage() must be absolute.");  }
			#endif

			homePage.File = file;
			homePage.PropertyLocation = propertyLocation;
			}


		/* Function: SetHomePage
		 */
		public void SetHomePage (Path file, Config.PropertySource propertySource)
			{
			SetHomePage(file, new Config.PropertyLocation(propertySource));
			}



		// Group: Information Functions
		// __________________________________________________________________________


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


		/* Function: BuildInheritanceList
		 * Returns a list of <Styles> containing this one and all its inherited styles in the order in which they should be applied.
		 */
		public List<Style> BuildInheritanceList ()
			{
			List<Style> inheritanceList = new List<Style>();

			AddToInheritanceList(ref inheritanceList);

			return inheritanceList;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: AddToInheritanceList
		 * A recursive function used by <BuildInheritanceList()>.
		 */
		protected void AddToInheritanceList (ref List<Style> inheritanceList)
			{
			// First see if this style already exists in the list.  If it does, we're done.  This not only prevents duplicates, it also
			// guards against circular dependencies creating an infinite loop.

			foreach (var style in inheritanceList)
				{
				if (this.IsSameFundamentalStyle(style))
					{  return;  }
				}

			// Next, recursively add any children.  We add them before ourself because the current style must appear after all
			// its inherited ones so it can override things.

			if (inherits != null)
				{
				foreach (var inheritStatement in inherits)
					{
					inheritStatement.Style.AddToInheritanceList(ref inheritanceList);
					}
				}

			// Now we can finally add ourself.

			inheritanceList.Add(this);
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Name
		 */
		public abstract string Name
			{  get;  }


		/* Property: Inherits
		 * A list of styles this one inherits, or null if none.  Do not change.
		 */
		public List<InheritStatement> Inherits
			{
			get
				{  return inherits;  }
			}


		/* Property: OnLoad
		 * A list of JavaScript OnLoad code statements associated with this style, or null if none.  Do not change.
		 */
		public List<OnLoadStatement> OnLoad
			{
			get
				{  return onLoad;  }
			}


		/* Property: Links
		 * A list of files to link to each output file, or null if none.  They can be .js, .json, or .css.  Each link stores an absolute
		 * path but it will be contained in <Folder>.  Do not change.
		 */
		public List<LinkStatement> Links
			{
			get
				{  return links;  }
			}


		/* Property: HomePage
		 * The home page HTML file this style uses, or null if none.
		 */
		public Path HomePage
			{
			get
				{  return homePage.File;  }
			}



		// Group: Variables
		// __________________________________________________________________________


		/* var: inherits
		 * A list of styles this one inherits, or null if none.
		 */
		protected List<InheritStatement> inherits;

		/* var: onLoad
		 * A list of OnLoad code statements associated with this style, or null if none.
		 */
		protected List<OnLoadStatement> onLoad;

		/* var: links
		 * A list of files to link to each output file, which can be CSS, JS, or JSON.  Null if none.
		 */
		protected List<LinkStatement> links;

		/* var: homePage
		 * The home page for this style, or undefined if none.
		 */
		protected HomePageStatement homePage;

		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.InheritStatement
	 * ___________________________________________________________________________
	 */
	public struct InheritStatement
		{
		public string Name;
		public Style Style;
		public Config.PropertyLocation PropertyLocation;
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.LinkStatement
	 * ___________________________________________________________________________
	 */
	public struct LinkStatement
		{
		public PageType Type;
		public Path File;
		public Config.PropertyLocation PropertyLocation;
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.OnLoadStatement
	 * ___________________________________________________________________________
	 */
	public struct OnLoadStatement
		{
		public PageType Type;
		public string Statement;
		public Config.PropertyLocation PropertyLocation;
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.HomePageStatement
	 * ___________________________________________________________________________
	 */
	public struct HomePageStatement
		{
		public Path File;
		public Config.PropertyLocation PropertyLocation;
		}
	}