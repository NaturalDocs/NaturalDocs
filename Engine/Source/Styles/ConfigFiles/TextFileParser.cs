﻿/*
 * Class: CodeClear.NaturalDocs.Engine.Styles.ConfigFiles.TextFileParser
 * ____________________________________________________________________________
 *
 * A class to handle loading and saving <Style.txt>.
 *
 *
 * Threading: Not Thread Safe
 *
 *		The parser object may be reused, but multiple threads cannot use it at the same time.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Globalization;
using System.Text.RegularExpressions;


namespace CodeClear.NaturalDocs.Engine.Styles.ConfigFiles
	{
	public partial class TextFileParser
		{

		// Group: Functions
		// __________________________________________________________________________


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
				Match match;

				while (file.Get(out lcIdentifier, out value))
					{

					// Inherit

					if (IsInheritPropertyRegexLC().IsMatch(lcIdentifier))
						{
						style.AddInheritedStyle(value, file.PropertyLocation, null);
						continue;
						}


					// Link

					match = IsLinkPropertyRegexLC().Match(lcIdentifier);
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
						else if (linkedFile.Extension.ToLower(CultureInfo.InvariantCulture) == "css" && pageType != PageType.All)
							{
							file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantLinkCSSFileToSpecificPageTypes(pageType)",
																 PageTypes.NameOf(pageType)) );
							}
						else
							{
							if (linkedFile.IsRelative)
								{  linkedFile = style.Folder + "/" + linkedFile;  }

							if (!System.IO.File.Exists(linkedFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindLinkedFile(name)", linkedFile) );  }
							else if (!style.Folder.Contains(linkedFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.LinkedFileMustBeInStyleFolder(name, folder)", linkedFile, style.Folder) );  }
							else
								{  style.AddLinkedFile(linkedFile, file.PropertyLocation, pageType);  }
							}

						continue;
						}


					// OnLoad

					match = IsOnLoadPropertyRegexLC().Match(lcIdentifier);
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

						string valueWithSubstitutions = FindLocaleSubstitutionRegex().Replace(value,
							delegate (Match match)
								{
								return Engine.Locale.SafeGet("NaturalDocs.Engine", match.Groups[1].ToString(), match.ToString());
								}
							);

						style.AddOnLoad(value, valueWithSubstitutions, file.PropertyLocation, pageType);
						continue;
						}


					// Home Page

					if (IsHomePagePropertyRegexLC().IsMatch(lcIdentifier))
						{
						Path homePageFile = value;
						string lcExtension = homePageFile.Extension.ToLower(CultureInfo.InvariantCulture);

						if (lcExtension != "html" && lcExtension != "htm")
							{
							file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.HomePageMustHaveHTMLExtension(extension)",
																 homePageFile.Extension) );
							}
						else
							{
							if (homePageFile.IsRelative)
								{  homePageFile = style.Folder + "/" + homePageFile;  }

							if (!System.IO.File.Exists(homePageFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindHomePageFile(name)", homePageFile) );  }
							else if (!style.Folder.Contains(homePageFile))
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "Style.txt.HomePageFileMustBeInStyleFolder(name, folder)", homePageFile, style.Folder) );  }
							else
								{
								style.SetHomePage((AbsolutePath)homePageFile, file.PropertyLocation);
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


		/* Regex: IsInheritPropertyRegexLC
		 * Will match if the entire string is the property name "inherit" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 */
		[GeneratedRegex("""^inherits?|(?:(?:add|inherit|import|include)s? style)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsInheritPropertyRegexLC();


		/* Regex: IsLinkPropertyRegexLC
		 *
		 * Will match if the entire string is the property name "link" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The type of link it is, either "content", "frame", "home", or none, in which case it applies to all pages.
		 */
		[GeneratedRegex("""^(?:(content|frame|home) )?(?:link|(?:link|add|inherit|include|import)s? (?:css|js|json|javascript|file)(?: file)?)$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsLinkPropertyRegexLC();


		/* Regex: IsOnLoadPropertyRegexLC
		 *
		 * Will match if the entire string is the property name "onload" or one of its acceptable variants.  Assumes the
		 * input string is already in lowercase.
		 *
		 * Capture Groups:
		 *
		 *		1 - The type of link it is, either "content", "frame", "home", or none, in which case it applies to all pages.
		 */
		[GeneratedRegex("""^(?:(content|frame|home) )?on ?load$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsOnLoadPropertyRegexLC();


		/* Regex: IsHomePagePropertyRegexLC
		 * Will match if the entire string is the property name "home page" or one of its acceptable variants.  Assumes
		 * the input string is already in lowercase.
		 */
		[GeneratedRegex("""^(?:(?:add|import|include)s? )?home ?page$""",
								  RegexOptions.Singleline | RegexOptions.CultureInvariant)]
		static private partial Regex IsHomePagePropertyRegexLC();


		/* Regex: FindLocaleSubstitutionRegex
		 *
		 * Will match instances in the string of "$Locale{[identifier]}" used to substitute values from <Locale.Manager>.
		 *
		 * Capture Groups:
		 *
		 *		1 - The locale identifier, such as "Theme.Light" in "$Locale{Theme.Light}".
		 */
		[GeneratedRegex("""[\$\@]Locale\{([^}]+)\}""",
								  RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
		static private partial Regex FindLocaleSubstitutionRegex();

		}
	}
