/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
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
 *		> Frame OnLoad: [code]
 *		> Content OnLoad: [code]
 *		
 *		Specifies a single line of JavaScript code that will be executed from the page's OnLoad function.  Can be restricted
 *		to certain page types or applied to all of them.  If you have a non-trivial amount of code to run you should define 
 *		a function to be called from here instead.
 *		
 *		> Link: [file]
 *		> Frame Link: [file]
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

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Output.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
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
				if (System.IO.File.Exists( EngineInstance.Config.ProjectConfigFolder + "/" + name + ".HTML/Style.txt" ))
					{  return EngineInstance.Config.ProjectConfigFolder + "/" + name + ".HTML/Style.txt";  }
				else if (System.IO.File.Exists( EngineInstance.Config.ProjectConfigFolder + "/" + name + "/Style.txt" ))
					{  return EngineInstance.Config.ProjectConfigFolder + "/" + name + "/Style.txt";  }
				else if (System.IO.File.Exists( EngineInstance.Config.ProjectConfigFolder + "/" + name + ".css" ))
					{  return EngineInstance.Config.ProjectConfigFolder + "/" + name + ".css";  }
				}

			if (System.IO.File.Exists( EngineInstance.Config.SystemStyleFolder + "/" + name + ".HTML/Style.txt" ))
				{  return EngineInstance.Config.SystemStyleFolder + "/" + name + ".HTML/Style.txt";  }
			else if (System.IO.File.Exists( EngineInstance.Config.SystemStyleFolder + "/" + name + "/Style.txt" ))
				{  return EngineInstance.Config.SystemStyleFolder + "/" + name + "/Style.txt";  }
			else if (System.IO.File.Exists( EngineInstance.Config.SystemStyleFolder + "/" + name + ".css" ))
				{  return EngineInstance.Config.SystemStyleFolder + "/" + name + ".css";  }

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
			// Creates all subdirectories needed.  Does nothing if it already exists.
			System.IO.Directory.CreateDirectory(Styles_OutputFolder());


			// main.css

			// There's nothing to condense so just write it directly to a file.
			using (System.IO.StreamWriter mainCSSFile = System.IO.File.CreateText(Styles_OutputFolder() + "/main.css"))
				{
				foreach (HTMLStyle style in styles)
					{
					if (style.IsCSSOnly)
						{
						Path outputPath = Styles_OutputFile(style.CSSFile);
						Path relativeOutputPath = outputPath.MakeRelativeTo(Styles_OutputFolder());
						mainCSSFile.Write("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
						}
					else if (style.Links != null)
						{
						foreach (HTMLStyleFileLink link in style.Links)
							{
							// We don't care about filters for CSS files.
							if (link.File.Extension.ToLower() == "css")
								{
								Path outputPath = Styles_OutputFile(style.Folder + '/' + link.File);
								Path relativeOutputPath = outputPath.MakeRelativeTo(Styles_OutputFolder());
								mainCSSFile.Write("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
								}
							}
						}
					}
				}


			// main.js

			StringBuilder[] jsLinks = new StringBuilder[ AllPageTypes.Length ];
			StringBuilder[] jsOnLoads = new StringBuilder[ AllPageTypes.Length ];

			foreach (HTMLStyle style in styles)
				{
				if (style.Links != null)
					{
					foreach (var link in style.Links)
						{
						string extension = link.File.Extension.ToLower();

						if (extension == "js" || extension == "json")
							{
							if (jsLinks[(int)link.Type] == null)
								{  jsLinks[(int)link.Type] = new StringBuilder();  }
							else
								{  jsLinks[(int)link.Type].Append(", ");  }

							Path outputPath = Styles_OutputFile(style.Folder + "/" + link.File);
							Path relativeOutputPath = outputPath.MakeRelativeTo(Styles_OutputFolder());
							jsLinks[(int)link.Type].Append("\"" + relativeOutputPath.ToURL() + "\"");
							}
						}
					}

				if (style.OnLoad != null)
					{
					foreach (var onLoadStatement in style.OnLoad)
						{
						StringBuilder onLoadStatementsForType = jsOnLoads[(int)onLoadStatement.Type];

						if (onLoadStatementsForType == null)
							{  
							onLoadStatementsForType = new StringBuilder();
							jsOnLoads[(int)onLoadStatement.Type] = onLoadStatementsForType;  
							}

						onLoadStatementsForType.Append("      ");
						onLoadStatementsForType.Append(onLoadStatement.Statement);

						if (onLoadStatementsForType[ onLoadStatementsForType.Length - 1 ] != ';')
							{  onLoadStatementsForType.Append(';');  }

						onLoadStatementsForType.Append('\n');
						}
					}
				}

			StringBuilder jsOutput = new System.Text.StringBuilder(
				"\"use strict\";\n" +
				"\n" +
				"var NDLoader = new function ()\n" +
				"   {\n");
				
			for (int i = 0; i < AllPageTypes.Length; i++)
				{
				jsOutput.Append("   this.JSLinks_" + AllPageTypeNames[i] + " = [ ");

				if (jsLinks[i] != null)
					{  jsOutput.Append( jsLinks[i].ToString() );  }

				jsOutput.Append(" ];\n");
				}


			jsOutput.Append(
				"\n" +
				"   this.LoadJS = function (pageType, relativePrefix)\n" +
				"      {\n" +
				"      this.LoadJSArray(this.JSLinks_All, relativePrefix);\n" +
				"      this.LoadJSArray(this['JSLinks_' + pageType], relativePrefix);\n" +
				"      };\n" +
				"\n" +
				"   this.LoadJSArray = function (links, relativePrefix)\n" +
				"      {\n" +

					// WebKit, and I'm guessing KHTML just to be safe, doesn't import scripts included the other way in time
					// for their functions to be accessible to body.OnLoad().

				"      if (navigator.userAgent.indexOf('KHTML') != -1)\n" +
				"         {\n" +
				"         for (var i = 0; i < links.length; i++)\n" +
				"            {\n" +
				"            document.write('<script type=\"text/javascript\" src=\"' + relativePrefix + links[i] + '\"></script>');\n" +
				"            }\n" +
				"         }\n" +

					// The proper way.

				"      else\n" +
				"         {\n" +
				"         var head = document.getElementsByTagName('head')[0];\n" +
				"         \n" +
				"         for (var i = 0; i < links.length; i++)\n" +
				"            {\n" +
				"            var script = document.createElement('script');\n" +
				"            script.src = relativePrefix + links[i];\n" +
				"            script.type = 'text/javascript';\n" +
				"            \n" +
				"            head.appendChild(script);\n" +
				"            }\n" +
				"         }\n" +
				"      };\n" +
				"\n" +
				"   this.OnLoad = function (pageType)\n" +
				"      {\n" +
				"      this.OnLoad_All();\n" +
				"      this['OnLoad_' + pageType]();\n" +
				"      };\n");


			for (int i = 0; i < AllPageTypes.Length; i++)
				{
				jsOutput.Append(
				"\n" +
				"   this.OnLoad_" + AllPageTypeNames[i] + " = function ()\n" +
				"      {\n");

				if (jsOnLoads[i] != null)
					{  jsOutput.Append( jsOnLoads[i].ToString() );  }

				jsOutput.Append(
				"      };\n");
				}

			jsOutput.Append(
				"   };\n");

			string jsOutputString = jsOutput.ToString();

			ResourceProcessors.JavaScript jsProcessor = new ResourceProcessors.JavaScript();
			jsOutputString = jsProcessor.Process(jsOutputString, EngineInstance.Config.ShrinkFiles);

			System.IO.File.WriteAllText(Styles_OutputFolder() + "/main.js", jsOutputString);
			}


		protected void BuildStyleFile (int fileID, CancelDelegate cancelled)
			{
			File file = EngineInstance.Files.FromID(fileID);

			// Will return null if the file isn't used by this builder
			Path outputFile = Styles_OutputFile(file.FileName);

			if (outputFile == null)
				{  return;  }

			if (file.Deleted)
				{
				if (System.IO.File.Exists(outputFile))
					{  
					System.IO.File.Delete(outputFile);

					lock (accessLock)
						{  buildState.FoldersToCheckForDeletion.Add(outputFile.ParentFolder);  }
					}
				}

			else // file new or changed
				{
				// Creates all subdirectories needed.  Does nothing if it already exists.
				System.IO.Directory.CreateDirectory(outputFile.ParentFolder);

				string extension = outputFile.Extension.ToLower();

				if (extension == "js" || extension == "json")
					{
					ResourceProcessors.JavaScript jsProcessor = new ResourceProcessors.JavaScript();
					string output = jsProcessor.Process(System.IO.File.ReadAllText(file.FileName), EngineInstance.Config.ShrinkFiles);
					System.IO.File.WriteAllText(outputFile, output);
					}
				else if (extension == "css")
					{
					ResourceProcessors.CSS cssProcessor = new ResourceProcessors.CSS();
					string output = cssProcessor.Process(System.IO.File.ReadAllText(file.FileName), EngineInstance.Config.ShrinkFiles);
					System.IO.File.WriteAllText(outputFile, output);
					}
				else
					{
					System.IO.File.Copy(file.FileName, outputFile, true);  
					}
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Function: Styles_OutputFolder
		 * Returns the folder for the passed style, or if null, the root output folder for all styles.
		 */
		protected Path Styles_OutputFolder (HTMLStyle style = null)
			{
			StringBuilder result = new StringBuilder(OutputFolder);
			result.Append("/styles");

			if (style != null)
				{  
				result.Append('/');
				result.Append(SanitizePath(style.Name));
				}
			
			return result.ToString();
			}

		/* Function: Styles_OutputFile
		 * Returns the output path of the passed style file if it is part of a style used by this builder.  Otherwise returns null.
		 */
		protected Path Styles_OutputFile (Path originalStyleFile)
			{
			foreach (HTMLStyle style in styles)
				{
				if (style.Contains(originalStyleFile))
					{
					Path relativeStyleFile = style.MakeRelative(originalStyleFile);
					return OutputFolder + "/styles/" + SanitizePath(style.Name) + "/" + SanitizePath(relativeStyleFile);
					}
				}

			return null;
			}



		// Group: Files.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddFile (File file)
			{
			lock (accessLock)
				{  buildState.StyleFilesToRebuild.Add(file.ID);  }
			}

		public void OnFileChanged (File file)
			{
			lock (accessLock)
				{  buildState.StyleFilesToRebuild.Add(file.ID);  }
			}

		public void OnDeleteFile (File file)
			{
			lock (accessLock)
				{  buildState.StyleFilesToRebuild.Add(file.ID);  }
			}

		}
	}

