/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Target
 * ____________________________________________________________________________
 * 
 * A HTML output target.
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
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Target : Output.Target, CodeDB.IChangeWatcher, Files.IChangeWatcher, SearchIndex.IChangeWatcher, 
											IStartupWatcher, IDisposable
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
		public Target (Output.Manager manager, Config.Targets.HTMLOutputFolder config) : base (manager)
			{
			accessLock = new object();

			buildState = null;
			unprocessedChanges = null;

			this.config = config;
			style = null;
			stylesWithInheritance = null;
			searchIndex = null;
			}


		public override bool Start (Errors.ErrorList errorList)
			{  
			int errors = errorList.Count;
			StartupIssues newStartupIssues = StartupIssues.None;


			//
			// Validate the output folder.
			//

			if (System.IO.Directory.Exists(config.Folder) == false)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.FolderDoesntExist(type, name)", "output", config.Folder) );
				return false;
				}


			//
			// Load and validate the styles, including any inherited styles.
			//

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

			if (style == null)
				{  return false;  }

			stylesWithInheritance = style.BuildInheritanceList();


			//
			// Load Config.nd
			//

			Config_nd binaryConfigParser = new Config_nd();
			List<Style> previousStyles;
			List<FileSourceInfo> previousFileSourceInfoList;
			bool hasBinaryConfigFile = false;
			
			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ))
				{
				hasBinaryConfigFile = binaryConfigParser.Load(WorkingDataFolder + "/Config.nd", out previousStyles, out previousFileSourceInfoList);
				}
			else // start fresh
				{
				previousStyles = new List<Style>();
				previousFileSourceInfoList = new List<FileSourceInfo>();
				}

			
			//
			// Compare to the previous list of styles.
			//

			bool inPurgingOperation = false;

			if (!hasBinaryConfigFile)
				{
				// If we don't have the binary config file we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.
				PurgeAllStyleFolders(ref inPurgingOperation);

				newStartupIssues |= StartupIssues.NeedToReparseStyleFiles;
				}

			else // (hasBinaryConfigFile)
				{
				// Purge the style folders for any no longer in use.

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

					if (!stillExists)
						{  
						PurgeStyleFolder(previousStyle.Name, ref inPurgingOperation);
						}
					}

				// Reparse styles on anything new.  If a style is new we can't assume all its files are going to be sent to the 
				// IChangeWatcher functions because another output target may have been using it, and thus they are already in
				// Files.Manager.

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

					if (!foundMatch)
						{  
						newStartupIssues |= StartupIssues.NeedToReparseStyleFiles;
						break;
						}
					}
				}


			//
			// Compare to the previous list of FileSources.
			//

			if (!hasBinaryConfigFile)
				{
				// If we don't have the binary config file we need to rebuild all the output because we don't know which FileSource was 
				// previously set to which number, which determines which output folder they use, like /files vs /files2.
				newStartupIssues |= StartupIssues.NeedToRebuildAllOutput;

				// This also means we have to purge every source or image output folder because we won't know which changed or are
				// no longer in use.
				PurgeAllSourceAndImageFolders(ref inPurgingOperation);
				}

			else  // (hasBinaryConfigFile)
				{
				bool hasDeletions = false;
				bool hasAdditions = false;


				// Purge the output folders of any deleted FileSources

				foreach (var previousFileSourceInfo in previousFileSourceInfoList)
					{
					bool stillExists = false;

					foreach (var fileSource in EngineInstance.Files.FileSources)
						{
						if (previousFileSourceInfo.IsSameFundamentalFileSource(fileSource))
							{
							stillExists = true;
							break;
							}
						}

					if (!stillExists)
						{
						hasDeletions = true;
						Path outputFolder;
						
						if (previousFileSourceInfo.Type == InputType.Source)
							{  outputFolder = Paths.SourceFile.OutputFolder(OutputFolder, previousFileSourceInfo.Number);  }
						else if (previousFileSourceInfo.Type == InputType.Image)
							{  outputFolder = Paths.Image.OutputFolder(OutputFolder, previousFileSourceInfo.Number, previousFileSourceInfo.Type);  }
						else
							{  throw new NotImplementedException();  }

						PurgeFolder(outputFolder, ref inPurgingOperation);
						}
					}


				// Check if any FileSources were added

				foreach (var fileSource in EngineInstance.Files.FileSources)
					{
					if (fileSource.Type == InputType.Source || fileSource.Type == InputType.Image)
						{
						bool foundMatch = false;

						foreach (var previousFileSourceInfo in previousFileSourceInfoList)
							{
							if (previousFileSourceInfo.IsSameFundamentalFileSource(fileSource))
								{
								foundMatch = true;
								break;
								}
							}

						if (!foundMatch)
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
					{  newStartupIssues |= StartupIssues.NeedToRebuildAllOutput;  }
				}


			//
			// Load BuildState.nd
			//

			BuildState_nd buildStateParser = new BuildState_nd();
			bool hasBinaryBuildStateFile = false;
			
			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh |
														 StartupIssues.FileIDsInvalidated |
														 StartupIssues.CodeIDsInvalidated |
														 StartupIssues.CommentIDsInvalidated ))
				{
				hasBinaryBuildStateFile = buildStateParser.Load(WorkingDataFolder + "/BuildState.nd", out buildState, out unprocessedChanges);
				}
			else // start fresh
				{  
				buildState = new BuildState();
				unprocessedChanges = new UnprocessedChanges();
				}

			if (!hasBinaryBuildStateFile)
				{
				// If we don't have a build state file we need to reparse all the source files because we need to know sourceFilesWithContent
				// and classesWithContent.  We also need to rebuild all the output because we don't know if there was anything left in 
				// sourceFilesToRebuild from the last run.  But those two flags actually aren't enough, because it will reparse those files, send
				// them to CodeDB, and then CodeDB won't send topic updates if the underlying content hasn't changed.  We'd actually need
				// to add all files and classes to UnprocessedChanges, but the Files module isn't started yet.  So fuck it, blow it all up and start
				// over.
				newStartupIssues |= StartupIssues.NeedToStartFresh;

				// Purge everything so no stray files are left behind from the previous build.
				PurgeAllSourceAndImageFolders(ref inPurgingOperation);
				PurgeAllClassFolders(ref inPurgingOperation);
				PurgeAllDatabaseFolders(ref inPurgingOperation);
				PurgeAllStyleFolders(ref inPurgingOperation);
				PurgeAllMenuFolders(ref inPurgingOperation);
				PurgeAllSearchIndexFolders(ref inPurgingOperation);
				}


			//
			// We're done with anything that could purge.
			//

			FinishedPurging(ref inPurgingOperation);


			//
			// Resave the Style.txt-based styles.
			//

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


			//
			// Save Config.nd.
			//

			if (!System.IO.Directory.Exists(WorkingDataFolder))
				{  System.IO.Directory.CreateDirectory(WorkingDataFolder);  }

			List<FileSourceInfo> fileSourceInfoList = new List<FileSourceInfo>();

			foreach (var fileSource in EngineInstance.Files.FileSources)
				{
				if (fileSource.Type == Files.InputType.Source || fileSource.Type == Files.InputType.Image)
					{
					FileSourceInfo fileSourceInfo = new FileSourceInfo();
					fileSourceInfo.CopyFrom(fileSource);
					fileSourceInfoList.Add(fileSourceInfo);
					};
				}

			binaryConfigParser.Save(WorkingDataFolder + "/Config.nd", stylesWithInheritance, fileSourceInfoList);


			//
			// Always rebuild the scaffolding since they're quick.  If you ever make this differential, remember that FramePage depends
			// on the project name and other information.
			//

			unprocessedChanges.AddFramePage();
			unprocessedChanges.AddHomePage();
			unprocessedChanges.AddMainStyleFiles();


			//
			// Load up unprocessedChanges if we're rebuilding
			//

			if (EngineInstance.HasIssues( StartupIssues.NeedToRebuildAllOutput ) ||
				(newStartupIssues & StartupIssues.NeedToRebuildAllOutput) != 0)
				{
				unprocessedChanges.AddSourceFiles(buildState.sourceFilesWithContent);
				unprocessedChanges.AddClasses(buildState.classesWithContent);
				unprocessedChanges.AddImageFiles(buildState.usedImageFiles);

				newStartupIssues |= StartupIssues.NeedToReparseStyleFiles;

				unprocessedChanges.AddMainStyleFiles();
				unprocessedChanges.AddMainSearchFiles();
				unprocessedChanges.AddFramePage();
				unprocessedChanges.AddHomePage();
				unprocessedChanges.AddMenu();

				// We'll handle search prefixes after starting SearchIndex
				}


			//
			// Create the search index, watch other modules, and apply new StartupIssues
			//

			searchIndex = new SearchIndex.Manager(this);

			EngineInstance.CodeDB.AddChangeWatcher(this);
			EngineInstance.Files.AddChangeWatcher(this);
			searchIndex.AddChangeWatcher(this);
			EngineInstance.AddStartupWatcher(this);

			if (newStartupIssues != StartupIssues.None)
				{  EngineInstance.AddStartupIssues(newStartupIssues, dontNotify: this);  }

			searchIndex.Start(errorList);


			//
			// If we're rebuilding everything, add the search index prefixes now that that's started
			//

			if (EngineInstance.HasIssues( StartupIssues.NeedToRebuildAllOutput ))
				{
				var usedPrefixes = searchIndex.UsedPrefixes();

				foreach (var searchPrefix in usedPrefixes)
					{  unprocessedChanges.AddSearchPrefix(searchPrefix);  }
				}


			return (errors == errorList.Count);
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


		/* Function: CreateBuilderProcess
		 * Creates a <TargetBuilder> capable of building the output for this target.
		 */
		override public Output.TargetBuilder CreateBuilderProcess ()
			{
			return new HTML.TargetBuilder(this);
			}


		/* Function: GetStatus
		 * Returns a numeric value representing the total changes yet to be processed.  It is the sum of everything in 
		 * this class weighted by the <TargetBuilder.Cost Constants> which estimate how hard they are to perform.  The value
		 * of the total is meaningless other than to track progress as it works its way towards zero.
		 */
		override public void GetStatus (out long workRemaining)
			{
			unprocessedChanges.GetStatus(out workRemaining);
			}


		/* Function: MakeRelativeURL
		 * Creates a relative URL between the two absolute filesystem paths.  Make sure the From parameter is a *file* and not
		 * a folder.
		 */
		public string MakeRelativeURL (Path fromFile, Path toFile)
			{
			return toFile.MakeRelativeTo(fromFile.ParentFolder).ToURL();
			}



		// Group: Purging Functions
		// __________________________________________________________________________

			
		/* Function: PurgeFolder
		 * Deletes an output folder and all its contents if it exists.  Pass a reference to a bool that tracks whether we've registered a
		 * PossiblyLongOperation for purging with <Engine.Instance>.  If it's set to false and there's a folder to purge it will call
		 * <Engine.Instance.StartPossiblyLongOperation()> and set it to true.  Call <FinishedPurging()> after all purging calls to end
		 * it.
		 */
		protected void PurgeFolder (Path folder, ref bool inPurgingOperation)
			{
			if (System.IO.Directory.Exists(folder))
				{  
				if (!inPurgingOperation)
					{
					EngineInstance.StartPossiblyLongOperation("PurgingOutputFiles");
					inPurgingOperation = true;
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


		/* Function: PurgeStyleFolder
		 * Deletes an output folder for a style and all its contents if it exists.  Pass a reference to a bool that tracks whether we've 
		 * registered a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeStyleFolder (string styleName, ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Style.OutputFolder(this.OutputFolder, styleName), ref inPurgingOperation );
			}


		/* Function: PurgeAllStyleFolders
		 * Deletes all the style output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a 
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllStyleFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Style.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllSourceAndImageFolders
		 * Deletes all the source and image output folders and their contents.  Pass a reference to a bool that tracks whether 
		 * we've registered a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all 
		 * purging calls.
		 */
		protected void PurgeAllSourceAndImageFolders (ref bool inPurgingOperation)
			{
			var sourceOrImageOutputFolderRegex = new Regex.Output.HTML.SourceOrImageOutputFolder();

			string[] outputFolders = System.IO.Directory.GetDirectories(OutputFolder);

			foreach (string outputFolder in outputFolders)
				{
				if (sourceOrImageOutputFolderRegex.IsMatch(outputFolder))
					{
					PurgeFolder(outputFolder, ref inPurgingOperation);
					}
				}
			}


		/* Function: PurgeAllClassFolders
		 * Deletes all the class output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a 
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllClassFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Class.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllDatabaseFolders
		 * Deletes all the database output folders and their contents.  Pass a reference to a bool that tracks whether we've registered
		 * a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllDatabaseFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Database.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllMenuFolders
		 * Deletes all the menu output folders and their contents.  Pass a reference to a bool that tracks whether we've registered a 
		 * PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllMenuFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.Menu.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: PurgeAllSearchIndexFolders
		 * Deletes all the search index output folders and their contents.  Pass a reference to a bool that tracks whether we've registered 
		 * a PossiblyLongOperation for purging with <Engine.Instance>.  Call <FinishedPurging()> after all purging calls.
		 */
		protected void PurgeAllSearchIndexFolders (ref bool inPurgingOperation)
			{
			PurgeFolder( Paths.SearchIndex.OutputFolder(this.OutputFolder), ref inPurgingOperation );
			}


		/* Function: FinishedPurging
		 * Call this after you're done calling all the other purging functions.  Pass the reference to the bool that tracked whether 
		 * we've registered a PossiblyLongOperation for purging with <Engine.Instance>.  If it's set to true it will call
		 * <Engine.Instance.EndPossiblyLongOperation()> and set it to false.
		 */
		protected void FinishedPurging (ref bool inPurgingOperation)
			{
			if (inPurgingOperation)
				{
				EngineInstance.EndPossiblyLongOperation();
				inPurgingOperation = false;
				}
			}



		// Group: Properties
		// __________________________________________________________________________
		

		/* Property: BuildState
		 */
		public BuildState BuildState
			{
			get
				{  return buildState;  }
			}


		/* Property: UnprocessedChanges
		 */
		public UnprocessedChanges UnprocessedChanges
			{
			get
				{  return unprocessedChanges;  }
			}


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


		/* Property: ProjectInfo
		 */
		public Config.ProjectInfo ProjectInfo
			{
			get
				{  return config.ProjectInfo;  }
			}


		/* Property: Style
		 * The <Style> that applies to this target, or null if none.
		 */
		override public Style Style
			{
			get
				{  return style;  }
			}


		/* Property: StylesWithInheritance
		 * A list which includes <Style> and all its inherited members in the order in which they should be applied.
		 */
		public List<Style> StylesWithInheritance
			{
			get
				{  return stylesWithInheritance;  }
			}


		/* Property: SearchIndex
		 * The <SearchIndex.Manager> associated with this build target.
		 */
		public SearchIndex.Manager SearchIndex
			{
			get
				{  return searchIndex;  }
			}



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