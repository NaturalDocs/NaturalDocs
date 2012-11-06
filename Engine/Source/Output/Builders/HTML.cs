/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 * An output builder for HTML.
 * 
 * 
 * Topic: Path Restrictions
 * 
 *		These restrictions apply to both output file paths and to hash paths.  The JavaScript must be able to convert a
 *		hash path to a file path easily so they cannot have different restrictions.
 * 
 *		- Path segments cannot contain a single colon character as it would conflict with the URL hash format
 *		  "#File:[path]:[symbol]".  Double colons are allowed.
 *		- Paths cannot contain the hash character.  File paths containing it would truncate the URL trying to load it.
 *		- Paths cannot contain the semicolon or ampersand characters as not all browsers can load files with them in the 
 *		   path.
 *		- Paths cannot contain the percent sign as it would conflict with URI encoding.
 *		- Paths cannot contain the question mark character as it will cause IE6 to truncate the hash string.
 *		- While browsers support paths with spaces they are restricted anyway to keep URLs contiguous.
 *		- Output file names cannot contain dots because Apache will try to execute Script.pl.html even though .pl is not
 *			the last extension.  Dots in folder names and hash paths are okay though.
 *		
 * 
 * Multithreading: Thread Safety Notes
 * 
 *		> Accessor's database Lock -> writeLock
 * 
 *		Externally, this class is thread safe as functions use <writeLock> to control access to internal variables.
 *		
 *		Interally, if code needs both a database lock and <writeLock> it must acquire the database lock first.  It also 
 *		must not upgrade the database lock from read/possible write to read/write while holding <writeLock>, as there
 *		may be a thread with a read-only accessor waiting for <writeLock>.  Acquiring and releasing <writeLock> while 
 *		holding a database lock is fine as long as no changes will be made to it.
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
 *		> [NumberSet: Class IDs to Rebuild]
 *		
 *		The source and class files that needed to be rebuilt but weren't yet.  If the last build was run to completion these should 
 *		be empty sets, though if the build was interrupted this will have the ones left to do.
 *		
 *		> [NumberSet: Source File IDs with Content]
 *		> [NumberSet: Class IDs with Content]
 *		
 *		A set of all the source and class files known to have content after all filters were applied.
 *		
 *		> [StringSet: Folders to Check for Deletion]
 *		
 *		A set of all folders which have had files removed and thus should be removed if empty.  If the last build was run
 *		to completion this should be an empty set.
 * 
 *		> [String: Data File Type] [NumberSet: Data File Numbers]
 *		> [String: Data File Type] [NumberSet: Data File Numbers]
 *		> ...
 *		> [String: null]
 *		
 *		A list of the data files that were created to build the menu, stored as string-NumberSet pairs that repeats until there
 *		is a null string.  This allows us to clean up old data files if we're using fewer than before.
 *		
 *		The type will be strings like "files" and "classes", so if the menu created files.js, files2.js, and files3.js, this will be
 *		stored as "files" and {1-3}.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
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
using GregValure.NaturalDocs.Engine.IDObjects;
using GregValure.NaturalDocs.Engine.Output.Styles;


