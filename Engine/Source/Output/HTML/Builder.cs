/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Builder
 * ____________________________________________________________________________
 * 
 * An output builder for HTML.
 * 
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		> Accessor's database Lock -> accessLock
 * 
 *		Externally, this class is thread safe as functions use <accessLock> to control access to internal variables.
 *		
 *		Interally, if code needs both a database lock and <accessLock> it must acquire the database lock first.  It also 
 *		must not upgrade the database lock from read/possible write to read/write while holding <accessLock>, as there
 *		may be a thread with a read-only accessor waiting for <accessLock>.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Builder : Output.Builder, CodeDB.IChangeWatcher, Files.IChangeWatcher, SearchIndex.IChangeWatcher, IDisposable
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		public Builder (Output.Manager manager, Config.Targets.HTMLOutputFolder config) : base (manager)
			{
			accessLock = new object();

			buildState = null;
			unprocessedChanges = null;
			unitsOfWorkInProgress = 0;

			this.config = config;
			style = null;
			stylesWithInheritance = null;
			searchIndex = null;
			}


		public override bool Start (Errors.ErrorList errorList)
			{  
			int errors = errorList.Count;


			// Validate the output folder.

			if (System.IO.Directory.Exists(config.Folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.FolderDoesntExist(type, name)", "output", config.Folder) );
				return false;
				}


			// Load and validate the style.  This will also load and validate any inherited styles.

			string styleName = config.ProjectInfo.StyleName;
			Style_txt styleParser = new Style_txt();

			if (styleName == null)
				{
				style = EngineInstance.Styles.LoadStyle("Default", errorList, Config.PropertySource.SystemDefault);
				}

			else if (EngineInstance.Styles.StyleExists(styleName))
				{  
				style = EngineInstance.Styles.LoadStyle(styleName, errorList, config.ProjectInfo.StyleNamePropertyLocation);
				}

			// Check if it's an empty folder we want to generate a default Style.txt for
			else if (System.IO.Directory.Exists( EngineInstance.Config.ProjectConfigFolder + '/' + styleName ) &&
					   !System.IO.File.Exists( EngineInstance.Config.ProjectConfigFolder + '/' + styleName + "/Style.txt" ))
				{
				style = new Styles.Advanced( EngineInstance.Config.ProjectConfigFolder + '/' + styleName + "/Style.txt" );

				// Inherit Default so everything still works before it's filled out.
				style.AddInheritedStyle("Default", Config.PropertySource.SystemGenerated);

				if (!styleParser.Save((Styles.Advanced)style, errorList, false))
					{  return false;  }

				// Now we have to reload it so it loads the inherited style as well.
				style = EngineInstance.Styles.LoadStyle(styleName, errorList, config.ProjectInfo.StyleNamePropertyLocation);
				}

			else
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Style.txt.CantFindStyle(name)", styleName), 
									 config.ProjectInfo.StyleNamePropertyLocation );
				return false;
				}

			stylesWithInheritance = style.BuildInheritanceList();


			// Load Config.nd

			Config_nd binaryConfigParser = new Config_nd();
			List<Style> previousStyles;
			List<FileSourceInfo> previousFileSourceInfoList;
			bool hasBinaryConfigFile = false;
			
			if (!EngineInstance.Config.ReparseEverything)
				{
				hasBinaryConfigFile = binaryConfigParser.Load(WorkingDataFolder + "/Config.nd", out previousStyles, out previousFileSourceInfoList);
				}
			else
				{
				previousStyles = new List<Style>();
				previousFileSourceInfoList = new List<FileSourceInfo>();
				}

			
			// Load BuildState.nd

			BuildState_nd buildStateParser = new BuildState_nd();
			bool hasBinaryBuildStateFile = false;
			
			if (!EngineInstance.Config.ReparseEverything)
				{  hasBinaryBuildStateFile = buildStateParser.Load(WorkingDataFolder + "/BuildState.nd", out buildState, out unprocessedChanges);  }
			else
				{  
				buildState = new BuildState();
				unprocessedChanges = new UnprocessedChanges();
				}

			if (!hasBinaryBuildStateFile)
				{
				// Because we need source/classFilesWithContent
				EngineInstance.Config.ReparseEverything = true;

				// Because we don't know if there was anything left in sourceFilesToRebuild
				EngineInstance.Config.RebuildAllOutput = true;
				}


			// Always rebuild the scaffolding since they're quick.  If you ever make this differential, remember that FramePage depends
			// on the project name and other information.

			unprocessedChanges.AddFramePage();
			unprocessedChanges.AddMainStyleFiles();

			if (EngineInstance.Config.RebuildAllOutput)
				{
				// If the documentation is being built for the first time, these will be triggered by the changes the parser detects.
				unprocessedChanges.AddMenu();
				unprocessedChanges.AddMainSearchFiles();
				}


			// Compare to the previous list of styles.

			bool saidPurgingOutputFiles = false;

			if (!hasBinaryConfigFile || !hasBinaryBuildStateFile)
				{
				// If the binary file doesn't exist, we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.
				Start_PurgeFolder(Paths.Style.OutputFolder(this.OutputFolder), ref saidPurgingOutputFiles);
				EngineInstance.Styles.ReparseStyleFiles = true;
				}

			else // (hasBinaryFile)
				{
				// Purge the style folders of anything deleted or changed.

				foreach (var previousStyle in previousStyles)
					{
					bool stillExists = false;

					foreach (var currentStyle in stylesWithInheritance)
						{
						if (currentStyle.IsSameFundamentalStyle(previousStyle))
							{
							stillExists = true;
							break;
							}
						}

					if (stillExists == false)
						{  
						Start_PurgeFolder(Paths.Style.OutputFolder(this.OutputFolder, previousStyle.Name), ref saidPurgingOutputFiles);
						}
					}

				// Reparse styles on anything new or changed.  If a style is new we can't assume all its files are going to be
				// sent to the IChangeWatcher functions because another output target may have been using it, and thus they
				// are already in Files.Manager.

				foreach (var currentStyle in stylesWithInheritance)
					{
					bool foundMatch = false;

					foreach (var previousStyle in previousStyles)
						{
						if (previousStyle.IsSameFundamentalStyle(currentStyle))
							{
							foundMatch = true;
							break;
							}
						}

					if (foundMatch == false)
						{  
						EngineInstance.Styles.ReparseStyleFiles = true;
						break;
						}
					}
				}


			// Compare to the previous list of FileSources.

			if (!hasBinaryConfigFile || !hasBinaryBuildStateFile)
				{
				// If the binary file doesn't exist, we have to purge every folder because some of them may have changed or are no
				// longer in use and we won't know which.

				Regex.Output.HTML.SourceOrImageOutputFolder sourceOrImageOutputFolderRegex = 
					new Regex.Output.HTML.SourceOrImageOutputFolder();

				string[] outputFolders = System.IO.Directory.GetDirectories(OutputFolder);

				foreach (string outputFolder in outputFolders)
					{
					if (sourceOrImageOutputFolderRegex.IsMatch(outputFolder))
						{
						Start_PurgeFolder(outputFolder, ref saidPurgingOutputFiles);
						}
					}
				}

			else  // (hasBinaryFile)
				{
				bool hasDeletions = false;
				bool hasAdditions = false;


				// Purge the output folders of anything deleted or changed.

				foreach (FileSourceInfo previousFileSourceInfo in previousFileSourceInfoList)
					{
					bool stillExists = false;

					foreach (Files.FileSource fileSource in EngineInstance.Files.FileSources)
						{
						if (previousFileSourceInfo.IsSameFundamentalFileSource(fileSource))
							{
							stillExists = true;
							break;
							}
						}

					if (stillExists == false)
						{
						hasDeletions = true;
						Path outputFolder;
						
						if (previousFileSourceInfo.Type == InputType.Source)
							{  outputFolder = Paths.SourceFile.OutputFolder(OutputFolder, previousFileSourceInfo.Number);  }
						else
							{  
							throw new Exception("xxx"); // xxx image source
							}

						Start_PurgeFolder(outputFolder, ref saidPurgingOutputFiles);
						}
					}


				// Check if anything was added or changed.

				foreach (Files.FileSource fileSource in EngineInstance.Files.FileSources)
					{
					if (fileSource.Type == InputType.Source || fileSource.Type == InputType.Image)
						{
						bool foundMatch = false;

						foreach (FileSourceInfo previousFileSourceInfo in previousFileSourceInfoList)
							{
							if (previousFileSourceInfo.IsSameFundamentalFileSource(fileSource))
								{
								foundMatch = true;
								break;
								}
							}

						if (foundMatch == false)
							{  
							hasAdditions = true;
							break;
							}
						}
					}
					

				// If there were both additions and deletions, force a rebuild.  This covers if a FileSource was simply moved from one
				// number to another, in which case the rebuild is required to populate the new folder.  This also covers if a folder
				// FileSource is replaced by one for its parent folder, in which case a rebuild is required to recreate the output for the
				// files in the child folder.

				if (hasAdditions && hasDeletions)
					{  EngineInstance.Config.RebuildAllOutput = true;  }
				}


			// If the binary file doesn't exist, purge the rest of the output files too.

			if (!hasBinaryBuildStateFile)
				{
				Start_PurgeFolder(Paths.Class.OutputFolder(this.OutputFolder), ref saidPurgingOutputFiles);
				Start_PurgeFolder(Paths.Database.OutputFolder(this.OutputFolder),  ref saidPurgingOutputFiles);
				Start_PurgeFolder(Paths.Menu.OutputFolder(this.OutputFolder), ref saidPurgingOutputFiles);
				Start_PurgeFolder(Paths.SearchIndex.OutputFolder(this.OutputFolder), ref saidPurgingOutputFiles);

				unprocessedChanges.AddMenu();
				unprocessedChanges.AddMainSearchFiles();
				}


			// We're done with anything that could purge.

			if (saidPurgingOutputFiles)
				{  EngineInstance.EndPossiblyLongOperation();  }


			// Resave the Style.txt-based styles.

			foreach (var style in stylesWithInheritance)
				{
				if (style is Styles.Advanced)
					{
					var advancedStyle = style as Styles.Advanced;
					bool isSystemStyle = EngineInstance.Config.SystemStyleFolder.Contains(advancedStyle.ConfigFile);

					// No error on save for system styles.
					styleParser.Save(advancedStyle, errorList, noErrorOnFail: isSystemStyle);  
					}
				}


			// Save Config.nd.

			if (!System.IO.Directory.Exists(WorkingDataFolder))
				{  System.IO.Directory.CreateDirectory(WorkingDataFolder);  }

			List<FileSourceInfo> fileSourceInfoList = new List<FileSourceInfo>();

			foreach (Files.FileSource fileSource in EngineInstance.Files.FileSources)
				{
				if (fileSource.Type == Files.InputType.Source || fileSource.Type == Files.InputType.Image)
					{
					FileSourceInfo fileSourceInfo = new FileSourceInfo();
					fileSourceInfo.CopyFrom(fileSource);
					fileSourceInfoList.Add(fileSourceInfo);
					};
				}

			binaryConfigParser.Save(WorkingDataFolder + "/Config.nd", stylesWithInheritance, fileSourceInfoList);


			// Create the search index and watch other modules

			searchIndex = new SearchIndex.Manager(this);

			EngineInstance.CodeDB.AddChangeWatcher(this);
			EngineInstance.Files.AddChangeWatcher(this);
			searchIndex.AddChangeWatcher(this);

			searchIndex.Start(errorList);


			return (errors == errorList.Count);
			}

			
		/* Function: Start_PurgeFolder
		 * A helper function used only by <Start()> which deletes a folder if it exists.  If it does exist and saidPurgingOutputFiles
		 * is false, it will set the status message and set it to true.
		 */
		protected void Start_PurgeFolder (Path folder, ref bool saidPurgingOutputFiles)
			{
			if (System.IO.Directory.Exists(folder))
				{  
				if (!saidPurgingOutputFiles)
					{
					EngineInstance.StartPossiblyLongOperation("PurgingOutputFiles");
					saidPurgingOutputFiles = true;
					}

				try
					{  System.IO.Directory.Delete(folder, true);  }
				catch (Exception e)
					{
					if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
						{  throw;  }
					}
				}
			}


		/* Function: Dispose
		 */
		override protected void Dispose (bool strictRulesApply)
			{
			if (!strictRulesApply)
				{
				try
					{
					if (searchIndex != null)
						{  searchIndex.Dispose();  }
					if (buildState != null)
						{
						BuildState_nd buildStateParser = new BuildState_nd();
						buildStateParser.Save(WorkingDataFolder + "/BuildState.nd", buildState, unprocessedChanges);
						}
					}
				catch 
					{  }
				}
			}


		public override void WorkOnUpdatingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			CodeDB.Accessor accessor = null;
			
			try
				{
				for (;;)
					{

					// Remember the following in the below code:
					// - unprocessedChanges is thread safe.  You don't need to hold accessLock to use it.
					// - You can't acquire or change the database lock while holding accessLock.
					// - You can't call cancelDelegate while holding accessLock.
					

					// Build frame page

					if (unprocessedChanges.PickFramePage())
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_FramePage;  }

						BuildFramePage(cancelDelegate);

						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_FramePage;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddFramePage();
							break;
							}
						else
							{  continue;  }
						}


					// Build main style files
						
					if (unprocessedChanges.PickMainStyleFiles())
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_MainStyleFiles;  }

						BuildMainStyleFiles(cancelDelegate);

						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_MainStyleFiles;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddMainStyleFiles();
							break;
							}
						else
							{  continue;  }
						}


					// Build style files
						
					int styleFileToRebuild = unprocessedChanges.PickStyleFile();

					if (styleFileToRebuild != 0)
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_StyleFile;  }
						
						BuildStyleFile(styleFileToRebuild, cancelDelegate);
						
						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_StyleFile;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddStyleFile(styleFileToRebuild);
							break;
							}		
						else
							{  continue;  }
						}
						

					// Build source files
					
					int sourceFileToRebuild = unprocessedChanges.PickSourceFile();

					if (sourceFileToRebuild != 0)
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_SourceFile;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						BuildSourceFile(sourceFileToRebuild, accessor, cancelDelegate);
						
						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_SourceFile;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddSourceFile(sourceFileToRebuild);
							break;
							}
						else
							{  continue;  }
						}
						

					// Build class files
						
					int classToRebuild = unprocessedChanges.PickClass();

					if (classToRebuild != 0)
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_ClassFile;  }
						
						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }
							
						BuildClassFile(classToRebuild, accessor, cancelDelegate);
						
						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_ClassFile;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddClass(classToRebuild);
							break;
							}		
						else
							{  continue;  }
						}
						
					else
						{  break;  }
					}
				}
			finally
				{
				if (accessor != null)
					{  accessor.Dispose();  }
				}
			}
			

		public override void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
			{
			if (cancelDelegate())
				{  return;  }

			CodeDB.Accessor accessor = null;
			
			try
				{
				for (;;)
					{
					
					// Remember the following in the below code:
					// - unprocessedChanges is thread safe.  You don't need to hold accessLock to use it.
					// - You can't acquire or change the database lock while holding accessLock.
					// - You can't call cancelDelegate while holding accessLock.


					// Delete empty folders


					List<Path> possiblyEmptyFolders = unprocessedChanges.PickPossiblyEmptyFolders();

					if (possiblyEmptyFolders != null)
						{
						// This task is not parallelizable so it gets claimed by one thread and looped to completion.  Theoretically it should
						// be, but in practice when two or more threads try to delete the same folder at the same time they both fail.  This
						// could happen if both the folder and it's parent folder are on the deletion list, so one thread gets it from the list
						// while the other thread gets it by walking up the child's tree.

						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_FolderToCheckForDeletion * possiblyEmptyFolders.Count;  }

						int folderIndex = 0;

						while (folderIndex < possiblyEmptyFolders.Count)
							{
							DeleteEmptyFolders(possiblyEmptyFolders[folderIndex]);
							folderIndex++;

							lock (accessLock)
								{  unitsOfWorkInProgress -= UnitsOfWork_FolderToCheckForDeletion;  }

							if (cancelDelegate())
								{  break;  }
							}

						// If folderIndex isn't at the end that means the cancel delegate was triggered and we have to add the remaining
						// folders back to the unprocessed changes.
						if (folderIndex < possiblyEmptyFolders.Count)
							{
							lock (accessLock)
								{  unitsOfWorkInProgress -= UnitsOfWork_FolderToCheckForDeletion * (possiblyEmptyFolders.Count - folderIndex);  }

							do
								{
								unprocessedChanges.AddPossiblyEmptyFolder(possiblyEmptyFolders[folderIndex]);
								folderIndex++;
								}
							while (folderIndex < possiblyEmptyFolders.Count);

							break;
							}
						else
							{  continue;  }
						}


					// Build the menu

					if (unprocessedChanges.PickMenu())
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_Menu;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildMenu(accessor, cancelDelegate);

						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_Menu;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddMenu();
							break;
							}
						else
							{  continue;  }
						}


					// Build the main search files

					if (unprocessedChanges.PickMainSearchFiles())
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_SearchPrefixIndex;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildSearchPrefixIndex(accessor, cancelDelegate);

						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_SearchPrefixIndex;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddMainSearchFiles();
							break;
							}
						else
							{  continue;  }
						}


					// Build the search index prefixes

					string prefixToRebuild = unprocessedChanges.PickSearchPrefix();

					if (prefixToRebuild != null)
						{
						lock (accessLock)
							{  unitsOfWorkInProgress += UnitsOfWork_KeywordDataFile;  }

						if (accessor == null)
							{  accessor = EngineInstance.CodeDB.GetAccessor();  }

						BuildSearchPrefixDataFile(prefixToRebuild, accessor, cancelDelegate);

						lock (accessLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_KeywordDataFile;  }

						if (cancelDelegate())
							{
							unprocessedChanges.AddSearchPrefix(prefixToRebuild);
							break;
							}
						else
							{  continue;  }
						}


					else
						{  break;  }
					}
				}
			finally
				{
				if (accessor != null)
					{  accessor.Dispose();  }
				}
			}
			

		override public long UnitsOfWorkRemaining ()
			{
			long value = 0;

			lock (accessLock)
				{
				unprocessedChanges.GetStatus(out value);
				value += unitsOfWorkInProgress;
				}

			return value;
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

					// We're stuck in Transitional while we use iframes, which are deprecated in Strict.  HTML5 will supposedly bring
					// iframes back.
					"<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">" +
					"\r\n\r\n" +

					"<html>" +
						"<head>" +

							"<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />" +

							"<title>" + pageTitle.ToHTML() + "</title>" +

							"<link rel=\"stylesheet\" type=\"text/css\" href=\"" +
								MakeRelativeURL(outputPath, Paths.Style.OutputFolder(this.OutputFolder) + "/main.css") +
								"\" />");

							string pageTypeName = PageTypes.NameOf(pageType);
							string jsRelativePrefix = MakeRelativeURL(outputPath, Paths.Style.OutputFolder(this.OutputFolder)) + '/';

							file.Write(
							"<script type=\"text/javascript\" src=\"" + jsRelativePrefix + "main.js\"></script>" +
							"<script type=\"text/javascript\">" +
								"NDLoader.LoadJS(\"" + pageTypeName + "\", \"" + jsRelativePrefix + "\");" +
							"</script>" +

						"</head>" + 

							"\r\n\r\n" +
							"<!-- Generated by Natural Docs, version " + Instance.VersionString + " -->" +
							"\r\n\r\n" +

							// The IE mark of the web.  Without it Internet Explorer will pop up messages and possibly block JavaScript 
							// from running from the local drive.  Note that it MUST have at least one \r\n after it or it won't work.
							// Microsoft Edge doesn't need it, but if it has it, it must be set to http://localhost or else it will block pages
							// from loading external JavaScript files, possibly because it sees it as a cross domain request.
							"<!-- saved from url=(0016)http://localhost -->" +
							"\r\n\r\n" +

						"<body onload=\"NDLoader.OnLoad('" + pageTypeName + "');\" " +
									 "class=\"NDPage ND" + pageTypeName + "Page\">" +

							pageContentHTML +
								
						"</body>" +
					"</html>");
				}
			}


		/* Function: BuildFramePage
		 * Builds index.html, which provides the documentation frame.
		 */
		protected void BuildFramePage (CancelDelegate cancelDelegate)
			{

			// Page and header titles

			string rawPageTitle;
			string headerTitleHTML;
			string headerSubtitleHTML;

			if (config.ProjectInfo.Title == null)
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.DefaultPageTitle");
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHeaderTitle").ToHTML();
				headerSubtitleHTML = null;
				}
			else
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.PageTitle(projectTitle)", config.ProjectInfo.Title);
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderTitle(projectTitle)", config.ProjectInfo.Title).ToHTML();

				if (config.ProjectInfo.Subtitle == null)
					{  headerSubtitleHTML = null;  }
				else
					{
					headerSubtitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderSubtitle(projectSubtitle)",
																				 config.ProjectInfo.Subtitle).ToHTML();
					}
				}


			// Footer

			string timestampHTML = config.ProjectInfo.MakeTimestamp();
			string copyrightHTML = config.ProjectInfo.Copyright;

			if (timestampHTML != null)
				{  timestampHTML = timestampHTML.ToHTML();  }
			if (copyrightHTML != null)
				{  copyrightHTML = copyrightHTML.ToHTML();  }


			// index.html, the main frame page

			StringBuilder content = new StringBuilder();

			content.Append(

				"<div id=\"NDMessages\">" +
					"<a href=\"javascript:NDFramePage.CloseMessages()\" id=\"MsgCloseButton\">" +
						Locale.Get("NaturalDocs.Engine", "HTML.Close").ToHTML() +
					"</a>" +
					"<div id=\"MsgContent\"></div>" +
				"</div>" +

				"<div id=\"NDHeader\">" +
					"<div id=\"HTitle\">" +
					
						"<a href=\"#\">" +
							headerTitleHTML +
						"</a>" +
					
					"</div>");

					if (headerSubtitleHTML != null)
						{  
						content.Append(
							"<div id=\"HSubtitle\">" +
								"<a href=\"#\">" +
									headerSubtitleHTML +
								"</a>" +
							"</div>");  
						}

				content.Append(
				"<input id=\"NDSearchField\" type=\"text\" />"+

				"</div>" +

				"<script type=\"text/javascript\">" +
					// The backslash on /div is necessary to validate even though it's logically not necessary.  See here:
					// http://www.htmlhelp.com/tools/validator/problems.html#script
					"document.write(\"<div id=\\\"NDLoadingNotice\\\"><\\/div>\");" +
				"</script>" +
				"<noscript>" +
					"<div id=\"NDJavaScriptRequiredNotice\">" +
						Locale.Get("NaturalDocs.Engine", "HTML.JavaScriptRequiredNotice").ToHTML() +
					"</div>" +
				"</noscript>" +

				"<div id=\"NDMenu\"></div>" +
				"<div id=\"NDMenuSizer\"></div>" +

				"<div id=\"NDSummary\"></div>" +
				"<div id=\"NDSummarySizer\"></div>" +

				"<div id=\"NDContent\">" +

					// We can theoretically replace this with an object tag which will let us go back to HTML Strict, but it's more trouble
					// than it's worth.  You'll still need to fall back to iframes for IE8 and below, and WebKit has issues with using
					// location.replace() from the local drive.  This keeps the JavaScript simpler.
					"<iframe id=\"CFrame\" frameborder=\"0\"></iframe>" +

				"</div>" +

				"<div id=\"NDFooter\">");

					if (copyrightHTML != null)
						{
						content.Append(
							"<div id=\"FCopyright\">" +
								copyrightHTML +
							"</div>");
						}

					if (timestampHTML != null)
						{
						content.Append(
							"<div id=\"FTimestamp\">" +
								timestampHTML +
							"</div>");
						}

					content.Append(
					"<div id=\"FGeneratedBy\">" +

						// Deliberately hard coded (as opposed to using Locale) so it stays consistent and we can find users of any
						// language by putting it into a search engine.  If they don't want it in their docs they can set #FGeneratedBy 
						// to display: none.
						"<a href=\"http://www.naturaldocs.org\" target=\"_blank\">Generated by Natural Docs</a>" +

					"</div>" +
				"</div>"

				);

			BuildFile(OutputFolder + "/index.html", rawPageTitle, content.ToString(), PageType.Frame);


			// other/home.html, the default welcome page

			content.Remove(0, content.Length);

			string titleHTML, subtitleHTML;

			if (config.ProjectInfo.Title != null)
				{
				titleHTML = headerTitleHTML;

				if (config.ProjectInfo.Subtitle != null)
					{  subtitleHTML = headerSubtitleHTML;  }
				else
					{  subtitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeSubtitleIfTitleExists").ToHTML();  }
				}
			else
				{
				titleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeTitle").ToHTML();
				subtitleHTML = null;
				}

			content.Append(
				"\r\n\r\n" +
				"<div class=\"HFrame\">" +
					"<div class=\"HContent\">" + 
						"<div class=\"HTitle\">" +
							titleHTML + 
						"</div>");

						if (subtitleHTML != null)
							{
							content.Append(
								"<div class=\"HSubtitle\">" +
									subtitleHTML + 
								"</div>");
							}


					content.Append(
						"\r\n\r\n" +
						"<div class=\"HFooter\">");

						if (copyrightHTML != null)
							{
							content.Append(
								"<div class=\"HCopyright\">" +
									copyrightHTML +
								"</div>");
							}

						if (timestampHTML != null)
							{
							content.Append(
								"<div class=\"HTimestamp\">" +
									timestampHTML +
								"</div>");
							}

						content.Append(
						"<div class=\"HGeneratedBy\">" +
							"<a href=\"http://www.naturaldocs.org\" target=\"_blank\">Generated by Natural Docs</a>" +
						"</div>" +

					"</div>" + 
				"</div>" + 
			"</div>");

			BuildFile(OutputFolder + "/other/home.html", rawPageTitle, content.ToString(), PageType.Home);
			}


		/* Function: DeleteEmptyFolders
		 * Deletes the passed folder if it's empty.  If so it also tries the parent folder, continuing up the tree until it finds a
		 * non-empty folder or reaches the root output folder.
		 */
		protected void DeleteEmptyFolders (Path folder)
			{
			while (OutputFolder.Contains(folder))
				{
				try
					{
					// If the folder isn't empty this will throw an exception.  We have to rely on that because .NET doesn't otherwise
					// provide an efficient way to detect if a folder is empty.  All its functions enumerate all its contents and return it
					// as an array; there's nothing that returns the first item and lets you stop there.
					System.IO.Directory.Delete(folder);
					}
				catch (Exception e)
					{
					if (e is System.IO.IOException || e is System.IO.DirectoryNotFoundException)
						{  break;  }
					else
						{  throw;  }
					}

				folder = folder.ParentFolder;
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________
		

		/* Property: OutputFolder
		 * The root output folder of the entire build target.
		 */
		public Path OutputFolder
			{
			get
				{  return config.Folder;  }
			}

		/* Property: WorkingDataFolder
		 * The working data folder specifically for this build target.
		 */
		public Path WorkingDataFolder
			{
			get
				{  return EngineInstance.Config.OutputWorkingDataFolderOf(config.Number);  }
			}


		/* Function: MakeRelativeURL
		 * Creates a relative URL between the two absolute filesystem paths.  Make sure the From parameter is a *file* and not
		 * a folder.
		 */
		protected string MakeRelativeURL (Path fromFile, Path toFile)
			{
			return toFile.MakeRelativeTo(fromFile.ParentFolder).ToURL();
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: Style
		 * The <Style> that applies to this builder, or null if none.
		 */
		override public Style Style
			{
			get
				{
				return style;
				}
			}


		/* Property: SearchIndex
		 * The <SearchIndex.Manager> associated with this build target.
		 */
		public SearchIndex.Manager SearchIndex
			{
			get
				{  return searchIndex;  }
			}



		// Group: Constants
		// __________________________________________________________________________


		/* Constants: UnitsOfWork Constants
		 * 
		 * The values for each task when calculating <UnitsOfWorkRemaining()>.
		 * 
		 *		UnitsOfWork_SourceFile - How much building a single source file costs.
		 *		UnitsOfWork_ClassFile - How much building a single class file costs.
		 *		UnitsOfWork_StyleFile - How much building a single style file costs.
		 *		UnitsOfWork_FramePage - How much building index.html costs.
		 *		UnitsOfWork_MainStyleFiles - How much building main.css and main.js costs.
		 *		UnitsOfWork_Menu - How much building the menu costs.
		 *		UnitsOfWork_SearchPrefixIndex - How much building the prefix index file costs.
		 *		UnitsOfWork_KeywordDataFile - How much building a single keyword data file costs.
		 *		UnitsOfWork_FolderToCheckForDeletion - How much checking a single folder for deletion costs.
		 */
		protected const long UnitsOfWork_SourceFile = 10;
		protected const long UnitsOfWork_ClassFile = 10;
		protected const long UnitsOfWork_StyleFile = 4;
		protected const long UnitsOfWork_FramePage = 1;
		protected const long UnitsOfWork_MainStyleFiles = 1;
		protected const long UnitsOfWork_Menu = 15;
		protected const long UnitsOfWork_SearchPrefixIndex = 1;
		protected const long UnitsOfWork_KeywordDataFile = 2;
		protected const long UnitsOfWork_FolderToCheckForDeletion = 1;



		// Group: Variables
		// __________________________________________________________________________
		
		
		/* var: accessLock
		 * A monitor used for accessing any of the variables in this class.
		 */
		protected object accessLock;
		
		/* var: buildState
		 * The current build state for the HTML target.
		 */
		protected BuildState buildState;

		/* var: unprocessedChanges
		 */
		protected UnprocessedChanges unprocessedChanges;

		/* var: unitsOfWorkInProgress
		 * 
		 * A running total of the units of work of tasks that are currently in progress.  This should be incremented by one of
		 * the <UnitsOfWork constants> whenever a task is claimed and decremented when it is released.
		 * 
		 * Why is this necessary?  When calculating a total for <UnitsOfWorkRemaining()> we can only detect unclaimed
		 * tasks from variables like <sourceFilesToRebuild> as each source file will be removed from that list as soon as a 
		 * thread starts working on it.  This is necessary to prevent another thread from claiming the same file.  We want to
		 * keep it in <UnitsOfWorkRemaining()> until it's completed so we maintain this variable to add to it.
		 */
		protected long unitsOfWorkInProgress;

		/* var: config
		 */
		protected Config.Targets.HTMLOutputFolder config;

		/* var: style
		 * The <Style> which applies to this output target.
		 */
		protected Style style;

		/* var: stylesWithInheritance
		 * A list which includes <style> and all its inherited members in the order in which they should be applied.
		 */
		protected List<Style> stylesWithInheritance;

		/* var: searchIndex
		 * The <SearchIndex.Manager> for this output target.
		 */
		protected SearchIndex.Manager searchIndex;

		}


	public struct FileSourceInfo
		{
		public bool IsSameFundamentalFileSource (Files.FileSource other)
			{
			return (Number == other.Number &&
						 Type == other.Type &&
						 UniqueIDString == other.UniqueIDString);
			}

		public void CopyFrom (Files.FileSource other)
			{
			Number = other.Number;
			Type = other.Type;
			UniqueIDString = other.UniqueIDString;
			}

		public int Number;
		public Files.InputType Type;
		public string UniqueIDString;
		}
	}

