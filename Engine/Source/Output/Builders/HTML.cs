/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * An output builder for HTML.
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
		 * IndexFile - index.html
		 * MainStyleFiles - main.css and main.js
		 * 
		 * FileHierarchy - FileHierarchy.json
		 */
		[Flags]
		protected enum BuildFlags : byte {
			IndexFile = 0x01,
			MainStyleFiles = 0x02,

			FileHierarchy = 0x04
			}


		/* enum: ClaimedTaskFlags
		 * Flags that specify which unparallelizable tasks are already being worked on by thread.
		 * 
		 * BuildFileHierarchy - A thread is updating FileHierarchy.json.
		 * CheckFoldersForDeletion - A thread is going through <foldersToCheckForDeletion>.
		 */
		 [Flags]
		 protected enum ClaimedTaskFlags : byte {
			BuildFileHierarchy = 0x01,
			CheckFoldersForDeletion = 0x02
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

			sourceFilesToRebuild = null;
			sourceFilesWithContent = null;
			foldersToCheckForDeletion = null;
			buildFlags = 0;
			claimedTaskFlags = 0;

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


			// Set the default build flags

			buildFlags = BuildFlags.IndexFile | BuildFlags.MainStyleFiles;
			// FileHierarchy only gets rebuilt if changes are detected in sourceFilesWithContent.
			buildFlags |= BuildFlags.FileHierarchy;  //xxx for testing


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
																										 out foldersToCheckForDeletion);
				}
			else
				{
				sourceFilesToRebuild = new IDObjects.NumberSet();
				sourceFilesWithContent = new IDObjects.NumberSet();
				foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false );
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
				buildFlags |= BuildFlags.FileHierarchy;
				}


			// Compare to the previous list of styles.

			bool saidPurgingOutputFiles = false;

			if (!hasBinaryConfigFile)
				{
				// If the binary file doesn't exist, we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.

				if (System.IO.Directory.Exists(RootStyleFolder))
					{  
					if (!saidPurgingOutputFiles)
						{
						Instance.StartPossiblyLongOperation("PurgingOutputFiles");
						saidPurgingOutputFiles = true;
						}

					System.IO.Directory.Delete(RootStyleFolder, true);  
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
						Path folder = StyleOutputFolder(previousStyle);

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

			if (!hasBinaryConfigFile)
				{
				// If the binary file doesn't exist, we have to purge every folder because some of them may have changed or are no
				// longer in use and we won't know which.

				Regex.Output.HTML.SourceOrImageOutputFolder sourceOrImageOutputFolderRegex = 
					new Regex.Output.HTML.SourceOrImageOutputFolder();

				string[] outputFolders = System.IO.Directory.GetDirectories(config.Folder);

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
						Path outputFolder = OutputPath(previousFileSourceInfo.Type, previousFileSourceInfo.Number);

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
															  foldersToCheckForDeletion );
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


					// Build index file

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


					// Build file hierarchy

					else if ( (buildFlags & BuildFlags.FileHierarchy) != 0 &&
								  (claimedTaskFlags & ClaimedTaskFlags.BuildFileHierarchy) == 0)
						{
						claimedTaskFlags |= ClaimedTaskFlags.BuildFileHierarchy;

						Monitor.Exit(writeLock);
						haveLock = false;

						BuildFileHierarchy(cancelDelegate);

						Monitor.Enter(writeLock);
						haveLock = true;

						claimedTaskFlags &= ~ClaimedTaskFlags.BuildFileHierarchy;

						if (!cancelDelegate())
							{  buildFlags &= ~BuildFlags.FileHierarchy;  }

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
		protected void BuildIndexFile (CancelDelegate cancelDelegate)
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

				"<div id=\"NDMenu\">" +
					"<div id=\"MContent\">" +
					"</div>" +
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


		/* Function: DeleteEmptyFolders
		 * Deletes the passed folder if it's empty.  If so it also tries the parent folder, continuing up the tree until it finds a
		 * non-empty folder or reaches the root output folder.
		 */
		protected void DeleteEmptyFolders (Path folder)
			{
			while (config.Folder.Contains(folder))
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
																					 out IDObjects.NumberSet fileIDsWithContent, out StringSet foldersToCheckForDeletion)
			{
			fileIDsToBuild = null;
			fileIDsWithContent = null;
			foldersToCheckForDeletion = null;

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

					fileIDsToBuild = new IDObjects.NumberSet(binaryFile);
					fileIDsWithContent = new IDObjects.NumberSet(binaryFile);
					foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false, binaryFile);
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
				}

			return result;
			}


		/* Function: SaveBinaryBuildStateFile
		 * Saves the passed information in <BuildState.nd>.
		 */
		public static void SaveBinaryBuildStateFile (Path filename, IDObjects.NumberSet fileIDsToBuild, 
																					 IDObjects.NumberSet fileIDsWithContent, StringSet foldersToCheckForDeletion)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteObject(fileIDsToBuild);
				binaryFile.WriteObject(fileIDsWithContent);
				binaryFile.WriteObject(foldersToCheckForDeletion);
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________
		

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

