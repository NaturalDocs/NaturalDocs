/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Builder Functions
		// __________________________________________________________________________


		/* Function: BuildMainStyleFiles
		 * Builds main.css and main.js.
		 */
		protected void BuildMainStyleFiles (CancelDelegate cancelDelegate)
			{
			// Creates all subdirectories needed.  Does nothing if it already exists.
			System.IO.Directory.CreateDirectory(Output.HTML.Paths.Style.OutputFolder(this.OutputFolder));


			// main.css

			// There's nothing to condense so just write it directly to a file.
			using (System.IO.StreamWriter mainCSSFile = 
						System.IO.File.CreateText(Output.HTML.Paths.Style.OutputFolder(this.OutputFolder) + "/main.css"))
				{
				foreach (var style in stylesWithInheritance)
					{
					if (style.Links != null)
						{
						foreach (var link in style.Links)
							{
							// We don't care about filters for CSS files.
							if (link.File.Extension.ToLower() == "css")
								{
								Path relativeLinkPath = style.MakeRelative(link.File);
								Path outputPath = Output.HTML.Paths.Style.OutputFile(this.OutputFolder, style.Name, relativeLinkPath);
								Path relativeOutputPath = outputPath.MakeRelativeTo(Output.HTML.Paths.Style.OutputFolder(this.OutputFolder));
								mainCSSFile.Write("@import URL(\"" + relativeOutputPath.ToURL() + "\");");
								}
							}
						}
					}
				}


			// main.js

			StringBuilder[] jsLinks = new StringBuilder[ Output.HTML.PageTypeUtilities.AllPageTypes.Length ];
			StringBuilder[] jsOnLoads = new StringBuilder[ Output.HTML.PageTypeUtilities.AllPageTypes.Length ];

			foreach (var style in stylesWithInheritance)
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

							Path relativeLinkPath = style.MakeRelative(link.File);
							Path outputPath = Output.HTML.Paths.Style.OutputFile(this.OutputFolder, style.Name, relativeLinkPath);
							Path relativeOutputPath = outputPath.MakeRelativeTo(Output.HTML.Paths.Style.OutputFolder(this.OutputFolder));
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
				
			for (int i = 0; i < Output.HTML.PageTypeUtilities.AllPageTypes.Length; i++)
				{
				jsOutput.Append("   this.JSLinks_" + Output.HTML.PageTypeUtilities.AllPageTypeNames[i] + " = [ ");

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


			for (int i = 0; i < Output.HTML.PageTypeUtilities.AllPageTypes.Length; i++)
				{
				jsOutput.Append(
				"\n" +
				"   this.OnLoad_" + Output.HTML.PageTypeUtilities.AllPageTypeNames[i] + " = function ()\n" +
				"      {\n");

				if (jsOnLoads[i] != null)
					{  jsOutput.Append( jsOnLoads[i].ToString() );  }

				jsOutput.Append(
				"      };\n");
				}

			jsOutput.Append(
				"   };\n");

			string jsOutputString = jsOutput.ToString();

			if (EngineInstance.Config.ShrinkFiles)
				{
				ResourceProcessors.JavaScript jsProcessor = new ResourceProcessors.JavaScript();
				jsOutputString = jsProcessor.Process(jsOutputString, true);
				}

			System.IO.File.WriteAllText(Output.HTML.Paths.Style.OutputFolder(this.OutputFolder) + "/main.js", jsOutputString);
			}


		protected void BuildStyleFile (int fileID, CancelDelegate cancelled)
			{
			File file = EngineInstance.Files.FromID(fileID);
			Path outputFile = null;
			
			foreach (var style in stylesWithInheritance)
				{
				if (style.Contains(file.FileName))
					{
					Path relativeStylePath = style.MakeRelative(file.FileName);
					outputFile = Output.HTML.Paths.Style.OutputFile(this.OutputFolder, style.Name, relativeStylePath);

					break;
					}
				}

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