namespace GregValure.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML : Builder, Files.IStyleChangeWatcher, IDisposable
		{

		/* enum: BuildFlags
		 * 
		 * Flags that specify what parts of the HTML output structure still need to be built.
		 * 
		 * BuildFramePage - index.html
		 * BuildMainStyleFiles - main.css and main.js
		 * 
		 * BuildMenu - The data files for the menu.
		 * 
		 * CheckFoldersForDeletion - Processing <foldersToCheckForDeletion>.  You can't simply check if the variable is
		 *														 empty because the task isn't parallelizable.  It may have contents but another thread
		 *														 is already working on it, in which case this flag will be reset.
		 */
		[Flags]
		protected enum BuildFlags : byte {
			BuildFramePage = 0x01,
			BuildMainStyleFiles = 0x02,
			BuildMenu = 0x04,

			CheckFoldersForDeletion = 0x08
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
			Home = 3
			}

		

		// Group: Functions
		// __________________________________________________________________________
		
		
		public HTML (Config.Entries.HTMLOutputFolder configEntry) : base ()
			{
			writeLock = new object();

			sourceFilesToRebuild = null;
			classFilesToRebuild = null;
			sourceFilesWithContent = null;
			classFilesWithContent = null;
			foldersToCheckForDeletion = null;
			buildFlags = 0;
			unitsOfWorkInProgress = 0;

			config = configEntry;
			styles = null;
			usedMenuDataFiles = null;
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

			buildFlags = BuildFlags.BuildFramePage | BuildFlags.BuildMainStyleFiles;
			// The menu only gets rebuilt if changes are detected in source/classFilesWithContent.
			// If you ever make this differential, remember that FramePage depends on the project name and other
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
																										 out sourceFilesToRebuild, out classFilesToRebuild,
																										 out sourceFilesWithContent, out classFilesWithContent,
																										 out foldersToCheckForDeletion, out usedMenuDataFiles);
				}
			else
				{
				sourceFilesToRebuild = new IDObjects.NumberSet();
				classFilesToRebuild = new IDObjects.NumberSet();
				sourceFilesWithContent = new IDObjects.NumberSet();
				classFilesWithContent = new IDObjects.NumberSet();
				foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false );
				usedMenuDataFiles = new StringTable<IDObjects.NumberSet>(false, false);
				}

			if (!hasBinaryBuildStateFile)
				{
				// Because we need source/classFilesWithContent
				Engine.Instance.Config.ReparseEverything = true;

				// Because we don't know if there was anything left in sourceFilesToRebuild
				Engine.Instance.Config.RebuildAllOutput = true;
				}

			if (Engine.Instance.Config.RebuildAllOutput)
				{  
				buildFlags |= BuildFlags.BuildMenu;
				}
			if (foldersToCheckForDeletion.IsEmpty == false)
				{
				buildFlags |= BuildFlags.CheckFoldersForDeletion;
				}


			// Compare to the previous list of styles.

			bool saidPurgingOutputFiles = false;

			if (!hasBinaryConfigFile || !hasBinaryBuildStateFile)
				{
				// If the binary file doesn't exist, we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.
				Start_PurgeFolder(Styles_OutputFolder(), ref saidPurgingOutputFiles);
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
						Start_PurgeFolder(Styles_OutputFolder(previousStyle), ref saidPurgingOutputFiles);
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

						Start_PurgeFolder(outputFolder, ref saidPurgingOutputFiles);
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


			// If the binary file doesn't exist, we have to purge all the class and database files

			if (!hasBinaryBuildStateFile)
				{
				Start_PurgeFolder(Class_OutputFolder(), ref saidPurgingOutputFiles);
				Start_PurgeFolder(Database_OutputFolder(), ref saidPurgingOutputFiles);
				}


			// If the binary file doesn't exist, we have to purge all the menu files

			if (!hasBinaryBuildStateFile)
				{
				Start_PurgeFolder(Menu_DataFolder, ref saidPurgingOutputFiles);
				buildFlags |= BuildFlags.BuildMenu;
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
					Instance.StartPossiblyLongOperation("PurgingOutputFiles");
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
		public void Dispose ()
			{
			try
				{
				SaveBinaryBuildStateFile( config.OutputWorkingDataFolder + "/BuildState.nd", sourceFilesToRebuild, classFilesToRebuild,
																  sourceFilesWithContent, classFilesWithContent, foldersToCheckForDeletion, usedMenuDataFiles );
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
					// Remember that you can't acquire or change the database lock while holding this.
					Monitor.Enter(writeLock);
					haveLock = true;
					
					if (cancelDelegate())
						{  return;  }


					// Build frame page

					if ((buildFlags & BuildFlags.BuildFramePage) != 0)
						{
						buildFlags &= ~BuildFlags.BuildFramePage;
						unitsOfWorkInProgress += UnitsOfWork_FramePage;

						Monitor.Exit(writeLock);
						haveLock = false;

						BuildFramePage(cancelDelegate);

						lock (writeLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_FramePage;  }

						if (cancelDelegate())
							{
							lock (writeLock)
								{  buildFlags |= BuildFlags.BuildFramePage;  }
							}
						}


					// Build main style files
						
					else if ((buildFlags & BuildFlags.BuildMainStyleFiles) != 0)
						{
						buildFlags &= ~BuildFlags.BuildMainStyleFiles;
						unitsOfWorkInProgress += UnitsOfWork_MainStyleFiles;

						Monitor.Exit(writeLock);
						haveLock = false;

						BuildMainStyleFiles(cancelDelegate);

						lock (writeLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_MainStyleFiles;  }

						if (cancelDelegate())
							{
							lock (writeLock)
								{  buildFlags |= BuildFlags.BuildMainStyleFiles;  }
							}
						}


					// Build source files
						
					else if (sourceFilesToRebuild.IsEmpty == false)
						{
						int sourceFileToRebuild = sourceFilesToRebuild.Highest;
						sourceFilesToRebuild.Remove(sourceFileToRebuild);
						unitsOfWorkInProgress += UnitsOfWork_SourceFile;
						
						Monitor.Exit(writeLock);
						haveLock = false;
						
						if (accessor == null)
							{  accessor = Engine.Instance.CodeDB.GetAccessor();  }
							
						BuildSourceFile(sourceFileToRebuild, accessor, cancelDelegate);
						
						lock (writeLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_SourceFile;  }

						if (cancelDelegate())
							{
							lock (writeLock)
								{  sourceFilesToRebuild.Add(sourceFileToRebuild);  }
							}						
						}
						

					// Build class files
						
					else if (classFilesToRebuild.IsEmpty == false)
						{
						int classFileToRebuild = classFilesToRebuild.Highest;
						classFilesToRebuild.Remove(classFileToRebuild);
						unitsOfWorkInProgress += UnitsOfWork_ClassFile;
						
						Monitor.Exit(writeLock);
						haveLock = false;
						
						if (accessor == null)
							{  accessor = Engine.Instance.CodeDB.GetAccessor();  }
							
						BuildClassFile(classFileToRebuild, accessor, cancelDelegate);
						
						lock (writeLock)
							{  unitsOfWorkInProgress -= UnitsOfWork_ClassFile;  }

						if (cancelDelegate())
							{
							lock (writeLock)
								{  classFilesToRebuild.Add(classFileToRebuild);  }
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
					// Remember that you can't acquire or change the database lock while holding this.
					Monitor.Enter(writeLock);
					haveLock = true;
					
					if (cancelDelegate())
						{  return;  }


					// Delete empty folders

					// This task is not parallelizable so it gets claimed by one thread and looped to completion.  Theoretically it should
					// be, but in practice when two or more threads try to delete the same folder at the same time they both fail.  This
					// could happen if both the folder and it's parent folder are on the deletion list, so one thread gets it from the list
					// while the other thread gets it by walking up the child's tree.

					if ((buildFlags & BuildFlags.CheckFoldersForDeletion) != 0)
						{
						buildFlags &= ~BuildFlags.CheckFoldersForDeletion;

						while (!cancelDelegate() && foldersToCheckForDeletion.IsEmpty == false)
							{
							Path folder = foldersToCheckForDeletion.RemoveOne();
							unitsOfWorkInProgress += UnitsOfWork_FolderToCheckForDeletion;

							Monitor.Exit(writeLock);
							haveLock = false;

							DeleteEmptyFolders(folder);

							Monitor.Enter(writeLock);
							haveLock = true;

							unitsOfWorkInProgress -= UnitsOfWork_FolderToCheckForDeletion;
							}

						// Reset the flag if it was cancelled with entries still left to do.
						if (foldersToCheckForDeletion.IsEmpty == false)
							{  buildFlags |= BuildFlags.CheckFoldersForDeletion;  }

						Monitor.Exit(writeLock);
						haveLock = false;
						}


					// Build the menu

					else if ((buildFlags & BuildFlags.BuildMenu) != 0)
						{
						buildFlags &= ~BuildFlags.BuildMenu;
						unitsOfWorkInProgress += UnitsOfWork_Menu;

						Monitor.Exit(writeLock);
						haveLock = false;

						if (accessor == null)
							{  accessor = Engine.Instance.CodeDB.GetAccessor();  }

						BuildMenu(accessor, cancelDelegate);

						Monitor.Enter(writeLock);
						haveLock = true;

						unitsOfWorkInProgress -= UnitsOfWork_Menu;

						if (cancelDelegate())
							{  buildFlags |= BuildFlags.BuildMenu;  }

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
			

		override public long UnitsOfWorkRemaining ()
			{
			long value = 0;

			lock (writeLock)
				{
				value += sourceFilesToRebuild.Count * UnitsOfWork_SourceFile;
				value += classFilesToRebuild.Count * UnitsOfWork_ClassFile;
				value += foldersToCheckForDeletion.Count * UnitsOfWork_FolderToCheckForDeletion;

				if ((buildFlags & BuildFlags.BuildFramePage) != 0)
					{  value += UnitsOfWork_FramePage;  }
				if ((buildFlags & BuildFlags.BuildMainStyleFiles) != 0)
					{  value += UnitsOfWork_MainStyleFiles;  }
				if ((buildFlags & BuildFlags.BuildMenu) != 0)
					{  value += UnitsOfWork_Menu;  }

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

							"<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\">" +

							"<title>" + pageTitle.ToHTML() + "</title>" +

							"<link rel=\"stylesheet\" type=\"text/css\" href=\"" +
								MakeRelativeURL(outputPath, Styles_OutputFolder() + "/main.css") +
								"\">");

							string pageTypeName = PageTypeNameOf(pageType);
							string jsRelativePrefix = MakeRelativeURL(outputPath, Styles_OutputFolder()) + '/';

							file.Write(
							"<script type=\"text/javascript\" src=\"" + jsRelativePrefix + "main.js\"></script>" +
							"<script type=\"text/javascript\">" +
								"NDLoader.LoadJS(\"" + pageTypeName + "\", \"" + jsRelativePrefix + "\");" +
							"</script>" +

						"</head>" + 

							"\r\n\r\n" +
							"<!-- Generated by Natural Docs, version " + Instance.VersionString + " -->" +
							"\r\n\r\n" +

							// The IE mark of the web which prevents it from popping up the information bar when loading HTML from the
							// local drive.  Note that it MUST have at least one \r\n after it or it won't work.
							"<!-- saved from url=(0026)http://www.naturaldocs.org -->" +
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
				"<div class=\"HFrame\">" +
					"<div class=\"HHeader\">" + 
						"<div class=\"HTitle\">" +
							titleHTML + 
						"</div>");

						if (subTitleHTML != null)
							{
							content.Append(
								"<div class=\"HSubTitle\">" +
									subTitleHTML + 
								"</div>");
							}

					content.Append("</div>");

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

					if (timeStampHTML != null)
						{
						content.Append(
							"<div class=\"HTimeStamp\">" +
								timeStampHTML +
							"</div>");
						}

					content.Append(
					"<div class=\"HGeneratedBy\">" +
						"<a href=\"http://www.naturaldocs.org\">Generated by Natural Docs</a>" +
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
		public static bool LoadBinaryBuildStateFile (Path filename, 
																							out IDObjects.NumberSet fileIDsToBuild, out IDObjects.NumberSet classIDsToBuild,
																							out IDObjects.NumberSet fileIDsWithContent, out IDObjects.NumberSet classIDsWithContent,
																							out StringSet foldersToCheckForDeletion,
																							out StringTable<IDObjects.NumberSet> usedDataFiles)
			{
			fileIDsToBuild = null;
			classIDsToBuild = null;
			fileIDsWithContent = null;
			classIDsWithContent = null;
			foldersToCheckForDeletion = null;
			usedDataFiles = null;

			BinaryFile binaryFile = new BinaryFile();
			bool result = true;

			try
				{
				if (binaryFile.OpenForReading(filename, "2.0") == false)
					{  result = false;  }
				else
					{
					// [NumberSet: Source File IDs to Rebuild]
					// [NumberSet: Class IDs to Rebuild]
					// [NumberSet: Source File IDs with Content]
					// [NumberSet: Class IDs with Content]
					// [StringSet: Folders to Check for Deletion]

					fileIDsToBuild = binaryFile.ReadNumberSet();
					classIDsToBuild = binaryFile.ReadNumberSet();
					fileIDsWithContent = binaryFile.ReadNumberSet();
					classIDsWithContent = binaryFile.ReadNumberSet();
					foldersToCheckForDeletion = binaryFile.ReadStringSet(Config.Manager.IgnoreCaseInPaths, false);

					// [String: Data File Type] [NumberSet: Data File Numbers]
					// [String: Data File Type] [NumberSet: Data File Numbers]
					// ...
					// [String: null]

					usedDataFiles = new StringTable<IDObjects.NumberSet>(false, false);
					string dataFileType = binaryFile.ReadString();

					while (dataFileType != null)
						{
						IDObjects.NumberSet dataFileNumbers = binaryFile.ReadNumberSet();
						usedDataFiles.Add(dataFileType, dataFileNumbers);

						dataFileType = binaryFile.ReadString();
						}
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

				if (classIDsToBuild == null)
					{  classIDsToBuild = new IDObjects.NumberSet();  }
				else
					{  classIDsToBuild.Clear();  }

				if (fileIDsWithContent == null)
					{  fileIDsWithContent = new IDObjects.NumberSet();  }
				else
					{  fileIDsWithContent.Clear();  }

				if (classIDsWithContent == null)
					{  classIDsWithContent = new IDObjects.NumberSet();  }
				else
					{  classIDsWithContent.Clear();  }

				if (foldersToCheckForDeletion == null)
					{  foldersToCheckForDeletion = new StringSet( Config.Manager.IgnoreCaseInPaths, false);  }
				else
					{  foldersToCheckForDeletion.Clear();  }

				if (usedDataFiles == null)
					{  usedDataFiles = new StringTable<IDObjects.NumberSet>(false, false);  }
				else
					{  usedDataFiles.Clear();  }
				}

			return result;
			}


		/* Function: SaveBinaryBuildStateFile
		 * Saves the passed information in <BuildState.nd>.
		 */
		public static void SaveBinaryBuildStateFile (Path filename, 
																							IDObjects.NumberSet fileIDsToBuild, IDObjects.NumberSet classIDsToBuild,
																							IDObjects.NumberSet fileIDsWithContent, IDObjects.NumberSet classIDsWithContent,
																							StringSet foldersToCheckForDeletion, StringTable<IDObjects.NumberSet> usedDataFiles)
			{
			using (BinaryFile binaryFile = new BinaryFile())
				{
				binaryFile.OpenForWriting(filename);

				// [NumberSet: Source File IDs to Rebuild]
				// [NumberSet: Class IDs to Rebuild]
				// [NumberSet: Source File IDs with Content]
				// [NumberSet: Class IDs with Content]
				// [StringSet: Folders to Check for Deletion]

				binaryFile.WriteNumberSet(fileIDsToBuild);
				binaryFile.WriteNumberSet(classIDsToBuild);
				binaryFile.WriteNumberSet(fileIDsWithContent);
				binaryFile.WriteNumberSet(classIDsWithContent);
				binaryFile.WriteStringSet(foldersToCheckForDeletion);

				// [String: Data File Type] [NumberSet: Data File Numbers]
				// [String: Data File Type] [NumberSet: Data File Numbers]
				// ...
				// [String: null]

				foreach (var dataFilePair in usedDataFiles)
					{
					binaryFile.WriteString(dataFilePair.Key);
					binaryFile.WriteNumberSet(dataFilePair.Value);
					}

				binaryFile.WriteString(null);
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
		 * Replaces characters in the path according to the <path restrictions>.  This should only be used for parts of the path
		 * generated by user information such as source folders.  You don't want to use this on the final combined path because 
		 * it could replace things like the colon that separates hash path sections or the colon that follows the drive letter in 
		 * Windows.
		 */
		public static string SanitizePath (string input, bool replaceDots = false)
			{
			if (input.IndexOfAny(restrictedPathChars) == -1 && (replaceDots == false || input.IndexOf('.') == -1))
				{  return input;  }
			else
				{
				StringBuilder output = new StringBuilder(input);

				foreach (char restrictedPathChar in restrictedPathChars)
					{  
					if (restrictedPathChar != ':')
						{  output.Replace(restrictedPathChar, '_');  }
					}

				if (replaceDots)
					{  output.Replace('.', '-');  }

				// We can still use input to get indexes for output because the translation has been 1:1 so there's no character
				// shifts.  Don't know why StringBuilder doesn't have an IndexOf() function though.
				int index = input.IndexOf(':');

				while (index != -1)
					{
					if (index + 1 < input.Length && input[index+1] == ':')
						{  
						index = input.IndexOf(':', index + 2);  
						}
					else
						{
						output[index] = '_';
						index = input.IndexOf(':', index + 1);
						}
					}

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



		// Group: Constants
		// __________________________________________________________________________


		/* Constants: UnitsOfWork Constants
		 * 
		 * The values for each task when calculating <UnitsOfWorkRemaining()>.
		 * 
		 *		UnitsOfWork_SourceFile - How much building a single source file costs.
		 *		UnitsOfWork_ClassFile - How much building a single class file costs.
		 *		UnitsOfWork_FramePage - How much building index.html costs.
		 *		UnitsOfWork_MainStyleFiles - How much building main.css and main.js costs.
		 *		UnitsOfWork_Menu - How much building the menu costs.
		 *		UnitsOfWork_FolderToCheckForDeletion - How much checking a single folder for deletion costs.
		 */
		protected const long UnitsOfWork_SourceFile = 10;
		protected const long UnitsOfWork_ClassFile = 10;
		protected const long UnitsOfWork_FramePage = 1;
		protected const long UnitsOfWork_MainStyleFiles = 1;
		protected const long UnitsOfWork_Menu = 15;
		protected const long UnitsOfWork_FolderToCheckForDeletion = 1;



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

		/* var: classFilesToRebuild
		 * A set of the class IDs that need their output rebuilt.
		 */
		protected IDObjects.NumberSet classFilesToRebuild;

		/* var: sourceFilesWithContent
		 * A set of the source file IDs that contain content this output target can use.  This is different from all the files with
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		protected IDObjects.NumberSet sourceFilesWithContent;

		/* var: classFilesWithContent
		 * A set of the class IDs that contain content this output target can use.  This is different from all the classes with 
		 * content in <CodeDB.Manager> because it is after all filters have been applied.
		 */
		protected IDObjects.NumberSet classFilesWithContent;
		
		/* var: foldersToCheckForDeletion
		 * A set of folders that have had files removed, and thus should be deleted if empty.  Note that threads should check
		 * <buildFlags> to see if this task is needed rather than checking if this set is empty.  It may not be empty but have
		 * another thread working on it, and the task is not parallelizable.
		 */
		protected StringSet foldersToCheckForDeletion;

		/* var: buildFlags
		 * Flags for everything that needs to be built not encompassed by other variables like <sourceFilesToRebuild>.
		 */
		protected BuildFlags buildFlags;

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
		protected Config.Entries.HTMLOutputFolder config;

		/* var: styles
		 * A list of <Styles.HTMLStyles> that apply to this builder in the order in which they should be loaded.
		 */
		protected List<Styles.HTMLStyle> styles;

		/* var: usedMenuDataFiles
		 * The data files created the last time the menu was built.  It maps strings like "files" and "classes" to NumberSets,
		 * so files.js, files2.js, and files3.js would map to "files" and {1-3}.
		 */
		protected StringTable<IDObjects.NumberSet> usedMenuDataFiles;



		// Group: Static Functions and Variables
		// __________________________________________________________________________

		/* var: restrictedPathChars
		 * An array of characters that cannot appear in paths according to <path restrictions>.
		 */
		public static char[] restrictedPathChars = { ':', '#', '?', ';', '&', '%', ' ' };

		/* var: AllPageTypes
		 * A static array of all the choices in <PageType>.
		 */
		public static PageType[] AllPageTypes = { PageType.All, PageType.Frame, PageType.Content, PageType.Home };

		/* var: AllPageTypeNames
		 * A static array of simple A-Z names with each index corresponding to those in <AllPageTypes>.
		 */
		public static string[] AllPageTypeNames = { "All", "Frame", "Content", "Home" };

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

