/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * An output builder for HTML.
 * 
 * 
 * File: Output.nd
 * 
 *		A file used to store information about the last time this output target was built.
 *		
 *		> [String: Style Path]
 *		> [String: Style Path]
 *		> ...
 *		> [String: null]
 *		
 *		Stores the list of styles that apply to this target, in the order in which they must be loaded, as a null-terminated
 *		list of style paths.  The paths are either to <HTMLStyle.CSSFile> or <HTMLStyle.ConfigFile>.  These are stored
 *		instead of the names so that if a name is interpreted differently from one run to the next it will be detected.  It's
 *		also the computed list of styles after all inheritance has been applied.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2008 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Output.Styles;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML : Builder, Files.IStyleChangeWatcher
		{

		/* enum: BuildFlags
		 * Flags that specify what parts of the HTML output structure still need to be built.
		 * 
		 * MainStyleFiles - main.css and main.js
		 * Everything - The combination of everything above.
		 */
		[Flags]
		protected enum BuildFlags : byte {
			IndexFile = 0x01,
			MainStyleFiles = 0x02,

			Everything = IndexFile | MainStyleFiles
			}


		/* enum: PageType
		 * Used for specifying the type of page something applies to.
		 * 
		 * All - Applies to all page types.
		 * Index - Applies to index.html.
		 * Content - Applies to page content for a source file or class.
		 */
		public enum PageType : byte {
			// Indexes are manual and start at zero so they can be used as indexes into AllPageTypeNames.
  			All = 0,
			Index = 1,
			Content = 2
			}

		

		// Group: Functions
		// __________________________________________________________________________
		
		
		public HTML (Config.Entries.HTMLOutputFolder configEntry) : base ()
			{
			writeLock = new object();
			sourceFilesToRebuild = new IDObjects.NumberSet();
			foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false );
			buildFlags = BuildFlags.Everything;
			config = configEntry;
			styles = null;
			}


		public override bool Start (Errors.ErrorList errorList)
			{  
			int errors = errorList.Count;


			// Validate the output folder.

			if (System.IO.Directory.Exists((config as Config.Entries.HTMLOutputFolder).Folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.FolderDoesntExist(type, name)", "output", 
																 (config as Config.Entries.HTMLOutputFolder).Folder) );
				return false;
				}


			// Load and validate the styles.

			string styleName = config.ProjectInfo.StyleName ?? "Default";
			Path stylePath = FindStyle(styleName);

			if (stylePath == null)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.CantFindStyle(name)", styleName) );
				return false;
				}

			styles = new List<HTMLStyle>();
			StringSet definedStyles = new StringSet( Config.Manager.IgnoreCaseInPaths, false );

			if (!Start_LoadStyles(styleName, stylePath, styles, definedStyles, errorList))
				{  return false;  }


			// Load and compare to the previous list of styles.

			List<HTMLStyle> previousStyles;
			bool hasBinaryFile = LoadBinaryFile(config.OutputWorkingDataFile, out previousStyles);

			if (!hasBinaryFile)
				{
				// If the binary file doesn't exist, we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.
				if (System.IO.Directory.Exists(RootStyleFolder))
					{  
					try
						{  System.IO.Directory.Delete(RootStyleFolder, true);  }
					catch
						{
						// If something is reading a file this will fail.  It's not worth stopping the program so just continue silently.
						}
					}

				Engine.Instance.Output.ReparseStyleFiles = true;
				}
			else
				{
				// Purge folders of anything deleted.  Since IsSameFundamentalStyle relies on the path and not just the name,
				// different styles with the same name will be handled correctly.
				foreach (HTMLStyle previousStyle in previousStyles)
					{
					bool stillExists = false;

					foreach (HTMLStyle currentStyle in styles)
						{
						if (currentStyle.IsSameFundamentalStyle(previousStyle))
							{
							stillExists = true;
							break;
							}
						}

					if (stillExists == false)
						{  
						if (System.IO.Directory.Exists( StyleOutputFolder(previousStyle) ))
							{  
							try
								{  System.IO.Directory.Delete( StyleOutputFolder(previousStyle), true);  }
							catch  
								{
								// If something is reading a file this will fail.  It's not worth stopping the program so just continue silently.
								}
							}
						 }
					}

				// Reparse on anything added.
				foreach (HTMLStyle currentStyle in styles)
					{
					bool foundMatch = false;

					foreach (HTMLStyle previousStyle in previousStyles)
						{
						if (previousStyle.IsSameFundamentalStyle(currentStyle))
							{
							foundMatch = true;
							break;
							}
						}

					if (foundMatch == false)
						{  
						Engine.Instance.Output.ReparseStyleFiles = true;
						break;
						}
					}
				}


			// Resave the Style.txt-based styles.

			foreach (HTMLStyle style in styles)
				{
				if (style.IsCSSOnly == false)
					{  
					// No error on save for system styles.
					SaveStyle(style, errorList, style.IsSystemStyle);  
					}
				}


			// Save output.nd.

			SaveBinaryFile(config.OutputWorkingDataFile, styles);


			return (errors == errorList.Count);
			}

			
		/* Function: Start_LoadStyles
		 * A recursive helper function used only by <Start()> which loads a style and everything it inherits.
		 * 
		 * Parameters:
		 *    styleName - The name of the style being loaded.  Must already be determined to exist.
		 *    stylePath - The path of the style being loaded.  Must already be determined to exist.
		 *    loadList - A list of <HTMLStyles> in the order in which they should be added to the output.
		 *    definedStyles - A set of style names that have been defined thus far.
		 *    errorList - Any errors found in <Style.txt> will be added here.
		 *		
		 * Returns:
		 *    Whether it completed without errors.
		 */
		private bool Start_LoadStyles (string styleName, string stylePath, List<HTMLStyle> loadList, StringSet definedStyles,
																	Errors.ErrorList errorList)
			{
			// If we're already defined, quit to avoid circular inheritance.  We don't add an error message because a style can
			// also be inherited by two separate styles, meaning this function would get called for it twice without it being an
			// error condition.
			if (definedStyles.Contains(styleName))
				{  return true;  }

			// We need to add ourself to the defined styles before processing inheritance to be able to detect this though.
			definedStyles.Add(styleName);

			int errors = errorList.Count;

			HTMLStyle style = LoadStyle(stylePath, errorList);

			if (style == null)
				{  return false;  }

			// If there's only one style and it's CSS-only, it inherits Default automatically.
			if (definedStyles.Count == 1 && style.IsCSSOnly && styleName != "Default")
				{  style.AddInheritedStyle("Default");  }

			if (style.Inherits != null)
				{
				foreach (string inheritedStyleName in style.Inherits)
					{
					Path inheritedStylePath = FindStyle(inheritedStyleName);

					if (inheritedStylePath == null)
						{
						Path configFile = (style.IsCSSOnly ? null : style.ConfigFile);

						errorList.Add( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.CantFindInheritedStyle(name)", inheritedStyleName),
													configFile );

						// We don't return because we want to find and report all possible errors, not just the first one.
						}
					else
						{
						Start_LoadStyles (inheritedStyleName, inheritedStylePath, loadList, definedStyles, errorList);
						// Ditto on returning if false.
						}
					}
				}

			// We add ourself to the load list AFTER processing inheritance so that it acts like a depth first search.  Inherited 
			// members need to be loaded first.
			loadList.Add(style);

			return (errorList.Count == errors);
			}


		public override void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			CodeDB.Accessor accessor = null;
			bool haveLock = false;
			
			try
				{
				for (;;)
					{
					Monitor.Enter(writeLock);
					haveLock = true;
					
					if (cancelDelegate())
						{  return;  }

					if ((buildFlags & BuildFlags.IndexFile) != 0)
						{
						buildFlags &= ~BuildFlags.IndexFile;
						Monitor.Exit(writeLock);
						haveLock = false;

						BuildIndexFile(cancelDelegate);

						if (cancelDelegate())
							{
							lock (writeLock)
								{  buildFlags |= BuildFlags.IndexFile;  }
							}
						}
						
					else if ((buildFlags & BuildFlags.MainStyleFiles) != 0)
						{
						buildFlags &= ~BuildFlags.MainStyleFiles;
						Monitor.Exit(writeLock);
						haveLock = false;

						BuildMainStyleFiles(cancelDelegate);

						if (cancelDelegate())
							{
							lock (writeLock)
								{  buildFlags |= BuildFlags.MainStyleFiles;  }
							}
						}
						
					else if (sourceFilesToRebuild.IsEmpty == false)
						{
						int sourceFileToRebuild = sourceFilesToRebuild.Highest;
						sourceFilesToRebuild.Remove(sourceFileToRebuild);
						
						Monitor.Exit(writeLock);
						haveLock = false;
						
						if (accessor == null)
							{  accessor = Engine.Instance.CodeDB.GetAccessor();  }
							
						BuildSourceFile(sourceFileToRebuild, accessor, cancelDelegate);
						
						if (cancelDelegate())
							{
							lock (writeLock)
								{  sourceFilesToRebuild.Add(sourceFileToRebuild);  }
							}						
						}
						
					else
						{  break;  }
						
					if (cancelDelegate())
						{  return;  }
					}
				}
			finally
				{
				if (haveLock)
					{  Monitor.Exit(writeLock);  }
				if (accessor != null)
					{  accessor.Dispose();  }
				}
			}
			

			
		// Group: Builder Functions
		// __________________________________________________________________________


		/* Function: BuildFile
		 * Builds an output file based on the passed parameters.  Using this function centralizes standard elements of the page
		 * structure like the doctype, charset, and embedded comments.
		 */
		public void BuildFile (Path outputPath, string pageTitle, string pageContentHTML, PageType pageType)
			{
			using (System.IO.StreamWriter file = CreateTextFileAndPath(outputPath))
				{
				file.Write(

					"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">" +
					"\r\n\r\n" +

					"<html>" +
						"<head>" +

							"<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">" +

							"<title>" + TextConverter.TextToHTML(pageTitle) + "</title>" +

							"<link rel=\"stylesheet\" type=\"text/css\" href=\"" +
								MakeRelativeURL(outputPath, RootStyleFolder + "/main.css") +
								"\">");

							string allName = PageTypeNameOf(PageType.All);
							string typeName = PageTypeNameOf(pageType);

							string allJSRelativeURL = MakeRelativeURL(outputPath, RootStyleFolder + "/main-" + allName.ToLower() + ".js");
							string typeJSRelativeURL = MakeRelativeURL(outputPath, RootStyleFolder + "/main-" + typeName.ToLower() + ".js");
							string jsRelativePrefix = allJSRelativeURL.Substring(0, allJSRelativeURL.Length - allName.Length - 8);


							file.Write(
							"<script type=\"text/javascript\" src=\"" + allJSRelativeURL + "\"></script>" +
							"<script type=\"text/javascript\" src=\"" + typeJSRelativeURL + "\"></script>" +
							"<script type=\"text/javascript\">" +
								"NDLoadJS_" + allName + "('" + jsRelativePrefix + "');" +
								"NDLoadJS_" + typeName + "('" + jsRelativePrefix + "');" +
							"</script>" +

						"</head>" + 

							"\r\n\r\n" +
							"<!-- Generated by Natural Docs, version " + Instance.VersionString + " -->" +
							"\r\n\r\n" +

							// The IE mark of the web which prevents it from popping up the information bar when loading HTML from the
							// local drive.  Note that it MUST have at least one \r\n after it or it won't work.
							"<!-- saved from url=(0026)http://www.naturaldocs.org -->" +
							"\r\n\r\n" +

						"<body onload=\"NDOnLoad_" + allName + "();NDOnLoad_" + typeName + "();\" " +
									 "class=\"NDPage ND" + typeName + "Page\">" +

							pageContentHTML +
								
						"</body>" +
					"</html>");
				}
			}


		/* Function: BuildIndexFile
		 * Builds index.html, which provides the documentation frame.
		 */
		public void BuildIndexFile (CancelDelegate cancelDelegate)
			{

			// Page and header titles

			string rawPageTitle;
			string rawHeaderTitle;
			string rawHeaderSubTitle;

			if (config.ProjectInfo.Title == null)
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.DefaultPageTitle");
				rawHeaderTitle = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHeaderTitle");
				rawHeaderSubTitle = null;
				}
			else
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.PageTitle(projectTitle)", config.ProjectInfo.Title);
				rawHeaderTitle = Locale.Get("NaturalDocs.Engine", "HTML.HeaderTitle(projectTitle)", config.ProjectInfo.Title);

				if (config.ProjectInfo.Subtitle == null)
					{  rawHeaderSubTitle = null;  }
				else
					{
					rawHeaderSubTitle = Locale.Get("NaturalDocs.Engine", "HTML.HeaderSubTitle(projectSubTitle)",
																				config.ProjectInfo.Subtitle);
					}
				}


			// Footer

			string rawTimeStamp = config.ProjectInfo.MakeTimeStamp();


			// Final page structure

			StringBuilder content = new StringBuilder();

			content.Append(

				"<div id=\"NDHeader\">" +
					"<div id=\"HTitle\">" +
					
						TextConverter.TextToHTML(rawHeaderTitle) +
					
					"</div>");

					if (rawHeaderSubTitle != null)
						{  
						content.Append(
							"<div id=\"HSubTitle\">" +
								TextConverter.TextToHTML(rawHeaderSubTitle) +
							"</div>");  
						}

				content.Append(
				"</div>" +

				"<div id=\"NDMenu\">xxx" +
				"</div>" +

				"<div id=\"NDContent\">xxx" +
				"</div>" +

				"<div id=\"NDFooter\">");

					if (config.ProjectInfo.Copyright != null)
						{
						content.Append(
							"<div id=\"FCopyright\">" +
								TextConverter.TextToHTML(config.ProjectInfo.Copyright) +
							"</div>");
						}

					if (rawTimeStamp != null)
						{
						content.Append(
							"<div id=\"FTimeStamp\">" +
								TextConverter.TextToHTML(rawTimeStamp) +
							"</div>");
						}

					content.Append(
					"<div id=\"FGeneratedBy\">" +

						// Deliberately hard coded (as opposed to using Locale) so it stays consistent and we can find users of any
						// language by putting it into a search engine.  If they don't want it in their docs they can set #FGeneratedBy 
						// to display: none.
						"<a href=\"http://www.naturaldocs.org\">Generated by Natural Docs</a>" +

					"</div>" +
				"</div>"

				);

			BuildFile(config.Folder + "/index.html", rawPageTitle, content.ToString(), PageType.Index);
			}



		// Group: File Functions
		// __________________________________________________________________________


		/* Function: LoadBinaryFile
		 * Loads the information in <Output.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public static bool LoadBinaryFile (Path filename, out List<HTMLStyle> styles)
			{
			styles = new List<HTMLStyle>();

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [String: Style Path]
					// [String: Style Path]
 					// ...
 					// [String: null]

					string stylePath = binaryFile.ReadString();

					while (stylePath != null)
						{
						styles.Add( new HTMLStyle(stylePath) );
						stylePath = binaryFile.ReadString();
						}
					}
				}
			catch
				{  
				styles.Clear();
				result = false;
				}
			finally
				{  binaryFile.Dispose();  }

			return result;
			}


		/* Function: SaveBinaryFile
		 * Saves the passed information in <Output.nd>.
		 */
		public static void SaveBinaryFile (Path filename, List<HTMLStyle> styles)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [String: Style Path]
				// [String: Style Path]
 				// ...
 				// [String: null]

				foreach (HTMLStyle style in styles)
					{
					if (style.IsCSSOnly)
						{  binaryFile.WriteString(style.CSSFile);  }
					else
						{  binaryFile.WriteString(style.ConfigFile);  }
					}

				binaryFile.WriteString(null);
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________
		

		/* Function: MakeRelativeURL
		 * Creates a relative URL between the two absolute filesystem paths.  Make sure the From parameter is a *file* and not
		 * a folder.
		 */
		public string MakeRelativeURL (Path fromFile, Path toFile)
			{
			return fromFile.ParentFolder.MakeRelative(toFile).ToURL();
			}
			
			
			
		// Group: Properties
		// __________________________________________________________________________


		/* Property: Styles
		 * A list of <Styles> that apply to this builder, or null if none.
		 */
		override public IList<Style> Styles
			{
			get
				{
				// Have to do this because you can't cast directly from List<HTMLStyle> to IList<Style>.  You can
				// cast an array to IList<Style> though.
				return styles.ToArray();
				}
			}



		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: writeLock
		 * A monitor used for accessing any of the variables in this class.
		 */
		protected object writeLock;
		
		/* var: sourceFilesToRebuild
		 * A set of the source file IDs that need to be rebuilt.
		 */
		protected IDObjects.NumberSet sourceFilesToRebuild;
		
		/* var: foldersToCheckForDeletion
		 * A set of folders that have had files removed, and thus should be deleted if empty.
		 */
		protected StringSet foldersToCheckForDeletion;

		/* var: buildFlags
		 * Flags for everything that needs to be built not encompassed by other variables like <sourceFilesToRebuild>.
		 */
		protected BuildFlags buildFlags;

		/* var: config
		 */
		protected Config.Entries.HTMLOutputFolder config;

		/* var: styles
		 * A list of <Styles.HTMLStyles> that apply to this builder in the order in which they should be loaded.
		 */
		protected List<Styles.HTMLStyle> styles;



		// Group: Static Functions and Variables
		// __________________________________________________________________________


		/* var: AllPageTypes
		 * A static array of all the choices in <PageType>.
		 */
		public static PageType[] AllPageTypes = { PageType.All, PageType.Index, PageType.Content };

		/* var: AllPageTypeNames
		 * A static array of simple A-Z names with each index corresponding to those in <AllPageTypes>.
		 */
		public  static string[] AllPageTypeNames = { "All", "Index", "Content" };

		/* Function: PageTypeNameOf
		 * Translates a <PageType> into a string.
		 */
		public static string PageTypeNameOf (PageType type)
			{
			return AllPageTypeNames[(int)type];
			}

		/* Function: PageTypeOf
		 * Translates a string into a <PageType>, or returns null if there isn't a match.
		 */
		public static PageType? PageTypeOf (string typeName)
			{
			for (int i = 0; i < AllPageTypeNames.Length; i++)
				{
				if (String.Compare(typeName, AllPageTypeNames[i], true) == 0)
					{  return AllPageTypes[i];  }
				}

			return null;
			}

		}
	}

