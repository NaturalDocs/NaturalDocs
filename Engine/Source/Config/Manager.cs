/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage the engine's configuration.
 * 
 * 
 * Topic: Usage
 * 
 *		- Create a <ProjectInfo> object with the command line configuration.  At minimum the project config folder must be set.
 *		
 *		- Call <Engine.Instance.Start()>, which will start this module.
 *		  
 *		- After the engine has been called all the properties are read-only.
 *		
 *		- All modules *MUST* check <ReparseEverything> before loading their own working data files and not bother if it's
 *		  set.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Errors;


namespace GregValure.NaturalDocs.Engine.Config
	{
	public class Manager
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Constructor: Manager
		 */
		static Manager ()
			{
			systemDefaultConfig = new ProjectConfig(Source.SystemDefault);

			// DEPENDENCY: Start() assumes these properties are set.

			systemDefaultConfig.TabWidth = DefaultTabWidth;
			systemDefaultConfig.TabWidthPropertyLocation = Source.SystemDefault;

			systemDefaultConfig.DocumentedOnly = false;
			systemDefaultConfig.DocumentedOnlyPropertyLocation = Source.SystemDefault;

			systemDefaultConfig.AutoGroup = true;
			systemDefaultConfig.AutoGroupPropertyLocation = Source.SystemDefault;

			systemDefaultConfig.ProjectInfo.StyleName = "Default";
			systemDefaultConfig.ProjectInfo.StyleNamePropertyLocation = Source.SystemDefault;
			}


		/* Constructor: Manager
		 */
		public Manager ()
			{  
			projectConfigFolder = null;
			workingDataFolder = null;
			
			tabWidth = DefaultTabWidth;
			documentedOnly = false;
			autoGroup = true;

			reparseEverything = false;
			rebuildAllOutput = false;

			#if SINGLE_CORE
				backgroundThreadsPerTask = 1;
			#else
				backgroundThreadsPerTask = System.Environment.ProcessorCount;
			
				if (backgroundThreadsPerTask < 1)  // In case it's not detected correctly
					{  backgroundThreadsPerTask = 1;  }
				else if (backgroundThreadsPerTask > 8)  // Hard upper limit
					{  backgroundThreadsPerTask = 8;  }
				else if (backgroundThreadsPerTask > 4)  // Leave one free with 5-8 cores
					{  backgroundThreadsPerTask--;  }
			#endif
			}
			
						
		/* Function: Start
		 * 
		 * Initializes the configuration and returns whether all the settings are correct and that execution is ready to begin.  
		 * If there are problems they are added as <Errors> to the errorList parameter.  This class is *not* designed to allow 
		 * multiple attempts.  If this function fails scrap the entire <Engine.Instance> and start again.
		 * 
		 * After <Start()> is called the properties of this class become read-only.  This function will add all the input and filter
		 * targets it has to <Files.Manager>, and all output targets to <Output.Manager>.
		 */
		public bool Start (ErrorList errorList, ProjectConfig commandLineConfig)
			{
			bool success = true;
			
			
			// Validate project config folder

			projectConfigFolder = commandLineConfig.ProjectConfigFolder;
				
			if (!commandLineConfig.ProjectConfigFolderPropertyLocation.IsDefined ||
				String.IsNullOrEmpty(projectConfigFolder))
				{
				errorList.Add( 
					message: Locale.Get("NaturalDocs.Engine", "Error.NoProjectConfigFolder"),
					configSource: Source.CommandLine,
					property: "ProjectConfigFolder"
					);
					
				success = false;
				}
				
			else if (!System.IO.Directory.Exists(projectConfigFolder))
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderDoesntExist(name)", projectConfigFolder),
					propertyLocation: commandLineConfig.ProjectConfigFolderPropertyLocation,
					property: "ProjectConfigFolder"
					);
					
				success = false;
				}
				
			else if (projectConfigFolder == SystemConfigFolder)
				{
				errorList.Add( 
					message: Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderCannotEqualSystemConfigFolder"),
					propertyLocation: commandLineConfig.ProjectConfigFolderPropertyLocation,
					property: "ProjectConfigFolder"
					);
					
				success = false;
				}

			if (success == false)
				{  return false;  }
				

			// Load and merge configuration files
			
			ProjectConfig combinedConfig = new ProjectConfig(Source.Combined);
			MergeConfig(combinedConfig, commandLineConfig);

			var projectTxtParser = new Project_txt();

			if (System.IO.File.Exists(projectConfigFolder + "/Project.txt"))
				{
				ProjectConfig projectTxtConfig;

				if (projectTxtParser.Load(projectConfigFolder + "/Project.txt", out projectTxtConfig, errorList))
					{  MergeConfig(combinedConfig, projectTxtConfig);  }
				else
					{  success = false;  }
				}
			else if (System.IO.File.Exists(ProjectConfigFolder + "/Menu.txt"))
				{
				// Try to extract information from a pre-2.0 Menu.txt instead.

				ProjectConfig menuTxtConfig;
				var menuTxtParser = new Menu_txt();

				if (menuTxtParser.Load(ProjectConfigFolder + "/Menu.txt", out menuTxtConfig))
					{  MergeConfig(combinedConfig, menuTxtConfig);  }
				// No errors if this fails
				}
			// If neither file exists it's not an error condition.  Just treat it as if nothing was defined.

			MergeConfig(combinedConfig, systemDefaultConfig);

			if (success == false)
				{  return false;  }


			// Validate the working data folder, creating one if it doesn't exist.

			workingDataFolder = combinedConfig.WorkingDataFolder;

			if (!combinedConfig.WorkingDataFolderPropertyLocation.IsDefined ||
				String.IsNullOrEmpty(workingDataFolder))
				{  workingDataFolder = projectConfigFolder + "/Working Data";  }

			if (!System.IO.Directory.Exists(workingDataFolder))
				{
				try
					{  System.IO.Directory.CreateDirectory(workingDataFolder);  }
				catch
					{
					errorList.Add(
						message: Locale.Get("NaturalDocs.Engine", "Error.CantCreateWorkingDataFolder(name)", workingDataFolder),
						property: "WorkingDataFolder"
						);
					
					success = false;
					}
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Load the previous configuration state.  Remember that not every value in ProjectConfig is stored in Project.nd.
				
			ProjectConfig previousConfig = null;
			var projectNDParser = new Project_nd();

			if (ReparseEverything == false && System.IO.File.Exists(workingDataFolder + "/Project.nd"))
				{  
				if (!projectNDParser.Load(workingDataFolder + "/Project.nd", out previousConfig))
					{  previousConfig = null;  }
				}
				

			// Merge output target numbers from Project.nd into the settings.  These are not stored in Project.txt because they're
			// pretty expendible.

			if (previousConfig != null)
				{
				foreach (var target in combinedConfig.OutputTargets)
					{
					foreach (var previousTarget in previousConfig.OutputTargets)
						{
						if (target.IsSameTarget(previousTarget))
							{
							target.Number = previousTarget.Number;
							break;
							}
						}
					}
				}

				
			// Target validation
						
			if (combinedConfig.InputTargets.Count < 1)
				{
				errorList.Add(
					message: Locale.Get("NaturalDocs.Engine", "Error.NoInputTargets"),
					property: "InputTargets"
					);
				success = false;
				}
			if (combinedConfig.OutputTargets.Count < 1)
				{
				errorList.Add( 
					message: Locale.Get("NaturalDocs.Engine", "Error.NoOutputTargets"),
					property: "OutputTargets"
					);
				success = false;
				}

			for (int i = 0; i < combinedConfig.InputTargets.Count; i++)
				{
				if (combinedConfig.InputTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}
			for (int i = 0; i < combinedConfig.FilterTargets.Count; i++)
				{
				if (combinedConfig.FilterTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}
			for (int i = 0; i < combinedConfig.OutputTargets.Count; i++)
				{
				if (combinedConfig.OutputTargets[i].Validate(errorList, i) == false)
					{  success = false;  }
				}

			if (success == false)
				{  return false;  }


			// Determine the target numbers that are already used and reset duplicates.
			
			IDObjects.NumberSet usedSourceNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet usedImageNumbers = new IDObjects.NumberSet();
			
			foreach (var target in combinedConfig.InputTargets)
				{
				if (target.Number != 0)
					{
					if (target.Type == Files.InputType.Source)
						{
						if (usedSourceNumbers.Contains(target.Number))
							{
							target.Number = 0;
							target.NumberPropertyLocation = Source.NotDefined;
							}
						else
							{  usedSourceNumbers.Add(target.Number);  }
						}
							
					else if (target.Type == Files.InputType.Image)
						{
						if (usedImageNumbers.Contains(target.Number))
							{
							target.Number = 0;
							target.NumberPropertyLocation = Source.NotDefined;
							}
						else
							{  usedImageNumbers.Add(target.Number);  }
						}
					}
				}

				
			IDObjects.NumberSet usedOutputNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet outputNumbersToPurge = new IDObjects.NumberSet();

			foreach (var target in combinedConfig.OutputTargets)
				{
				if (target.Number != 0)
					{
					if (usedOutputNumbers.Contains(target.Number))
						{  
						target.Number = 0;
						target.NumberPropertyLocation = Source.NotDefined;

						// Since we don't know which of the two entries generated working data under this number, purge it to be safe.
						outputNumbersToPurge.Add(target.Number);
						}
					else
						{  usedOutputNumbers.Add(target.Number);  }
					}
				}
				

			// Assign numbers to the entries that don't already have them and generate default input folder names.

			foreach (var target in combinedConfig.InputTargets)
				{
				if (target.Type == Files.InputType.Source)
					{
					if (target.Number == 0)
						{
						target.Number = usedSourceNumbers.LowestAvailable;
						target.NumberPropertyLocation = Source.SystemGenerated;

						usedSourceNumbers.Add(target.Number);
						}
							
					if (target.Name == null && combinedConfig.InputTargets.Count > 1)
						{
						target.GenerateDefaultName();
						}
					}
						
				else if (target.Type == Files.InputType.Image)
					{
					if (target.Number == 0)
						{
						target.Number = usedImageNumbers.LowestAvailable;
						target.NumberPropertyLocation = Source.SystemGenerated;

						usedImageNumbers.Add(target.Number);
						}
					}
				}

			foreach (var target in combinedConfig.OutputTargets)
				{
				if (target.Number == 0)
					{
					target.Number = usedOutputNumbers.LowestAvailable;
					target.NumberPropertyLocation = Source.SystemGenerated;

					usedOutputNumbers.Add(target.Number);

					// If we're assigning it for the first time, purge it on the off chance that there's data left over from another
					// builder.
					outputNumbersToPurge.Add(target.Number);
					}
				}


			// Rebuild everything if there's an output target that didn't exist on the last run.

			foreach (var target in combinedConfig.OutputTargets)
				{
				bool foundMatch = false;

				if (previousConfig != null)
					{
					foreach (var previousTarget in previousConfig.OutputTargets)
						{
						if (previousTarget.IsSameTarget(target))
							{
							foundMatch = true;
							break;
							}
						}
					}

				if (foundMatch == false)
					{
					RebuildAllOutput = true;
					break;
					}
				}

				
			// Apply global settings.

			// DEPENDENCY: We assume all these settings are set in systemDefaultConfig so we don't have to worry about them being 
			// undefined.
			tabWidth = combinedConfig.TabWidth;
			documentedOnly = combinedConfig.DocumentedOnly;
			autoGroup = combinedConfig.AutoGroup;

			if (previousConfig == null ||
				tabWidth != previousConfig.TabWidth ||
				documentedOnly != previousConfig.DocumentedOnly ||
				autoGroup != previousConfig.AutoGroup)
				{
				ReparseEverything = true;
				}


			// Resave the configuration. Project_txt will handle skipping all the default and command line properties.

			projectTxtParser.Save(projectConfigFolder + "/Project.txt", combinedConfig, errorList);
			projectNDParser.Save(workingDataFolder + "/Project.nd", combinedConfig);
			
			
			// Create file sources and filters for Files.Manager
	
			foreach (var target in combinedConfig.InputTargets)
				{  Engine.Instance.Files.AddFileSource(CreateFileSource(target));  }

			foreach (var target in combinedConfig.FilterTargets)
				{  Engine.Instance.Files.AddFilter(CreateFilter(target));  }

			// Some people may put the output folder in their source folder.  Exclude it automatically.
			foreach (var target in combinedConfig.OutputTargets)
				{  
				var filter = CreateOutputFilter(target);

				if (filter != null)
					{  Engine.Instance.Files.AddFilter(filter);  }
				}
				
				
			// Create more default filters

			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(ProjectConfigFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(WorkingDataFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemConfigFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemStyleFolder) );
			
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolderRegex(new Regex.Config.DefaultIgnoredSourceFolderRegex()) );
			
			
			// Check all input folder entries against the filters.
			
			for (int i = 0; i < combinedConfig.InputTargets.Count; i++)
				{
				if (combinedConfig.InputTargets[i] is Targets.SourceFolder)
					{
					var sourceFolderTarget = (Targets.SourceFolder)combinedConfig.InputTargets[i];
					
					if (sourceFolderTarget.Type == Files.InputType.Source &&
						Engine.Instance.Files.SourceFolderIsIgnored(sourceFolderTarget.Folder))
						{
						errorList.Add(
							message: Locale.Get("NaturalDocs.Engine", "Error.SourceFolderIsIgnored(sourceFolder)", sourceFolderTarget.Folder),
							propertyLocation: sourceFolderTarget.FolderPropertyLocation,
							property: "InputTargets[" + i + "].Folder"
							);
							
						success = false;
						}
					}
				else
					{  throw new NotImplementedException();  }
				}
			

			// Create builders for Output.Manager
			
			foreach (var target in combinedConfig.OutputTargets)
				{
				// Merge the global project info so it has a complete configuration.  The configuration files have already been saved without it.
				MergeProjectInfo(target.ProjectInfo, combinedConfig.ProjectInfo);

				Engine.Instance.Output.AddBuilder(CreateBuilder(target));  
				}


			// Purge stray output working data, since otherwise it will be left behind if an output entry is removed.

			Regex.Config.OutputPathNumber outputPathNumberRegex = new Regex.Config.OutputPathNumber();
			bool raisedPossiblyLongOperationEvent = false;

			string[] outputDataFolders = System.IO.Directory.GetDirectories(workingDataFolder, "Output*", System.IO.SearchOption.TopDirectoryOnly);
			foreach (string outputDataFolder in outputDataFolders)
				{  
				System.Text.RegularExpressions.Match match = outputPathNumberRegex.Match(outputDataFolder);
				if (match.Success)
					{
					string numberString = match.Groups[1].ToString();
					int number;

					if (String.IsNullOrEmpty(numberString))
						{  number = 1;  }
					else
						{  number = int.Parse(numberString);  }

					if (outputNumbersToPurge.Contains(number) || !usedOutputNumbers.Contains(number))
						{  
						// Since we're deleting an entire folder, mark it as a possibly long operation.  Some output formats may create many
						// files in there which could take a while to clear out.
						if (!raisedPossiblyLongOperationEvent)
							{
							Engine.Instance.StartPossiblyLongOperation("PurgingOutputWorkingData");
							raisedPossiblyLongOperationEvent = true;
							}

						try
							{  System.IO.Directory.Delete(outputDataFolder, true);  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}
				}

			string[] outputDataFiles = System.IO.Directory.GetFiles(workingDataFolder, "Output*.nd", System.IO.SearchOption.TopDirectoryOnly);
			foreach (string outputDataFile in outputDataFiles)
				{  
				System.Text.RegularExpressions.Match match = outputPathNumberRegex.Match(outputDataFile);
				if (match.Success)
					{
					string numberString = match.Groups[1].ToString();
					int number;

					if (String.IsNullOrEmpty(numberString))
						{  number = 1;  }
					else
						{  number = int.Parse(numberString);  }

					if (outputNumbersToPurge.Contains(number) || !usedOutputNumbers.Contains(number))
						{  
						// Since this should just be a few individual files we don't have to worry about it being a possibly long operation,
						// although this will piggyback on that event if it was already raised.

						System.IO.File.Delete(outputDataFile);  
						}
					}
				}

			if (raisedPossiblyLongOperationEvent)
				{  Engine.Instance.EndPossiblyLongOperation();  }
				

			return success;
			}


		/* Function: MergeConfig
		 * 
		 * Merges the settings of the secondary configuration into the primary one.  The primary configuration will only adopt the secondary 
		 * settings which it does not already have set.  When merging you should start with your most important configuration and merge
		 * others into it in order of importance.
		 * 
		 * If the primary config has input targets, only unset properties from matching targets in the secondary config will be copied.  Any
		 * targets not appearing in the primary config will be ignored.  If there are no input targets in the primary config, they will be copied
		 * from the secondary config.
		 * 
		 * This system also applies for output and filter targets, with the exception of filter targets from a <Source.SystemDefault> config.
		 * Filters found in a <Source.SystemDefault> config will always be added to the primary config.
		 */
		public static void MergeConfig (ProjectConfig primaryConfig, ProjectConfig secondaryConfig)
			{

			// Project folders

			if (!primaryConfig.ProjectConfigFolderPropertyLocation.IsDefined)
				{
				primaryConfig.ProjectConfigFolder = secondaryConfig.ProjectConfigFolder;
				primaryConfig.ProjectConfigFolderPropertyLocation = secondaryConfig.ProjectConfigFolderPropertyLocation;
				}

			if (!primaryConfig.WorkingDataFolderPropertyLocation.IsDefined)
				{
				primaryConfig.WorkingDataFolder = secondaryConfig.WorkingDataFolder;
				primaryConfig.WorkingDataFolderPropertyLocation = secondaryConfig.WorkingDataFolderPropertyLocation;
				}


			// Global project info

			MergeProjectInfo(primaryConfig.ProjectInfo, secondaryConfig.ProjectInfo);


			// Input targets

			// Copy them from the secondary config if the primary config doesn't have any.  This allows things like having
			// input folders in Project.txt when there's none on the command line.
			if (primaryConfig.InputTargets.Count == 0)
				{
				foreach (var secondaryTarget in secondaryConfig.InputTargets)
					{
					primaryConfig.InputTargets.Add( secondaryTarget.Duplicate() );
					}
				}

			// If the primary config does have input targets, only copy secondary settings from matching targets.  This
			// allows things like having input folders specified on the command line but still being able to set the name and
			// number from Project.txt.  However, any targets in Project.txt that don't appear on the command line are ignored.
			// This allows people to change the command line and have it reflected immediately instead of having old sources
			// hang around until they're deleted from the command line AND Project.txt.
			else
				{
				foreach (var primaryTarget in primaryConfig.InputTargets)
					{
					foreach (var secondaryTarget in secondaryConfig.InputTargets)
						{
						if (secondaryTarget.IsSameTarget(primaryTarget))
							{
							MergeInputTargets(primaryTarget, secondaryTarget);
							break;
							}
						}
					}
				}


			// Filter targets

			// Copy them from the secondary config if the primary config doesn't have any.  This allows things like specifying
			// filters in Project.txt when there's none on the command line.  Also copy system default filters no matter what.
			if (primaryConfig.FilterTargets.Count == 0 ||
				secondaryConfig.Source == Source.SystemDefault)
				{
				foreach (var secondaryTarget in secondaryConfig.FilterTargets)
					{
					primaryConfig.FilterTargets.Add( secondaryTarget.Duplicate() );
					}
				}
			// Filter targets don't have secondary properties to merge so there's nothing to do if the primary config already had
			// some.  We don't combine them because we want changes to the command line reflected immediately instead of
			// getting tattooed in Project.txt until it's deleted there too.


			// Output targets

			// Output targets follow the same logic and behavior as input targets.
			if (primaryConfig.OutputTargets.Count == 0)
				{
				foreach (var secondaryTarget in secondaryConfig.OutputTargets)
					{
					primaryConfig.OutputTargets.Add( secondaryTarget.Duplicate() );
					}
				}
			else
				{
				foreach (var primaryTarget in primaryConfig.OutputTargets)
					{
					foreach (var secondaryTarget in secondaryConfig.OutputTargets)
						{
						if (secondaryTarget.IsSameTarget(primaryTarget))
							{
							MergeOutputTargets(primaryTarget, secondaryTarget);
							break;
							}
						}
					}
				}


			// Global settings

			if (!primaryConfig.TabWidthPropertyLocation.IsDefined)
				{
				primaryConfig.TabWidth = secondaryConfig.TabWidth;
				primaryConfig.TabWidthPropertyLocation = secondaryConfig.TabWidthPropertyLocation;
				}

			if (!primaryConfig.DocumentedOnlyPropertyLocation.IsDefined)
				{
				primaryConfig.DocumentedOnly = secondaryConfig.DocumentedOnly;
				primaryConfig.DocumentedOnlyPropertyLocation = secondaryConfig.DocumentedOnlyPropertyLocation;
				}

			if (!primaryConfig.AutoGroupPropertyLocation.IsDefined)
				{
				primaryConfig.AutoGroup = secondaryConfig.AutoGroup;
				primaryConfig.AutoGroupPropertyLocation = secondaryConfig.AutoGroupPropertyLocation;
				}

			}


		/* Function: MergeProjectInfo
		 * 
		 * Merges the settings of the secondary <ProjectInfo> into the primary one.  The primary <ProjectInfo> will only adopt the secondary 
		 * settings which it does not already have set.  When merging you should start with your most important configuration and merge
		 * others into it in order of importance.
		 */
		protected static void MergeProjectInfo (ProjectInfo primaryProjectInfo, ProjectInfo secondaryProjectInfo)
			{
			if (!primaryProjectInfo.TitlePropertyLocation.IsDefined)
				{
				primaryProjectInfo.Title = secondaryProjectInfo.Title;
				primaryProjectInfo.TitlePropertyLocation = secondaryProjectInfo.TitlePropertyLocation;

				// If we're using the secondary project info's title, we want to use its subtitle as well, even if it's undefined.  Setting only the
				// title should reset the subtitle.
				if (!primaryProjectInfo.SubtitlePropertyLocation.IsDefined)
					{
					primaryProjectInfo.Subtitle = secondaryProjectInfo.Subtitle;
					primaryProjectInfo.SubtitlePropertyLocation = secondaryProjectInfo.SubtitlePropertyLocation;
					}
				}

			if (!primaryProjectInfo.CopyrightPropertyLocation.IsDefined)
				{
				primaryProjectInfo.Copyright = secondaryProjectInfo.Copyright;
				primaryProjectInfo.CopyrightPropertyLocation = secondaryProjectInfo.CopyrightPropertyLocation;
				}

			if (!primaryProjectInfo.TimeStampCodePropertyLocation.IsDefined)
				{
				primaryProjectInfo.TimeStampCode = secondaryProjectInfo.TimeStampCode;
				primaryProjectInfo.TimeStampCodePropertyLocation = secondaryProjectInfo.TimeStampCodePropertyLocation;
				}

			if (!primaryProjectInfo.StyleNamePropertyLocation.IsDefined)
				{
				primaryProjectInfo.StyleName = secondaryProjectInfo.StyleName;
				primaryProjectInfo.StyleNamePropertyLocation = secondaryProjectInfo.StyleNamePropertyLocation;
				}
			}


		/* Function: MergeInputTargets
		 * 
		 * Merges the settings of the secondary input target into the primary one.  The primary target will only adopt the secondary 
		 * settings which it does not already have set.  When merging you should start with your most important target and merge
		 * others into it in order of importance.
		 * 
		 * It is assumed that the two targets are of the same class and match with <Targets.InputBase.IsSameTarget()>.
		 */
		protected static void MergeInputTargets (Targets.InputBase primaryTarget, Targets.InputBase secondaryTarget)
			{
			#if DEBUG
			if (primaryTarget.GetType() != secondaryTarget.GetType())
				{  throw new Exception ("Cannot call MergeInputTargets() on two different types.");  }
			if (primaryTarget.IsSameTarget(secondaryTarget) == false)
				{  throw new Exception ("Cannot call MergeInputTargets() when they do not match with IsSameTarget().");  }
			#endif

			if (!primaryTarget.NamePropertyLocation.IsDefined)
			    {
			    primaryTarget.Name = secondaryTarget.Name;
			    primaryTarget.NamePropertyLocation = secondaryTarget.NamePropertyLocation;
			    }
			if (!primaryTarget.NumberPropertyLocation.IsDefined)
			    {
			    primaryTarget.Number = secondaryTarget.Number;
			    primaryTarget.NumberPropertyLocation = secondaryTarget.NumberPropertyLocation;
			    }
			if (!primaryTarget.TypePropertyLocation.IsDefined)
			    {
			    primaryTarget.Type = secondaryTarget.Type;
			    primaryTarget.TypePropertyLocation = secondaryTarget.TypePropertyLocation;
			    }
				

			if (primaryTarget is Targets.SourceFolder)
				{
				if (!(primaryTarget as Targets.SourceFolder).FolderPropertyLocation.IsDefined)
					{
					(primaryTarget as Targets.SourceFolder).Folder = (secondaryTarget as Targets.SourceFolder).Folder;
					(primaryTarget as Targets.SourceFolder).FolderPropertyLocation = (secondaryTarget as Targets.SourceFolder).FolderPropertyLocation;
					}
				}
			else
				{  throw new NotImplementedException();  }
			}


		/* Function: MergeOutputTargets
		 * 
		 * Merges the settings of the secondary output target into the primary one.  The primary target will only adopt the secondary 
		 * settings which it does not already have set.  When merging you should start with your most important target and merge
		 * others into it in order of importance.
		 * 
		 * It is assumed that the two targets are of the same class and match with <Targets.OutputBase.IsSameTarget()>.
		 */
		protected static void MergeOutputTargets (Targets.OutputBase primaryTarget, Targets.OutputBase secondaryTarget)
			{
			#if DEBUG
			if (primaryTarget.GetType() != secondaryTarget.GetType())
				{  throw new Exception ("Cannot call MergeOutputTargets() on two different types.");  }
			if (primaryTarget.IsSameTarget(secondaryTarget) == false)
				{  throw new Exception ("Cannot call MergeOutputTargets() when they do not match with IsSameTarget().");  }
			#endif

			MergeProjectInfo(primaryTarget.ProjectInfo, secondaryTarget.ProjectInfo);

			if (!primaryTarget.NumberPropertyLocation.IsDefined)
			    {
			    primaryTarget.Number = secondaryTarget.Number;
			    primaryTarget.NumberPropertyLocation = secondaryTarget.NumberPropertyLocation;
			    }
				

			if (primaryTarget is Targets.HTMLOutputFolder)
				{
				if (!(primaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation.IsDefined)
					{
					(primaryTarget as Targets.HTMLOutputFolder).Folder = (secondaryTarget as Targets.HTMLOutputFolder).Folder;
					(primaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation = (secondaryTarget as Targets.HTMLOutputFolder).FolderPropertyLocation;
					}
				}
			else
				{  throw new NotImplementedException();  }
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CreateFileSource
		 * Creates and returns a <Files.FileSource> from the passed input target config.
		 */
		protected virtual Files.FileSource CreateFileSource (Targets.InputBase target)
			{
			if (target is Targets.SourceFolder)
				{  return new Files.FileSources.Folder((Targets.SourceFolder)target);  }
			else
				{  throw new NotImplementedException();  }
			}

		/* Function: CreateFilter
		 * Creates and returns a <Files.Filter> from the passed target config.
		 */
		protected virtual Files.Filter CreateFilter (Targets.FilterBase target)
			{
			if (target is Targets.IgnoredSourceFolder)
				{  return new Files.Filters.IgnoredSourceFolder((target as Targets.IgnoredSourceFolder).Folder);  }
			else if (target is Targets.IgnoredSourceFolderPattern)
				{  return new Files.Filters.IgnoredSourceFolderPattern((target as Targets.IgnoredSourceFolderPattern).Pattern);  }
			else
				{  throw new NotImplementedException();  }
			}

		/* Function: CreateOutputFilter
		 * Creates and returns a <Files.Filter> from the passed output target so that the target's files are excluded from being
		 * scanned with the source, or null if one isn't needed.
		 */
		protected virtual Files.Filter CreateOutputFilter (Targets.OutputBase target)
			{
			if (target is Targets.HTMLOutputFolder)
				{  return new Files.Filters.IgnoredSourceFolder((target as Targets.HTMLOutputFolder).Folder);  }
			else
				{  throw new NotImplementedException();  }
			}

		/* Function: CreateBuilder
		 * Creates and returns an <Output.Builder> from the passed target config.
		 */
		protected virtual Output.Builder CreateBuilder (Targets.OutputBase target)
			{
			if (target is Targets.HTMLOutputFolder)
				{  return new Output.Builders.HTML((Targets.HTMLOutputFolder)target);  }
			else
				{  throw new NotImplementedException();  }
			}


	
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: ProjectConfigFolder
		 * 
		 * The project configuration folder's absolute <Path>, formerly called the project folder.  This must be 
		 * defined before <Start()> will succeed.  If it is set to a relative path, it will be converted to absolute 
		 * with the current working folder.  Once <Start()> is called it cannot be changed.
		 */
		public Path ProjectConfigFolder
			{
			get
				{  return projectConfigFolder;  }
			}
			
		
		/* Property: SystemConfigFolder
		 * The system configuration folder's absolute <Path>.
		 */
		public Path SystemConfigFolder
			{
			get
				{  
				return Path.FromAssembly( System.Reflection.Assembly.GetExecutingAssembly() ).ParentFolder + "/Config";  
				}
			}
			
			
		/* Property: SystemStyleFolder
		 * The system style folder's absolute <Path>.  All styles will be subfolders of this one.
		 */
		public Path SystemStyleFolder
			{
			get
				{  
				return Path.FromAssembly( System.Reflection.Assembly.GetExecutingAssembly() ).ParentFolder + "/Styles";  
				}
			}
			
			
		/* Property: WorkingDataFolder
		 * The working data folder's absolute <Path>.  Optional to set.  Once <Start()> is called it cannot
		 * be changed.
		 */
		public Path WorkingDataFolder
			{
			get
				{  return workingDataFolder;  }
			}
			

		/* Property: ReparseEverything
		 * 
		 * If set, all source files are going to be reparsed.  Modules *MUST* check this and rebuild their data files from scratch
		 * if it's set.  This is important because this gets set if certain data files are corrupted (such as <Languages.nd>) and thus
		 * various numeric IDs are not guaranteed to mean the same thing they did on the last run.
		 * 
		 * It is only possible to change this property to true.  You cannot turn it off once it's on.
		 */
		public bool ReparseEverything
			{
			get
				{  return reparseEverything;  }
			set
				{
				if (value == true)
					{  reparseEverything = true;  }
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: RebuildAllOutput
		 * 
		 * If set, all output is going to be regenerated.
		 * 
		 * It is only possible to change this property to true.  You cannot turn it off once it's on.
		 */
		public bool RebuildAllOutput
			{
			get
				{  return rebuildAllOutput;  }
			set
				{
				if (value == true)
					{  
					rebuildAllOutput = true;  
					reparseEverything = true; //xxx until rebuildAllOutput is supported
					}
				else
					{  throw new InvalidOperationException();  }
				}
			}
			
			
		/* Property: TabWidth
		 * The number of spaces a tab character should be expanded to.
		 */
		public int TabWidth
			{
			get
				{  return tabWidth;  }
			}


		/* Property: DocumentedOnly
		 * Whether only documented code elements should appear in the output.
		 */
		public bool DocumentedOnly
			{
			get
				{  return documentedOnly;  }
			}


		/* Property: AutoGroup
		 * Whether automatic grouping should be applied.
		 */
		public bool AutoGroup
			{
			get
				{  return autoGroup;  }
			}


		/* Property: BackgroundThreadsPerTask
		 * The number of threads Natural Docs should use for each parallelizable background task.
		 */
		public int BackgroundThreadsPerTask
			{
			get
				{  return backgroundThreadsPerTask;  }
			set
				{
				if (value < 1)
					{  throw new InvalidOperationException();  }
					
				backgroundThreadsPerTask = value;
				}
			}


		/* Function: OutputWorkingDataFileOf
		 * Returns the working data file path for the passed output entry number.  It's up to the output entry whether
		 * it wants to actually create and use a file at this path, this just makes sure it has its own unique path.
		 */
		public Path OutputWorkingDataFileOf (int number)
			{
			return workingDataFolder + "/Output" + (number == 1 ? "" : number.ToString()) + ".nd";
			}
			
			
		/* Function: OutputWorkingDataFolderOf
		 * Returns the working data folder path for the passed output entry number.  It's up to the output entry whether
		 * it wants to actually create and use files in this folder, this just makes sure it has its own unique path.
		 */
		public Path OutputWorkingDataFolderOf (int number)
			{
			return workingDataFolder + "/Output" + (number == 1 ? "" : number.ToString());
			}
			
			

		// Group: Static Properties
		// __________________________________________________________________________
		
		
		/* Property: SystemDefaultConfig
		 * The system defaults that should be combined with user <ProjectConfigs>.
		 */
		static public ProjectConfig SystemDefaultConfig
			{
			get
				{  return systemDefaultConfig;  }
			}


		/* Property: UsingUnix
		 * True if the program is running in Unix.
		 */
		static public bool UsingUnix
			{
			get
				{
				// Early versions of Mono returned 128 as the value, whereas PlatformID.Unix is 4.
				// There's also now OS X which is 6.
				return ( System.Environment.OSVersion.Platform == PlatformID.Unix ||
							  System.Environment.OSVersion.Platform == PlatformID.MacOSX ||
							  (int)System.Environment.OSVersion.Platform == 128);
				}
			}
			
			
		/* Property: PathSeparatorCharacter
		 * The path separator character for the current platform, such as slash or backslash.
		 */
		static public char PathSeparatorCharacter
			{
			get
				{ 
				if (UsingUnix == true)
					{  return '/';  }
				else  // all others are variants of Windows
					{  return '\\';  }
				}
			}
			
			
		/* Property: IgnoreCaseInPaths
		 * Whether paths are case sensitive on the current platform.
		 */
		static public bool IgnoreCaseInPaths
			{
			get
				{
				if (UsingUnix == true)
					{  return false;  }
				else  // all others are variants of Windows
					{  return true;  }
				}
			}


		/* Property: KeySettingsForPaths
		 * The <Collections.KeySettings> that should be used when using paths as a key.  It will apply <IgnoreCaseInPaths>.
		 */
		static public Collections.KeySettings KeySettingsForPaths
			{
			get
				{
				if (IgnoreCaseInPaths)
					{  return Collections.KeySettings.IgnoreCase;  }
				else
					{  return Collections.KeySettings.Literal;  }
				}
			}


			
		// Group: Variables
		// __________________________________________________________________________
		
		
		/* String: projectConfigFolder
		 * The project configuration folder <Path>.  It will always be absolute.
		 */
		protected Path projectConfigFolder;
		
		/* String: workingDataFolder
		 * The working data folder <Path>, formerly always a subfolder of the project folder.  It will always
		 * be absolute.
		 */
		protected Path workingDataFolder;
		
		/* var: tabWidth
		 * The number of spaces tabs should be expanded to.
		 */
		protected int tabWidth;

		/* var: documentedOnly
		 * Whether only documented code elements should appear in the output.
		 */
		protected bool documentedOnly;

		/* var: autoGroup
		 * Whether automatic grouping should be applied.
		 */
		protected bool autoGroup;

		/* bool: reparseEverything
		 * Whether all source files should be reparsed.
		 */
		protected bool reparseEverything;
		
		/* bool: rebuildAllOutput
		 * Whether all output should be recreated from scatch.
		 */
		protected bool rebuildAllOutput;
		
		/* int: backgroundThreadsPerTask
		 */
		protected int backgroundThreadsPerTask;



		// Group: Static Variables
		// __________________________________________________________________________

		static protected ProjectConfig systemDefaultConfig;


		// Group: Constants
		// __________________________________________________________________________

		public const int DefaultTabWidth = 4;
		
		}
	}