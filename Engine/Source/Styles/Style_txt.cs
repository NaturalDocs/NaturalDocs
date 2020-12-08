/* 
 * Class: CodeClear.NaturalDocs.Engine.Styles.Style_txt
 * ____________________________________________________________________________
 * 
 * A class to handle loading and saving <Style.txt>.
 * 
 * 
 * Threading: Not Thread Safe
 * 
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 * 
 * 
 * File: Style.txt
 * 
 *		A configuration file for an advanced style in Natural Docs.
 *		
 *		> Inherit: [style]
 *		
 *		States that this style inherits the specified style, meaning the inherited style's files will be included in the
 *		output first, then this style's files.  Can be specified multiple times.
 *		
 *		
 *		> OnLoad: [code]
 *		> Frame OnLoad: [code]
 *		> Content OnLoad: [code]
 *		> Home OnLoad: [code]
 *		
 *		Specifies a single line of JavaScript code that will be executed from the page's OnLoad function.  Can be restricted
 *		to certain page types or applied to all of them.  If you have a non-trivial amount of code to run you should define 
 *		a function to be called from here instead.
 *		
 *		
 *		> Link: [file]
 *		> Frame Link: [file]
 *		> Content Link: [file]
 *		> Home Link: [file]
 *		
 *		Specifies a .css, .js, or .json file that should be included in the page output, such as with a script or link tag.  
 *		JavaScript files can be restricted to certain page types or linked to all of them.  CSS files can only be linked to all
 *		of them.  The file path is relative to the style's folder.
 *		
 *		All files found in the style's folder are not automatically included because some may be intended to be loaded 
 *		dynamically, or the .css files may already be linked together with @import.
 *		
 *		
 *		> Home Page: [file]
 *		
 *		Specifies a .html file to serve as the home page.  If it's defined none of the OnLoad or Link properties will have any 
 *		effect on it.  Equivalent properties must be manually added to the HTML file if they're needed.  If a home page isn't
 *		defined then default one will be generated.
 *		
 *		The home page HTML file may contain these values which will be replaced by their corresponding properties in the
 *		project info:
 *		
 *		- %NaturalDocs_Title%
 *		- %NaturalDocs_Subtitle%
 *		- %NaturalDocs_Copyright%
 *		- %NaturalDocs_Timestamp%
 *		
 *		Entity subsitutions will be made for <, >, and " so they are safe to embed directly in the HTML or in double-quoted
 *		JavaScript strings.
 *		
 *		Revision History:
 *		
 *			- 2.2
 *				- Added the Home Page property.
 *				
 *			- 2.1
 *				- Added Home as an option for OnLoad and Link statements.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public class Style_txt
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: Style_txt
		 */
		public Style_txt ()
			{
			inheritRegex = new Regex.Styles.Inherit();
			linkRegex = new Regex.Styles.Link();
			onLoadRegex = new Regex.Styles.OnLoad();
			homePageRegex = new Regex.Styles.HomePage();
			}


		/* Function: Load
		 * Loads <Style.txt> and returns it as a <Styles.Advanced> object.  If there are any errors found in <Style.txt> they will be added 
		 * to the list and the function will return null.
		 */
		public Styles.Advanced Load (Path path, Errors.ErrorList errors)
			{
			Styles.Advanced style = new Styles.Advanced(path);

			using (ConfigFile file = new ConfigFile())
				{
				if (!file.Open(path,
								   Config.PropertySource.StyleConfigurationFile,
								   ConfigFile.FileFormatFlags.MakeIdentifiersLowercase |
								   ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace, errors))
					{  return null;  }

				int errorCount = errors.Count;
				string lcIdentifier, value;
				System.Text.RegularExpressions.Match match;

				while (file.Get(out lcIdentifier, out value))
					{

					// Inherit

					if (inheritRegex.IsMatch(lcIdentifier))
						{  
						style.AddInheritedStyle(value, file.PropertyLocation, null);
						continue;
						}


					// Link

					match = linkRegex.Match(lcIdentifier);
					if (match.Success)
						{  
						PageType pageType = PageType.All;
						if (match.Groups[1].Success)
							{
							PageType? pageTypeTemp = PageTypes.FromName(match.Groups[1].ToString());

							if (pageTypeTemp == null)
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );  }
							else
								{  pageType = pageTypeTemp.Value;  }
							}

						Path linkedFile = value;

						if (!Styles.Manager.LinkableFileExtensions.Contains(linkedFile.Extension))
							{  
							file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantLinkFileWithExtension(extension)",
																 linkedFile.Extension) );  
							}
						else if (linkedFile.Extension.ToLower() == "css" && pageType != PageType.All)
							{
							file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantLinkCSSFileToSpecificPageTypes(pageType)", 
																 PageTypes.NameOf(pageType)) );
							}
						else
							{
							Path fullLinkedFile = style.Folder + "/" + linkedFile;

							if (!System.IO.File.Exists(fullLinkedFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindLinkedFile(name)", fullLinkedFile) );  }
							else
								{  style.AddLinkedFile(fullLinkedFile, file.PropertyLocation, pageType);  }
							}

						continue;
						}


					// OnLoad

					match = onLoadRegex.Match(lcIdentifier);
					if (match.Success)
						{  
						PageType pageType = PageType.All;
						if (match.Groups[1].Success)
							{
							PageType? pageTypeTemp = PageTypes.FromName(match.Groups[1].ToString());

							if (pageTypeTemp == null)
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );  }
							else
								{  pageType = pageTypeTemp.Value;  }
							}

						style.AddOnLoad(value, file.PropertyLocation, pageType);
						continue;
						}


					// Home Page

					if (homePageRegex.IsMatch(lcIdentifier))
						{  
						Path homePageFile = value;
						string lcExtension = homePageFile.Extension.ToLower();

						if (lcExtension != "html" && lcExtension != "htm")
							{  
							file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.HomePageMustHaveHTMLExtension(extension)",
																 homePageFile.Extension) );  
							}
						else
							{
							Path fullHomePageFile = style.Folder + "/" + homePageFile;

							if (!System.IO.File.Exists(fullHomePageFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindHomePageFile(name)", fullHomePageFile) );  }
							else
								{  
								style.SetHomePage(fullHomePageFile, file.PropertyLocation);
								}
							}

						continue;
						}


					file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );
					}


				// Check for files or JavaScript linked to custom home pages since they wouldn't apply.

				if (style.HomePage != null)
					{
					if (style.Links != null)
						{
						foreach (var link in style.Links)
							{
							if (link.Type == PageType.Home)
								{
								errors.Add( Locale.Get("NaturalDocs.Engine", "Style.txt.CantUseHomeLinksWithCustomHomePage"), link.PropertyLocation);
								}
							}
						}

					if (style.OnLoad != null)
						{
						foreach (var onLoad in style.OnLoad)
							{
							if (onLoad.Type == PageType.Home)
								{
								errors.Add( Locale.Get("NaturalDocs.Engine", "Style.txt.CantUseHomeOnLoadWithCustomHomePage"), onLoad.PropertyLocation);
								}
							}
						}
					}


				if (errorCount == errors.Count)
					{  return style;  }
				else
					{  return null;  }
				}
			}


		/* Function: Save
		 * Saves the passed style's <Style.txt>.
		 */
		public bool Save (Styles.Advanced style, Errors.ErrorList errorList, bool noErrorOnFail)
			{
			System.Text.StringBuilder output = new System.Text.StringBuilder(512);


			// Header
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.Header.multiline") );
			output.AppendLine();
			output.AppendLine();
			

			// Inheritance
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.InheritanceHeader.multiline") );
			output.AppendLine();

			if (style.Inherits != null)
				{
				foreach (var inheritedStyle in style.Inherits)
					{
					output.Append("Inherit: ");
					output.AppendLine(inheritedStyle.Name);
					}

				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.InheritanceReference.multiline") );
			output.AppendLine();
			output.AppendLine();


			// Linked Files
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.LinkedFilesHeader.multiline") );
			output.AppendLine();

			if (style.Links != null)
				{
				foreach (var link in style.Links)
					{
					if (link.Type != PageType.All)
						{  
						output.Append(PageTypes.NameOf(link.Type));
						output.Append(' ');
						}

					output.Append("Link: ");
					output.AppendLine(style.MakeRelative(link.File));
					}

				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.LinkedFilesReference.multiline") );
			output.AppendLine();
			output.AppendLine();


			// OnLoad
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.OnLoadHeader.multiline") );
			output.AppendLine();

			if (style.OnLoad != null)
				{
				foreach (var onLoadStatement in style.OnLoad)
					{
					if (onLoadStatement.Type != PageType.All)
						{  
						output.Append(PageTypes.NameOf(onLoadStatement.Type));
						output.Append(' ');
						}

					output.Append("OnLoad: ");
					output.AppendLine(onLoadStatement.Statement);
					output.AppendLine();
					}

				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.OnLoadReference.multiline") );
			output.AppendLine();
			output.AppendLine();


			// Home Page
			
			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.HomePageHeader.multiline") );
			output.AppendLine();

			if (style.HomePage != null)
				{
				output.Append("Home Page: ");
				output.AppendLine(style.MakeRelative(style.HomePage));
				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "Style.txt.HomePageReference.multiline") );


			return ConfigFile.SaveIfDifferent(style.ConfigFile, output.ToString(), noErrorOnFail, errorList);
			}



		// Group: Regular Expressions
		// __________________________________________________________________________

		protected Regex.Styles.Inherit inheritRegex;
		protected Regex.Styles.Link linkRegex;
		protected Regex.Styles.OnLoad onLoadRegex;
		protected Regex.Styles.HomePage homePageRegex;

		}
	}

