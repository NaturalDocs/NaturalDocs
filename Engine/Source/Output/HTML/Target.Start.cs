/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Target
 * ____________________________________________________________________________
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Files;
using CodeClear.NaturalDocs.Engine.Styles;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Target
		{

		// Group: Functions
		// __________________________________________________________________________
		
		
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
			Styles.ConfigFiles.TextFileParser styleParser = new Styles.ConfigFiles.TextFileParser();

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

			ConfigFiles.BinaryConfigParser binaryConfigParser = new ConfigFiles.BinaryConfigParser();
			Config.ProjectInfo previousProjectInfo;
			List<Style> previousStyles;
			List<FileSourceInfo> previousFileSourceInfoList;
			bool hasBinaryConfigFile = false;
			
			if (!EngineInstance.HasIssues( StartupIssues.NeedToStartFresh ))
				{
				hasBinaryConfigFile = binaryConfigParser.Load(WorkingDataFolder + "/Config.nd", out previousProjectInfo, 
																					out previousStyles, out previousFileSourceInfoList);
				}
			else // start fresh
				{
				previousProjectInfo = new Config.ProjectInfo();
				previousStyles = new List<Style>();
				previousFileSourceInfoList = new List<FileSourceInfo>();
				}


			//
			// Compare to the previous project title, subtitle, and copyright.
			//

			// We don't care about timestamp or home page here since their changes are detected separately.
			bool titlesAndCopyrightChanged = (!hasBinaryConfigFile ||
																ProjectInfo.Title != previousProjectInfo.Title ||
																ProjectInfo.Subtitle != previousProjectInfo.Subtitle ||
																ProjectInfo.Copyright != previousProjectInfo.Copyright);

			
			//
			// Compare to the previous list of styles.
			//

			bool inPurgingOperation = false;
			bool hasStyleChanges = false;

			if (!hasBinaryConfigFile)
				{
				// If we don't have the binary config file we have to purge every style folder because some of them may no longer be in
				// use and we won't know which.
				PurgeAllStyleFolders(ref inPurgingOperation);

				newStartupIssues |= StartupIssues.NeedToReparseStyleFiles;
				hasStyleChanges = true;
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
						hasStyleChanges = true;
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
						hasStyleChanges = true;
						newStartupIssues |= StartupIssues.NeedToReparseStyleFiles;
						break;
						}
					}

				// Check if the list of styles is the same or there's any changes in the settings.

				if (stylesWithInheritance.Count != previousStyles.Count)
					{  hasStyleChanges = true;  }

				if (!hasStyleChanges)
					{
					for (int i = 0; i < stylesWithInheritance.Count; i++)
						{
						if (!stylesWithInheritance[i].IsSameStyleAndProperties(previousStyles[i], includeInheritedStyles: false))
							{
							hasStyleChanges = true;
							break;
							}
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

			ConfigFiles.BinaryBuildStateParser buildStateParser = new ConfigFiles.BinaryBuildStateParser();
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
			// Save Config.nd
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

			binaryConfigParser.Save(WorkingDataFolder + "/Config.nd", ProjectInfo, stylesWithInheritance, fileSourceInfoList);


			//
			// Determine our home page
			//

			AbsolutePath homePage;

			if (ProjectInfo.HomePage != null)
				{  homePage = ProjectInfo.HomePage;  }
			else
				{  homePage = Style.HomePageOf(stylesWithInheritance);  }

			bool homePageChanged = (homePage != buildState.HomePage);
			buildState.HomePage = homePage;

			if (homePage != null)
				{
				DateTime homePageLastModified = System.IO.File.GetLastWriteTimeUtc(homePage);

				if (!homePageChanged && buildState.HomePageLastModified.Ticks != homePageLastModified.Ticks)
					{  homePageChanged = true;  }

				buildState.HomePageLastModified = homePageLastModified;
				}
			else // homePage == null)
				{
				buildState.HomePageLastModified = new DateTime(0);
				}

			// HomePageUsesTimestamp will be determined when it's built.


			//
			// Generate our timestamp
			//

			// We do it here instead of as needed because there are two places it could be used (the frame page and the home page)
			// and we want to avoid the admittedly unlikely possibility that Natural Docs can be building around midnight and use one
			// date for one and another for the other.
			string newTimestamp = ProjectInfo.MakeTimestamp();
			bool timestampChanged = (newTimestamp != buildState.GeneratedTimestamp);

			buildState.GeneratedTimestamp = newTimestamp;


			//
			// Load up unprocessedChanges
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

			else
				{
				if (!hasBinaryConfigFile || titlesAndCopyrightChanged || timestampChanged)
					{  unprocessedChanges.AddFramePage();  }

				if (!hasBinaryConfigFile || hasStyleChanges)	
					{  unprocessedChanges.AddMainStyleFiles();  }

				if (!hasBinaryConfigFile || homePageChanged || titlesAndCopyrightChanged ||
					(timestampChanged && buildState.HomePageUsesTimestamp))
					{  unprocessedChanges.AddHomePage();  }
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


			bool success = (errors == errorList.Count);

			started = success;
			return success;
			}
		}
	}