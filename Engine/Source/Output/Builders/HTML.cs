/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * An output builder for HTML.
 * 
 * 
 * Topic: Path Restrictions
 * 
 *		- Output file paths cannot contain the colon character as it would conflict with the URL hash format
 *		  "#File:[path]:[symbol]".
 *		- Output file paths cannot contain the hash character as it would truncate any URL based on it.
 *		- Output file paths cannot contain the semicolon or ampersand characters as not all browsers can load files with
 *		  them in the path.
 *		
 * 
 * File: Config.nd
 * 
 *		A file used to store information about the configuration as of last time this output target was built.
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
 *		> [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
 *		> [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
 *		> ...
 *		> [Int32: 0]
 *		>
 *		> [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
 *		> [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
 *		> ...
 *		> [Int32: 0]
 *		
 *		Stores all the <FileSource> IDs and what their numbers are.  This allows us to purge the related output folders if
 *		one is deleted or changes.
 *		
 * 
 * File: BuildState.nd
 * 
 *		A file used to store the build state of this output target the last time it was built.
 *		
 *		> [NumberSet: Source File IDs to Rebuild]
 *		
 *		The source files that needed to be rebuilt but weren't yet.  If the last build was run to completion this should be
 *		an empty set, though if the build was interrupted this will have the ones left to do.
 *		
 *		> [NumberSet: Source File IDs with Content]
 *		
 *		A set of all the source files known to have content after all filters were applied.
 *		
 *		> [StringSet: Folders to Check for Deletion]
 *		
 *		A set of all folders which have had files removed and thus should be removed if empty.  If the last build was run
 *		to completion this should be an empty set.
 * 
 *		> [NumberSet: File Menu Root Folder IDs]
 *		
 *		The IDs used building the file menu.  This allows us to clean up old JS data files if we're using less than before.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// We must include this so that "FileSource" doesn't get accidentally interpreted as Output.Styles.FileSource
// instead of Files.FileSource.  Including them both makes "FileSource" ambiguous and the compiler forces you
// to specify.
using GregValure.NaturalDocs.Engine.Files;

using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Output.Styles;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML : Builder, Files.IStyleChangeWatcher, IDisposable
		{

		/* enum: BuildFlags
		 * Flags that specify what parts of the HTML output structure still need to be built.
		 * 
		 * PageFrame - index.html
		 * MainStyleFiles - main.css and main.js
		 * 
		 * FileMenu - files.js
		 */
		[Flags]
		protected enum BuildFlags : byte {
			PageFrame = 0x01,
			MainStyleFiles = 0x02,

			FileMenu = 0x04
			}


		/* enum: ClaimedTaskFlags
		 * Flags that specify which unparallelizable tasks are already being worked on by thread.
		 * 
		 * BuildFileMenu - A thread is updating files.js.
		 * CheckFoldersForDeletion - A thread is going through <foldersToCheckForDeletion>.
		 */
		 [Flags]
		 protected enum ClaimedTaskFlags : byte {
			BuildFileMenu = 0x01,
			CheckFoldersForDeletion = 0x02
			}


		/* enum: PageType
		 * Used for specifying the type of page something applies to.
		 * 
		 * All - Applies to all page types.
		 * Frame - Applies to index.html.
		 * Content - Applies to page content for a source file or class.
		 * Other - Applies to other page content such as the default home page.
		 */
		public enum PageType : byte {
			// Indexes are manual and start at zero so they can be used as indexes into AllPageTypeNames.
  			All = 0,
			Frame = 1,
			Content = 2,
			Other = 3
			}

		

		// Group: Functions
		// __________________________________________________________________________
		
		
		public HTML (Config.Entries.HTMLOutputFolder configEntry) : base ()
			{
			writeLock = new object();

			sourceFilesToRebuild = null;
			sourceFilesWithContent = null;
			foldersToCheckForDeletion = null;
			buildFlags = 0;
			claimedTaskFlags = 0;

			config = configEntry;
			styles = null;
			fileMenuRootFolderIDs = null;

			fileTopicTypeID = -1;
			nonCodeTopicTypeIDs = new IDObjects.NumberSet();
			}


		public override bool Start (Errors.ErrorList errorList)
			{  
			int errors = errorList.Count;


			// Look up special topic type IDs

			bool ignore;
			TopicTypes.TopicType topicType = Instance.TopicTypes.FromKeyword("File", out ignore);

			if (topicType != null)
				{  fileTopicTypeID = topicType.ID;  }

			// This list isn't meant to be definitive since people can just define their own topic types, but it helps.
			string[] nonCodeKeywords = { "Topic", "File", "Group", "Section" };

			foreach (string nonCodeKeyword in nonCodeKeywords)
				{
				topicType = Instance.TopicTypes.FromKeyword(nonCodeKeyword, out ignore);

				if (topicType != null)
					{  nonCodeTopicTypeIDs.Add(topicType.ID);  }
				}


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


			// Set the default build flags

			buildFlags = BuildFlags.PageFrame | BuildFlags.MainStyleFiles;
			// FileMenu only gets rebuilt if changes are detected in sourceFilesWithContent.
			// If you ever make this differential, remember that PageFrame depends on the project name and other
			// information.


			// Load Config.nd

			List<HTMLStyle> previousStyles;
			List<FileSourceInfo> previousFileSourceInfoList;
			bool hasBinaryConfigFile = false;
			
			if (!Engine.Instance.Config.ReparseEverything)
				{
				hasBinaryConfigFile = LoadBinaryConfigFile(config.OutputWorkingDataFolder + "/Config.nd", 
																							out previousStyles, out previousFileSourceInfoList);
				}
			else
				{
				previousStyles = new List<HTMLStyle>();
				previousFileSourceInfoList = new List<FileSourceInfo>();
				}

			
			// Load BuildState.nd

			bool hasBinaryBuildStateFile = false;
			
			if (!Engine.Instance.Config.ReparseEverything)
				{
				hasBinaryBuildStateFile = LoadBinaryBuildStateFile(config.OutputWorkingDataFolder + "/BuildState.nd", 
																										 out sourceFilesToRebuild, out sourceFilesWithContent, 
																										 out foldersToCheckForDeletion, out fileMenuRootFolderIDs);
				}
			else
				{
				sourceFilesToRebuild = new IDObjects.NumberSet();
				sourceFilesWithContent = new IDObjects.NumberSet();
				foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false );
				fileMenuRootFolderIDs = new IDObjects.NumberSet();
				}

			if (!hasBinaryBuildStateFile)
				{
				// Because we need sourceFilesWithContent
				Engine.Instance.Config.ReparseEverything = true;

				// Because we don't know if there was anything left in sourceFilesToRebuild
				Engine.Instance.Config.RebuildAllOutput = true;
				}

			if (Engine.Instance.Config.RebuildAllOutput)
				{  
				buildFlags |= BuildFlags.FileMenu;
				}


			// Compare to the previous list of styles.

			bool saidPurgingOutputFiles = false;

			if (!hasBinaryConfigFile || !hasBinaryBuildStateFile)
				{
				// If the binary file doesn't exist, we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.

				if (System.IO.Directory.Exists(Styles_OutputFolder()))
					{  
					if (!saidPurgingOutputFiles)
						{
						Instance.StartPossiblyLongOperation("PurgingOutputFiles");
						saidPurgingOutputFiles = true;
						}

					try
						{  System.IO.Directory.Delete(Styles_OutputFolder(), true);  }
					catch (Exception e)
						{
						if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
							{  throw;  }
						}
					}

				Engine.Instance.Output.ReparseStyleFiles = true;
				}

			else // (hasBinaryFile)
				{
				// Purge the style folders of anything deleted or changed.

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
						Path folder = Styles_OutputFolder(previousStyle);

						if (System.IO.Directory.Exists(folder))
							{  
							if (!saidPurgingOutputFiles)
								{
								Instance.StartPossiblyLongOperation("PurgingOutputFiles");
								saidPurgingOutputFiles = true;
								}

							System.IO.Directory.Delete(folder, true);  
							}
						}
					}

				// Reparse styles on anything new or changed.  If a style is new we can't assume all its files are going to be
				// sent to the IChangeWatcher functions because another output target may have been using it, and thus they
				// are already in Files.Manager.

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
						if (!saidPurgingOutputFiles)
							{
							Instance.StartPossiblyLongOperation("PurgingOutputFiles");
							saidPurgingOutputFiles = true;
							}

						System.IO.Directory.Delete(outputFolder, true);  
						}
					}

				Engine.Instance.Config.RebuildAllOutput = true;
				}

			else  // (hasBinaryFile)
				{
				bool hasDeletions = false;
				bool hasAdditions = false;


				// Purge the output folders of anything deleted or changed.

				foreach (FileSourceInfo previousFileSourceInfo in previousFileSourceInfoList)
					{
					bool stillExists = false;

					foreach (Files.FileSource fileSource in Engine.Instance.Files.FileSources)
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
							{  outputFolder = Source_OutputFolder(previousFileSourceInfo.Number);  }
						else
							{  
							throw new Exception("xxx");							// xxx image source
							}

						if (System.IO.Directory.Exists(outputFolder))
							{  
							if (!saidPurgingOutputFiles)
								{
								Instance.StartPossiblyLongOperation("PurgingOutputFiles");
								saidPurgingOutputFiles = true;
								}

							System.IO.Directory.Delete(outputFolder, true);
							}
						}
					}


				// Check if anything was added or changed.

				foreach (Files.FileSource fileSource in Engine.Instance.Files.FileSources)
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
					{  Engine.Instance.Config.RebuildAllOutput = true;  }
				}


			// If the binary file doesn't exist, we have to purge all the menu files

			if (!hasBinaryBuildStateFile)
				{

				if (System.IO.Directory.Exists(FileMenu_OutputFolder))
					{  
					if (!saidPurgingOutputFiles)
						{
						Instance.StartPossiblyLongOperation("PurgingOutputFiles");
						saidPurgingOutputFiles = true;
						}

					try
						{  System.IO.Directory.Delete(FileMenu_OutputFolder, true);  }
					catch (Exception e)
						{
						if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
							{  throw;  }
						}
					}

				buildFlags |= BuildFlags.FileMenu;
				}


			// We're done with anything that could purge.

			if (saidPurgingOutputFiles)
				{  Instance.EndPossiblyLongOperation();  }


			// Resave the Style.txt-based styles.

			foreach (HTMLStyle style in styles)
				{
				if (style.IsCSSOnly == false)
					{  
					// No error on save for system styles.
					SaveStyle(style, errorList, style.IsSystemStyle);  
					}
				}


			// Save Config.nd.

			if (!System.IO.Directory.Exists(config.OutputWorkingDataFolder))
				{  System.IO.Directory.CreateDirectory(config.OutputWorkingDataFolder);  }

			List<FileSourceInfo> fileSourceInfoList = new List<FileSourceInfo>();

			foreach (Files.FileSource fileSource in Engine.Instance.Files.FileSources)
				{
				if (fileSource.Type == Files.InputType.Source || fileSource.Type == Files.InputType.Image)
					{
					FileSourceInfo fileSourceInfo = new FileSourceInfo();
					fileSourceInfo.CopyFrom(fileSource);
					fileSourceInfoList.Add(fileSourceInfo);
					};
				}

			SaveBinaryConfigFile(config.OutputWorkingDataFolder + "/Config.nd", styles, fileSourceInfoList);


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


		/* Function: Dispose
		 */
		public void Dispose ()
			{
			try
				{
				SaveBinaryBuildStateFile( config.OutputWorkingDataFolder + "/BuildState.nd", sourceFilesToRebuild, sourceFilesWithContent,
																  foldersToCheckForDeletion, fileMenuRootFolderIDs );
				}
			catch 
				{  }
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


					// Build frame page

					if ((buildFlags & BuildFlags.PageFrame) != 0)
						{
						buildFlags &= ~BuildFlags.PageFrame;
						Monitor.Exit(writeLock);
						haveLock = false;

						BuildPageFrame(cancelDelegate);

						if (cancelDelegate())
							{
							lock (writeLock)
								{  buildFlags |= BuildFlags.PageFrame;  }
							}
						}


					// Build main style files
						
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


					// Build source files
						
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
			

		public override void WorkOnFinalizingOutput (CancelDelegate cancelDelegate)
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


					// Delete empty folders

					// This task is not parallelizable so it gets claimed by one thread and looped to completion.  Theoretically it should
					// be, but in practice when two or more threads try to delete the same folder at the same time they both fail.  This
					// could happen if both the folder and it's parent folder are on the deletion list, so one thread gets it from the list
					// while the other thread gets it by walking up the child's tree.

					if ( (claimedTaskFlags & ClaimedTaskFlags.CheckFoldersForDeletion) == 0 &&
						 foldersToCheckForDeletion.IsEmpty == false)
						{
						claimedTaskFlags |= ClaimedTaskFlags.CheckFoldersForDeletion;

						try
							{
							while (!cancelDelegate())
								{
								Path folder = foldersToCheckForDeletion.RemoveOne();

								Monitor.Exit(writeLock);
								haveLock = false;

								if (folder == null)
									{  break;  }

								DeleteEmptyFolders(folder);

								Monitor.Enter(writeLock);
								haveLock = true;
								}
							}
						finally
							{  claimedTaskFlags &= ~ClaimedTaskFlags.CheckFoldersForDeletion;  }
						}


					// Build file menu

					else if ( (buildFlags & BuildFlags.FileMenu) != 0 &&
								  (claimedTaskFlags & ClaimedTaskFlags.BuildFileMenu) == 0)
						{
						claimedTaskFlags |= ClaimedTaskFlags.BuildFileMenu;

						Monitor.Exit(writeLock);
						haveLock = false;

						BuildFileMenu(cancelDelegate);

						Monitor.Enter(writeLock);
						haveLock = true;

						claimedTaskFlags &= ~ClaimedTaskFlags.BuildFileMenu;

						if (!cancelDelegate())
							{  buildFlags &= ~BuildFlags.FileMenu;  }

						Monitor.Exit(writeLock);
						haveLock = false;
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
		protected void BuildFile (Path outputPath, string pageTitle, string pageContentHTML, PageType pageType)
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

							"<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">" +

							"<title>" + pageTitle.ToHTML() + "</title>" +

							"<link rel=\"stylesheet\" type=\"text/css\" href=\"" +
								MakeRelativeURL(outputPath, Styles_OutputFolder() + "/main.css") +
								"\">");

							string allName = PageTypeNameOf(PageType.All);
							string typeName = PageTypeNameOf(pageType);

							string allJSRelativeURL = MakeRelativeURL(outputPath, Styles_OutputFolder() + "/main-" + allName.ToLower() + ".js");
							string typeJSRelativeURL = MakeRelativeURL(outputPath, Styles_OutputFolder() + "/main-" + typeName.ToLower() + ".js");
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


		/* Function: BuildPageFrame
		 * Builds index.html, which provides the documentation frame.
		 */
		protected void BuildPageFrame (CancelDelegate cancelDelegate)
			{

			// Page and header titles

			string rawPageTitle;
			string headerTitleHTML;
			string headerSubTitleHTML;

			if (config.ProjectInfo.Title == null)
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.DefaultPageTitle");
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHeaderTitle").ToHTML();
				headerSubTitleHTML = null;
				}
			else
				{
				rawPageTitle = Locale.Get("NaturalDocs.Engine", "HTML.PageTitle(projectTitle)", config.ProjectInfo.Title);
				headerTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderTitle(projectTitle)", config.ProjectInfo.Title).ToHTML();

				if (config.ProjectInfo.Subtitle == null)
					{  headerSubTitleHTML = null;  }
				else
					{
					headerSubTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.HeaderSubTitle(projectSubTitle)",
																				 config.ProjectInfo.Subtitle).ToHTML();
					}
				}


			// Footer

			string timeStampHTML = config.ProjectInfo.MakeTimeStamp();
			string copyrightHTML = config.ProjectInfo.Copyright;

			if (timeStampHTML != null)
				{  timeStampHTML = timeStampHTML.ToHTML();  }
			if (copyrightHTML != null)
				{  copyrightHTML = copyrightHTML.ToHTML();  }


			// index.html, the main page frame

			StringBuilder content = new StringBuilder();

			content.Append(

				"<div id=\"NDMessages\">" +
					"<a href=\"javascript:NDPageFrame.CloseMessages()\" id=\"MsgCloseButton\">" +
						Locale.Get("NaturalDocs.Engine", "HTML.Close").ToHTML() +
					"</a>" +
					"<div id=\"MsgContent\"></div>" +
				"</div>" +

				"<div id=\"NDHeader\">" +
					"<div id=\"HTitle\">" +
					
						headerTitleHTML +
					
					"</div>");

					if (headerSubTitleHTML != null)
						{  
						content.Append(
							"<div id=\"HSubTitle\">" +
								headerSubTitleHTML +
							"</div>");  
						}

				content.Append(
				"</div>" +

				"<div id=\"NDMenu\">" +
					"<div id=\"MContent\">" +
					"</div>" +
				"</div>" +

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

					if (timeStampHTML != null)
						{
						content.Append(
							"<div id=\"FTimeStamp\">" +
								timeStampHTML +
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

			BuildFile(OutputFolder + "/index.html", rawPageTitle, content.ToString(), PageType.Frame);


			// other/home.html, the default welcome page

			content.Remove(0, content.Length);

			string titleHTML, subTitleHTML;

			if (config.ProjectInfo.Title != null)
				{
				titleHTML = headerTitleHTML;

				if (config.ProjectInfo.Subtitle != null)
					{  subTitleHTML = headerSubTitleHTML;  }
				else
					{  subTitleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeSubTitleIfTitleExists").ToHTML();  }
				}
			else
				{
				titleHTML = Locale.Get("NaturalDocs.Engine", "HTML.DefaultHomeTitle").ToHTML();
				subTitleHTML = null;
				}

			content.Append(
				"\r\n\r\n" +
				"<div class=\"OHomeHeader\">" + 
					"<div class=\"OHomeTitle\">" +
						titleHTML + 
					"</div>");

					if (subTitleHTML != null)
						{
						content.Append(
							"<div class=\"OHomeSubTitle\">" +
								subTitleHTML + 
							"</div>");
						}

			content.Append(
				"</div>");

			string welcome;

			if (config.ProjectInfo.Subtitle != null)
				{  
				welcome = Locale.Get("NaturalDocs.Engine", "HTML.HomeParagraph(projectTitle, projectSubTitle).multiline", 
																										config.ProjectInfo.Title, config.ProjectInfo.Subtitle);
				}
			else if (config.ProjectInfo.Title != null)
				{  welcome = Locale.Get("NaturalDocs.Engine", "HTML.HomeParagraph(projectTitle).multiline", config.ProjectInfo.Title);  }
			else
				{  welcome = Locale.Get("NaturalDocs.Engine", "HTML.HomeParagraph.multiline");  }

			content.Append(
				"\r\n\r\n" +
				"<p>" + welcome.ToHTML() + "</p>" +

				"\r\n\r\n" +
				"<div class=\"OHomeFooter\">");

				if (copyrightHTML != null)
					{
					content.Append(
						"<div class=\"OHomeCopyright\">" +
							copyrightHTML +
						"</div>");
					}

				if (timeStampHTML != null)
					{
					content.Append(
						"<div class=\"OHomeTimeStamp\">" +
							timeStampHTML +
						"</div>");
					}

				content.Append(
				"<div class=\"OHomeGeneratedBy\">" +
					"<a href=\"http://www.naturaldocs.org\">Generated by Natural Docs</a>" +
				"</div>" +

			"</div>");

			BuildFile(OutputFolder + "/other/home.html", rawPageTitle, content.ToString(), PageType.Other);
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
				catch
					{  break;  }

				folder = folder.ParentFolder;
				}
			}



		// Group: File Functions
		// __________________________________________________________________________


		/* Function: LoadBinaryConfigFile
		 * Loads the information in <Config.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public static bool LoadBinaryConfigFile (Path filename, out List<HTMLStyle> styles, out List<FileSourceInfo> fileSourceInfoList)
			{
			styles = new List<HTMLStyle>();
			fileSourceInfoList = new List<FileSourceInfo>();

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

					// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
					// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
					// ...
					// [Int32: 0]

					FileSourceInfo fileSourceInfo = new FileSourceInfo();
					fileSourceInfo.Type = Files.InputType.Source;

					for (;;)
						{
						fileSourceInfo.Number = binaryFile.ReadInt32();

						if (fileSourceInfo.Number == 0)
							{  break;  }

						fileSourceInfo.UniqueIDString = binaryFile.ReadString();
						fileSourceInfoList.Add(fileSourceInfo);
						}

					// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
					// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
					// ...
					// [Int32: 0]

					fileSourceInfo.Type = Files.InputType.Image;

					for (;;)
						{
						fileSourceInfo.Number = binaryFile.ReadInt32();

						if (fileSourceInfo.Number == 0)
							{  break;  }

						fileSourceInfo.UniqueIDString = binaryFile.ReadString();
						fileSourceInfoList.Add(fileSourceInfo);
						}
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{
				styles.Clear();
				fileSourceInfoList.Clear();
				}

			return result;
			}


		/* Function: SaveBinaryConfigFile
		 * Saves the passed information in <Config.nd>.
		 */
		public static void SaveBinaryConfigFile (Path filename, List<HTMLStyle> styles, List<FileSourceInfo> fileSourceInfoList)
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

				// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
				// [Int32: Source FileSource Number] [String: Source FileSource UniqueIDString]
				// ...
				// [Int32: 0]

				foreach (FileSourceInfo fileSourceInfo in fileSourceInfoList)
					{
					if (fileSourceInfo.Type == Files.InputType.Source)
						{
						binaryFile.WriteInt32(fileSourceInfo.Number);
						binaryFile.WriteString(fileSourceInfo.UniqueIDString);
						}
					}

				binaryFile.WriteInt32(0);

				// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
				// [Int32: Image FileSource Number] [String: Image FileSource UniqueIDString]
				// ...
				// [Int32: 0]

				foreach (FileSourceInfo fileSourceInfo in fileSourceInfoList)
					{
					if (fileSourceInfo.Type == Files.InputType.Image)
						{
						binaryFile.WriteInt32(fileSourceInfo.Number);
						binaryFile.WriteString(fileSourceInfo.UniqueIDString);
						}
					}

				binaryFile.WriteInt32(0);
				}
			}


		/* Function: LoadBinaryBuildStateFile
		 * Loads the information in <BuildState.nd> and returns whether it was successful.  If not all the out parameters will still 
		 * return objects, they will just be empty.  
		 */
		public static bool LoadBinaryBuildStateFile (Path filename, out IDObjects.NumberSet fileIDsToBuild, 
																					 out IDObjects.NumberSet fileIDsWithContent, out StringSet foldersToCheckForDeletion,
																					 out IDObjects.NumberSet fileMenuRootFolderIDs)
			{
			fileIDsToBuild = null;
			fileIDsWithContent = null;
			foldersToCheckForDeletion = null;
			fileMenuRootFolderIDs = null;

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Source File IDs with Content]
					// [StringSet: Folders to Check for Deletion]
					// [NumberSet: File Menu Root Folder IDs]

					fileIDsToBuild = new IDObjects.NumberSet(binaryFile);
					fileIDsWithContent = new IDObjects.NumberSet(binaryFile);
					foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false, binaryFile);
					fileMenuRootFolderIDs = new IDObjects.NumberSet(binaryFile);
					}
				}
			catch
				{  result = false;  }
			finally
				{  binaryFile.Dispose();  }

			if (result == false)
				{
				if (fileIDsToBuild == null)
					{  fileIDsToBuild = new IDObjects.NumberSet();  }
				else
					{  fileIDsToBuild.Clear();  }

				if (fileIDsWithContent == null)
					{  fileIDsWithContent = new IDObjects.NumberSet();  }
				else
					{  fileIDsWithContent.Clear();  }

				if (foldersToCheckForDeletion == null)
					{  foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false);  }
				else
					{  foldersToCheckForDeletion.Clear();  }

				if (fileMenuRootFolderIDs == null)
					{  fileMenuRootFolderIDs = new IDObjects.NumberSet();  }
				else
					{  fileMenuRootFolderIDs.Clear();  }
				}

			return result;
			}


		/* Function: SaveBinaryBuildStateFile
		 * Saves the passed information in <BuildState.nd>.
		 */
		public static void SaveBinaryBuildStateFile (Path filename, IDObjects.NumberSet fileIDsToBuild, 
																							IDObjects.NumberSet fileIDsWithContent, StringSet foldersToCheckForDeletion,
																							IDObjects.NumberSet fileMenuRootFolderIDs)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [StringSet: Folders to Check for Deletion]
				// [NumberSet: File Menu Root Folder IDs]

				binaryFile.WriteObject(fileIDsToBuild);
				binaryFile.WriteObject(fileIDsWithContent);
				binaryFile.WriteObject(foldersToCheckForDeletion);
				binaryFile.WriteObject(fileMenuRootFolderIDs);
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

		/* Function: SanitizePath
		 * Replaces characters in the path according to the <Path Restrictions>.  This should only be used for parts of the path
		 * generated by user information such as source folders.  You don't want to use this on the absolute result path because 
		 * it could replace things like the colon after the drive letter on Windows or parts of the path specifying the root output 
		 * folder instead of just folders inside it.
		 */
		protected static Path SanitizePath (Path input)
			{
			#if DEBUG
				if (input.IsAbsolute)
					{  throw new Exception("You can't use SantizePath on an absolute folder.  Only use it for sections generated from user content.");  }
			#endif

			if (input.ToString().IndexOfAny(restrictedPathCharacters) == -1)
				{  return input;  }
			else
				{
				StringBuilder output = new StringBuilder(input);

				foreach (char restrictedPathCharacter in restrictedPathCharacters)
					{  output.Replace(restrictedPathCharacter, '_');  }

				return output.ToString();
				}
			}


		/* Function: SanitizePathString
		 * Same as <SanitizePath()>, only working from a string.  Lighter weight for things like style names which become part of
		 * the output path but would need to be unnecessarily converted back and forth to a <Path> in the other function.
		 */
		protected static string SanitizePathString (string input)
			{
			if (input.IndexOfAny(restrictedPathCharacters) == -1)
				{  return input;  }
			else
				{
				StringBuilder output = new StringBuilder(input);

				foreach (char restrictedPathCharacter in restrictedPathCharacters)
					{  output.Replace(restrictedPathCharacter, '_');  }

				return output.ToString();
				}
			}


		/* Function: MakeRelativeURL
		 * Creates a relative URL between the two absolute filesystem paths.  Make sure the From parameter is a *file* and not
		 * a folder.
		 */
		protected string MakeRelativeURL (Path fromFile, Path toFile)
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

		/* var: sourceFilesWithContent
		 * A set of the source file IDs that contain content this output target can use.  This is different from all the files with
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		protected IDObjects.NumberSet sourceFilesWithContent;
		
		/* var: foldersToCheckForDeletion
		 * A set of folders that have had files removed, and thus should be deleted if empty.
		 */
		protected StringSet foldersToCheckForDeletion;

		/* var: buildFlags
		 * Flags for everything that needs to be built not encompassed by other variables like <sourceFilesToRebuild>.
		 */
		protected BuildFlags buildFlags;

		/* var: claimedTaskFlags
		 * Flags specifying which unparallelizable tasks are already claimed by a thread.
		 */
		protected ClaimedTaskFlags claimedTaskFlags;

		/* var: config
		 */
		protected Config.Entries.HTMLOutputFolder config;

		/* var: styles
		 * A list of <Styles.HTMLStyles> that apply to this builder in the order in which they should be loaded.
		 */
		protected List<Styles.HTMLStyle> styles;

		/* var: fileMenuRootFolderIDs
		 * The IDs used by the <FileMenu> to build <files.js>.
		 */
		protected IDObjects.NumberSet fileMenuRootFolderIDs;

		/* var: fileTopicTypeID
		 * A reference to the <TopicType> ID of the "file" keyword, or -1 if it isn't defined.
		 */
		protected int fileTopicTypeID;

		/* var: nonCodeTopicTypeIDs
		 * A set of the <TopicType> IDs which are definitely not code.  This list is NOT definitive, as people can always 
		 * define their own topic types, but it's still helpful.
		 */
		protected IDObjects.NumberSet nonCodeTopicTypeIDs;



		// Group: Static Functions and Variables
		// __________________________________________________________________________

		/* var: restrictedPathCharacters
		 * An array of characters that cannot appear in output paths according to <Path Restrictions>.
		 */
		public static char[] restrictedPathCharacters = { ':', '#', ';', '&' };

		/* var: breakURLCharacters
		 * An array of characters that cause an inline URL to wrap.
		 */
		public static char[] breakURLCharacters = { '.', '/', '#', '?', '&' };

		/* var: maxUnbrokenURLCharacters
		 * The longest stretch between <breakURLCharacters> that can occur unbroken in an inline URL.  Formatting attempts
		 * to break on those characters as it looks cleaner, but this limit forces it to happen if they don't occur.
		 */
		public const int maxUnbrokenURLCharacters = 35;

		/* var: AllPageTypes
		 * A static array of all the choices in <PageType>.
		 */
		public static PageType[] AllPageTypes = { PageType.All, PageType.Frame, PageType.Content, PageType.Other };

		/* var: AllPageTypeNames
		 * A static array of simple A-Z names with each index corresponding to those in <AllPageTypes>.
		 */
		public  static string[] AllPageTypeNames = { "All", "Frame", "Content", "Other" };

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

		static protected Regex.Output.HTML.FileSplitSymbols FileSplitSymbolsRegex = new Regex.Output.HTML.FileSplitSymbols();
		static protected Regex.Output.HTML.CodeSplitSymbols CodeSplitSymbolsRegex = new Regex.Output.HTML.CodeSplitSymbols();

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

