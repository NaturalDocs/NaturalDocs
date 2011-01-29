/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
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
 *		> OnLoad: [code]
 *		> Index OnLoad: [code]
 *		> Content OnLoad: [code]
 *		
 *		Specifies a single line of JavaScript code that will be executed from the page's OnLoad function.  Can be restricted
 *		to certain page types or applied to all of them.  If you have a non-trivial amount of code to run you should define 
 *		a function to be called from here instead.
 *		
 *		> Link: [file]
 *		> Index Link: [file]
 *		> Content Link: [file]
 *		
 *		Specifies a .css, .js, or .json file that should be included in the page output, such as with a script or link tag.  
 *		JavaScript files can be restricted to certain page types or linked to all of them.  The file path is relative to the style's
 *		folder.
 *		
 *		All files found in the style's folder are not automatically included because some may be intended to be loaded 
 *		dynamically, or the .css files may already be linked together with @import.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using GregValure.NaturalDocs.Engine.Output.Styles;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: FindStyle
		 * Attempts to convert the passed style name into a style path, returning null if one isn't found.
		 * For CSS-only styles, it will be a path to the .css file in the system or project folder.  For full styles,
		 * it will be a path to <Style.txt> in the system or project folder.
		 */
		protected Path FindStyle (string name, bool systemFolderOnly = false)
			{
			if (!systemFolderOnly)
				{
				if (System.IO.File.Exists( Instance.Config.ProjectConfigFolder + "/" + name + ".HTML/Style.txt" ))
					{  return Instance.Config.ProjectConfigFolder + "/" + name + ".HTML/Style.txt";  }
				else if (System.IO.File.Exists( Instance.Config.ProjectConfigFolder + "/" + name + "/Style.txt" ))
					{  return Instance.Config.ProjectConfigFolder + "/" + name + "/Style.txt";  }
				else if (System.IO.File.Exists( Instance.Config.ProjectConfigFolder + "/" + name + ".css" ))
					{  return Instance.Config.ProjectConfigFolder + "/" + name + ".css";  }
				}

			if (System.IO.File.Exists( Instance.Config.SystemStyleFolder + "/" + name + ".HTML/Style.txt" ))
				{  return Instance.Config.SystemStyleFolder + "/" + name + ".HTML/Style.txt";  }
			else if (System.IO.File.Exists( Instance.Config.SystemStyleFolder + "/" + name + "/Style.txt" ))
				{  return Instance.Config.SystemStyleFolder + "/" + name + "/Style.txt";  }
			else if (System.IO.File.Exists( Instance.Config.SystemStyleFolder + "/" + name + ".css" ))
				{  return Instance.Config.SystemStyleFolder + "/" + name + ".css";  }

			return null;
			}


		/* Function: LoadStyle
		 * Converts a style path to a <HTMLStyle> object, loading its <Style.txt> if appropriate.  If there are any errors found 
		 * in <Style.txt> they will be added to the list and the function will return null.
		 */
		public static HTMLStyle LoadStyle (Path stylePath, Errors.ErrorList errors)
			{
			HTMLStyle style = new HTMLStyle(stylePath);

			if (style.IsCSSOnly)
				{  return style;  }

			using (ConfigFile file = new ConfigFile())
				{
				if (!file.Open(stylePath, ConfigFile.FileFormatFlags.MakeIdentifiersLowercase |
															 ConfigFile.FileFormatFlags.CondenseIdentifierWhitespace, errors))
					{  return null;  }

				int errorCount = errors.Count;
				string lcIdentifier, value;
				System.Text.RegularExpressions.Match match;

				Regex.Styles.HTML.Inherit inheritRegex = new Regex.Styles.HTML.Inherit();
				Regex.Styles.HTML.Link linkRegex = new Regex.Styles.HTML.Link();
				Regex.Styles.HTML.OnLoad onLoadRegex = new Regex.Styles.HTML.OnLoad();
				Regex.Styles.HTML.LinkableFileExtensions linkableFileExtensionsRegex = new Regex.Styles.HTML.LinkableFileExtensions();

				while (file.Get(out lcIdentifier, out value))
					{
					if (inheritRegex.IsMatch(lcIdentifier))
						{  
						style.AddInheritedStyle(value);
						continue;
						}

					match = linkRegex.Match(lcIdentifier);
					if (match.Success)
						{  
						PageType pageType = PageType.All;
						if (match.Groups[1].Success)
							{
							PageType? pageTypeTemp = PageTypeOf(match.Groups[1].ToString());

							if (pageTypeTemp == null)
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );  }
							else
								{  pageType = pageTypeTemp.Value;  }
							}

						Path linkedFile = value;

						if (linkableFileExtensionsRegex.IsMatch(linkedFile.Extension))
							{
							Path fullLinkedFile = style.Folder + "/" + linkedFile;

							if (System.IO.File.Exists(fullLinkedFile))
								{  style.AddLinkedFile(linkedFile, pageType);  }
							else
								{
								file.AddError( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.CantFindLinkedFile(name)", fullLinkedFile) );
								}
							}
						else
							{  
							file.AddError( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.CantLinkFileWithExtension(extension)",
																			 linkedFile.Extension) );  
							}

						continue;
						}

					match = onLoadRegex.Match(lcIdentifier);
					if (match.Success)
						{  
						PageType pageType = PageType.All;
						if (match.Groups[1].Success)
							{
							PageType? pageTypeTemp = PageTypeOf(match.Groups[1].ToString());

							if (pageTypeTemp == null)
								{  file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );  }
							else
								{  pageType = pageTypeTemp.Value;  }
							}

						style.AddOnLoad(value, pageType);
						continue;
						}

					file.AddError( Locale.Get("NaturalDocs.Engine", "ConfigFile.NotAValidIdentifier(identifier)", lcIdentifier) );
					}

				if (errorCount == errors.Count)
					{  return style;  }
				else
					{  return null;  }
				}
			}


		/* Function: SaveStyle
		 * Saves the passed <HTMLStyle> as <Style.txt>, provided it's not CSS-only.
		 */
		static public bool SaveStyle (HTMLStyle style, Errors.ErrorList errorList, bool noErrorOnFail)
			{
			if (style.IsCSSOnly)
				{  throw new InvalidOperationException();  }

			System.Text.StringBuilder output = new System.Text.StringBuilder(512);


			// Header
			
			output.AppendLine("Format: " + Engine.Instance.VersionString);
			output.AppendLine();
			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.Header.multiline") );
			output.AppendLine();
			output.AppendLine();
			

			// Inheritance
			
			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.InheritanceHeader.multiline") );
			output.AppendLine();

			if (style.Inherits != null)
				{
				foreach (string inheritedStyleName in style.Inherits)
					{
					output.Append("Inherit: ");
					output.AppendLine(inheritedStyleName);
					}

				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.InheritanceReference.multiline") );
			output.AppendLine();
			output.AppendLine();


			// Linked Files
			
			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.LinkedFilesHeader.multiline") );
			output.AppendLine();

			if (style.Links != null)
				{
				foreach (HTMLStyleFileLink link in style.Links)
					{
					if (link.Type != PageType.All)
						{  
						output.Append(PageTypeNameOf(link.Type));
						output.Append(' ');
						}

					output.Append("Link: ");
					output.AppendLine(link.File);
					}

				output.AppendLine();
				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.LinkedFilesReference.multiline") );
			output.AppendLine();
			output.AppendLine();


			// OnLoad
			
			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.OnLoadHeader.multiline") );
			output.AppendLine();

			if (style.OnLoad != null)
				{
				foreach (HTMLStyleOnLoadStatement onLoadStatement in style.OnLoad)
					{
					if (onLoadStatement.Type != PageType.All)
						{  
						output.Append(PageTypeNameOf(onLoadStatement.Type));
						output.Append(' ');
						}

					output.Append("OnLoad: ");
					output.AppendLine(onLoadStatement.Statement);
					output.AppendLine();
					}

				output.AppendLine();
				}

			output.Append( Locale.Get("NaturalDocs.Engine", "HTML.Style.txt.OnLoadReference.multiline") );


			return ConfigFile.SaveIfDifferent(style.ConfigFile, output.ToString(), noErrorOnFail, errorList);
			}



		// Group: Builder Functions
		// __________________________________________________________________________


		/* Function: BuildMainStyleFiles
		 * Builds main.css and main.js.
		 */
		protected void BuildMainStyleFiles (CancelDelegate cancelDelegate)
			{
			// main.css

			// There's nothing to condense so just write it directly to a file.
			using (System.IO.StreamWriter mainCSSFile = System.IO.File.CreateText(RootStyleFolder + "/main.css"))
				{
				foreach (HTMLStyle style in styles)
					{
					if (style.IsCSSOnly)
						{
						Path outputPath = StyleFileOutputPath(style.CSSFile);
						Path relativeOutputPath = RootStyleFolder.MakeRelative(outputPath);
						mainCSSFile.Write("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
						}
					else if (style.Links != null)
						{
						foreach (HTMLStyleFileLink link in style.Links)
							{
							// We don't care about filters for CSS files.
							if (link.File.Extension.ToLower() == "css")
								{
								Path outputPath = StyleFileOutputPath(style.Folder + '/' + link.File);
								Path relativeOutputPath = RootStyleFolder.MakeRelative(outputPath);
								mainCSSFile.Write("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
								}
							}
						}
					}
				}


			// main-[filter].js

			for (int filterIndex = 0; filterIndex < Builders.HTML.AllPageTypes.Length; filterIndex++)
				{
				if (cancelDelegate())
					{  return;  }

				PageType filter = Builders.HTML.AllPageTypes[filterIndex];
				string filterName = Builders.HTML.PageTypeNameOf(filter);


				System.Text.StringBuilder jsOutput = new System.Text.StringBuilder();

				jsOutput.Append("function NDLoadJS_" + filterName + " (relativePrefix) {");

				jsOutput.Append("var links = [");
				int linkCount = 0;

				foreach (HTMLStyle style in styles)
					{
					if (style.Links != null)
						{
						for (int i = 0; i < style.Links.Count; i++)
							{
							string extension = style.Links[i].File.Extension.ToLower();

							if (style.Links[i].Type == filter && (extension == "js" || extension == "json"))
								{
								if (linkCount > 0)
									{  jsOutput.Append(',');  }

								Path outputPath = StyleFileOutputPath(style.Folder + "/" + style.Links[i].File);
								Path relativeOutputPath = RootStyleFolder.MakeRelative(outputPath);
								jsOutput.Append("\"" + relativeOutputPath.ToURL() + "\"");

								linkCount++;
								}
							}
						}
					}

				jsOutput.Append("];");

				jsOutput.Append(
					// WebKit, and I'm guessing KHTML just to be safe, doesn't import scripts included the other way in time
					// for their functions to be accessible to body.OnLoad().
					"if (navigator.userAgent.indexOf('KHTML') != -1)" +
						"{" +
						"for (var i = 0; i < links.length; i++)" +
							"{" +
							"document.write('<script type=\"text/javascript\" src=\"' + relativePrefix + links[i] + '\"></script>');" +
							"}" +
						"}" +

					// The proper way.
					"else" +
						"{" +
						"var head = document.getElementsByTagName('head')[0];" +

						"for (var i = 0; i < links.length; i++)" +
							"{" +
							"var script = document.createElement('script');" +
							"script.src = relativePrefix + links[i];" +
							"script.type = 'text/javascript';" +

							"head.appendChild(script);" +
							"}" +
						"}"
					);

				jsOutput.Append('}');

				if (linkCount == 0)
					{
					jsOutput.Remove(0, jsOutput.Length);
					jsOutput.Append("function NDLoadJS_" + filterName + " (relativePrefix) { }");
					}

				jsOutput.Append("function NDOnLoad_" + filterName + " () {");

				foreach (HTMLStyle style in styles)
					{
					if (style.OnLoad != null)
						{
						foreach (HTMLStyleOnLoadStatement onLoadStatement in style.OnLoad)
							{
							if (onLoadStatement.Type == filter)
								{
								jsOutput.Append(onLoadStatement.Statement);
								jsOutput.Append(';');
								}
							}
						}
					}

				jsOutput.Append('}');

				System.IO.File.WriteAllText(RootStyleFolder + "/main-" + filterName.ToLower() + ".js", Shrinker.ShrinkJS(jsOutput.ToString()));
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Property: RootStyleFolder
		 * The folder that holds all the style folders.
		 */
		protected Path RootStyleFolder
			{
			get
				{  return config.Folder + "/styles";  }
			}

		/* Function: StyleOutputFolder
		 * Returns the folder for the passed style.
		 */
		protected Path StyleOutputFolder (HTMLStyle style)
			{
			return config.Folder + "/styles/" + style.Name;
			}

		/* Function: StyleFileOutputPath
		 * Returns the output path of the passed style file if it is part of a style used by this builder.  Otherwise returns null.
		 */
		protected Path StyleFileOutputPath (Path originalStyleFile)
			{
			foreach (HTMLStyle style in styles)
				{
				if (style.Contains(originalStyleFile))
					{
					Path relativeStyleFile = style.MakeRelative(originalStyleFile);
					return config.Folder + "/styles/" + style.Name + "/" + relativeStyleFile;
					}
				}

			return null;
			}



		// Group: Files.IStyleChangeWatcher Functions
		// __________________________________________________________________________


		public Files.Manager.ReleaseClaimedFileReason OnAddOrChangeFile (Path originalStyleFile)
			{
			Path outputStyleFile = StyleFileOutputPath(originalStyleFile);

			if (outputStyleFile == null)
				{  return Files.Manager.ReleaseClaimedFileReason.SuccessfullyProcessed;  }
			else
				{
				// Creates all subdirectories needed.  Does nothing if it already exists.
				System.IO.Directory.CreateDirectory(outputStyleFile.ParentFolder);

				try
					{
					string extension = outputStyleFile.Extension.ToLower();

					if (extension == "js" || extension == "json")
						{
						string output = Shrinker.ShrinkJS(System.IO.File.ReadAllText(originalStyleFile));
						System.IO.File.WriteAllText(outputStyleFile, output);
						}
					else if (extension == "css")
						{
						string output = Shrinker.ShrinkCSS(System.IO.File.ReadAllText(originalStyleFile));
						System.IO.File.WriteAllText(outputStyleFile, output);
						}
					else
						{
						System.IO.File.Copy(originalStyleFile, outputStyleFile, true);  
						}
					}
				catch (System.IO.FileNotFoundException)
					{  return Files.Manager.ReleaseClaimedFileReason.FileDoesntExist;  }
				catch (System.IO.DirectoryNotFoundException)
					{  return Files.Manager.ReleaseClaimedFileReason.FileDoesntExist;  }
				catch (System.UnauthorizedAccessException)
					{  return Files.Manager.ReleaseClaimedFileReason.CantAccessFile;  }
				catch (System.IO.IOException)
					{  return Files.Manager.ReleaseClaimedFileReason.CantAccessFile;  }

				return Files.Manager.ReleaseClaimedFileReason.SuccessfullyProcessed;
				}
			}


		public Files.Manager.ReleaseClaimedFileReason OnDeleteFile (Path originalStyleFile)
			{
			Path outputStyleFile = StyleFileOutputPath(originalStyleFile);

			if (outputStyleFile == null || !System.IO.File.Exists(outputStyleFile))
				{  return Files.Manager.ReleaseClaimedFileReason.SuccessfullyProcessed;  }

			else
				{
				System.IO.File.Delete(outputStyleFile);  
				foldersToCheckForDeletion.Add(outputStyleFile.ParentFolder);
				return Files.Manager.ReleaseClaimedFileReason.SuccessfullyProcessed;
				}
			}

		}
	}

