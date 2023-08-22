/*
 * Class: CodeClear.NaturalDocs.Engine.Styles.Style
 * ____________________________________________________________________________
 *
 * A class representing an output style.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2022 Code Clear LLC.
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
		public void AddOnLoad (string onLoadString, string onLoadStringAfterSubstitutions, Config.PropertyLocation propertyLocation, PageType type = PageType.All)
			{
			if (onLoad == null)
				{  onLoad = new List<OnLoadStatement>();  }

			OnLoadStatement entry = new OnLoadStatement();
			entry.Type = type;
			entry.Statement = onLoadString;
			entry.StatementAfterSubstitutions = onLoadStringAfterSubstitutions;
			entry.PropertyLocation = propertyLocation;

			onLoad.Add(entry);
			}


		/* Function: AddOnLoad
		 */
		public void AddOnLoad (string onLoadString, string onLoadStringAfterSubstitutions, Config.PropertySource propertySource, PageType type = PageType.All)
			{
			AddOnLoad(onLoadString, onLoadStringAfterSubstitutions, new Config.PropertyLocation(propertySource), type);
			}


		/* Function: SetHomePage
		 */
		public void SetHomePage (AbsolutePath file, Config.PropertyLocation propertyLocation)
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
		public void SetHomePage (AbsolutePath file, Config.PropertySource propertySource)
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
		abstract public RelativePath MakeRelative (Path file);


		/* Function: IsSameFundamentalStyle
		 * Returns whether this style is fundamentally the same as the passed one, meaning any identifying properties will be the
		 * same (i.e. both referencing the same style folder) but secondary properties may be different.
		 */
		abstract public bool IsSameFundamentalStyle (Style other);


		/* Function: IsSameStyleAndProperties
		 * Returns whether this style is the same as the passed one, meaning both <IsSameFundamentalStyle()> plus all supporting
		 * properties.  It will return false if any setting is different.
		 */
		public bool IsSameStyleAndProperties (Style other, bool includeInheritedStyles = true)
			{
			if (!IsSameFundamentalStyle(other))
				{  return false;  }


			// Inherits

			int inheritsCount = (inherits == null ? 0 : inherits.Count);
			int otherInheritsCount = (other.inherits == null ? 0 : other.inherits.Count);

			if (inheritsCount != otherInheritsCount)
				{  return false;  }

			for (int i = 0; i < inheritsCount; i++)
				{
				if (inherits[i].HasSameProperties(other.inherits[i]) == false)
					{  return false;  }
				}


			// OnLoad

			int onLoadCount = (onLoad == null ? 0 : onLoad.Count);
			int otherOnLoadCount = (other.onLoad == null ? 0 : other.onLoad.Count);

			if (onLoadCount != otherOnLoadCount)
				{  return false;  }

			for (int i = 0; i < onLoadCount; i++)
				{
				if (onLoad[i].HasSameProperties(other.onLoad[i]) == false)
					{  return false;  }
				}


			// Links

			int linksCount = (links == null ? 0 : links.Count);
			int otherLinksCount = (other.links == null ? 0 : other.links.Count);

			if (linksCount != otherLinksCount)
				{  return false;  }

			for (int i = 0; i < linksCount; i++)
				{
				if (links[i].HasSameProperties(other.links[i]) == false)
					{  return false;  }
				}


			// Home Page

			if (homePage.HasSameProperties(other.homePage) == false)
				{  return false;  }


			// Compare inherited styles

			if (includeInheritedStyles && inheritsCount > 0)
				{
				// We're going to build inheritance lists and compare those instead of just recursing through the inherit statements.
				// Why?  The function that builds the inheritance list avoids circular dependencies so we won't have the potential
				// for an infinite loop.

				List<Style> inheritanceList = BuildInheritanceList();
				List<Style> otherInheritanceList = other.BuildInheritanceList();

				if (inheritanceList.Count != otherInheritanceList.Count)
					{  return false;  }

				for (int i = 0; i < inheritanceList.Count; i++)
					{
					if (!inheritanceList[i].IsSameStyleAndProperties(otherInheritanceList[i], includeInheritedStyles: false))
						{  return false;  }
					}
				}


			return true;
			}


		/* Function: BuildInheritanceList
		 * Returns a list of <Styles> containing this one and all its inherited styles in the order in which they should be applied.
		 */
		public List<Style> BuildInheritanceList ()
			{
			List<Style> inheritanceList = new List<Style>();

			AddToInheritanceList(ref inheritanceList);

			return inheritanceList;
			}


		/* Function: HomePageOf
		 * Returns the home page that should be used from the list of styles generated by <BuildInheritanceList()>, or null if it
		 * should use the default one.
		 */
		static public AbsolutePath HomePageOf (List<Style> inheritanceList)
			{
			// The inheritance list is in the order in which they should be applied, which means the last one is the highest level
			// one, so walk from the end of the list to the beginning.
			for (int i = inheritanceList.Count - 1; i >= 0; i--)
				{
				if (inheritanceList[i].HomePage != null)
					{  return inheritanceList[i].HomePage;  }
				}

			return null;
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
		public AbsolutePath HomePage
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

		public bool HasSameProperties (InheritStatement other)
			{  return (Name == other.Name);  }
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.LinkStatement
	 * ___________________________________________________________________________
	 */
	public struct LinkStatement
		{
		public PageType Type;
		public Path File;
		public Config.PropertyLocation PropertyLocation;

		public bool HasSameProperties (LinkStatement other)
			{  return (Type == other.Type && File == other.File);  }
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.OnLoadStatement
	 * ___________________________________________________________________________
	 */
	public struct OnLoadStatement
		{
		public PageType Type;
		public string Statement;
		public string StatementAfterSubstitutions;
		public Config.PropertyLocation PropertyLocation;

		public bool HasSameProperties (OnLoadStatement other)
			{  return (Type == other.Type && Statement == other.Statement && StatementAfterSubstitutions == other.StatementAfterSubstitutions);  }
		}


	/* Struct: CodeClear.NaturalDocs.Engine.Styles.HomePageStatement
	 * ___________________________________________________________________________
	 */
	public struct HomePageStatement
		{
		public AbsolutePath File;
		public Config.PropertyLocation PropertyLocation;

		public bool HasSameProperties (HomePageStatement other)
			{  return (File == other.File);  }
		}
	}
