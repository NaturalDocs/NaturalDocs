/* 
 * Class: GregValure.NaturalDocs.Engine.Config.Manager
 * ____________________________________________________________________________
 * 
 * A class to manage the engine's configuration.
 * 
 * 
 * Topic: Usage
 * 
 *		- Set configuration options as desired through properties like <ProjectConfigFolder> and <CommandLineConfig>.
 *			At minimum, <ProjectConfigFolder> must be set.  All other settings are optional.
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
		public Manager ()
			{  
			projectConfigFolder = null;
			workingDataFolder = null;
			
			tabWidth = DefaultTabWidth;
			documentedOnly = false;
			autoGroup = true;
			commandLineConfig = new ConfigData();

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
		 * After <Start()> is called the properties of this class become read-only.  This function will add all input entries and
		 * filters it has to <Files.Manager>, and all output entries to <Output.Manager>.
		 */
		public bool Start (ErrorList errorList)
			{
			bool success = true;
			
			
			// Validate project config folder
				
			if (String.IsNullOrEmpty(projectConfigFolder))
				{
				errorList.Add( 
					Locale.Get("NaturalDocs.Engine", "Error.NoProjectConfigFolder")
					);
					
				success = false;
				}
				
			else if (!System.IO.Directory.Exists(projectConfigFolder))
				{
				errorList.Add(
					Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderDoesntExist(name)", projectConfigFolder)
					);
					
				success = false;
				}
				
			else if (projectConfigFolder == SystemConfigFolder)
				{
				errorList.Add( 
					Locale.Get("NaturalDocs.Engine", "Error.ProjectConfigFolderCannotEqualSystemConfigFolder")
					);
					
				success = false;
				}

			if (success == false)
				{  return false;  }
				

			// Validate the working data folder, creating one if it doesn't exist.

			if (String.IsNullOrEmpty(workingDataFolder))
				{  workingDataFolder = projectConfigFolder + "/Working Data";  }

			if (!System.IO.Directory.Exists(workingDataFolder))
				{
				try
					{  System.IO.Directory.CreateDirectory(workingDataFolder);  }
				catch
					{
					errorList.Add(
						Locale.Get("NaturalDocs.Engine", "Error.CantCreateWorkingDataFolder(name)", workingDataFolder)
						);
					
					success = false;
					}
				}
				
			if (success == false)
				{  return false;  }
				
				
			// Load configuration files
			
			ConfigData configFileData = null;
			Path configFilePath = ProjectConfigFolder + "/Project.txt";
			ConfigFileParser configFileParser = CreateConfigFileParser();
			
			if (System.IO.File.Exists(configFilePath))
				{
				if (configFileParser.LoadFile(configFilePath, errorList))
					{  configFileData = configFileParser.ParsedData;  }
				else
					{  success = false;  }
				}
			else if (System.IO.File.Exists(ProjectConfigFolder + "/Menu.txt"))
				{
				// Try to extract information from a pre-2.0 Menu.txt instead.
				if (configFileParser.LoadOldMenuFile(ProjectConfigFolder + "/Menu.txt"))
					{  configFileData = configFileParser.ParsedData;  }
				else
					{  success = false;  }
				}
			else
				{
				// If neither file exists it's not an error condition.  Just treat it as if nothing was defined.
				configFileData = new ConfigData();
				}
				
			ConfigData binaryConfigFileData = null;
			Path binaryConfigFilePath = WorkingDataFolder + "/Project.nd";

			if (ReparseEverything == false && configFileParser.LoadBinaryFile(binaryConfigFilePath))
				{  binaryConfigFileData = configFileParser.ParsedData;  }
			else
				{  binaryConfigFileData = new ConfigData();  }
				
			if (success == false)
				{  return false;  }
				

			// We're using the command line as our authoritative settings and merging Project.txt into it.  If there's entries of a certain
			// type on the command line, we only copy secondary settings from matching entries in Project.txt and discard the rest.  If
			// the command line doesn't contain a certain type of entry, they get copied in directly from Project.txt.

			int inputEntries = 0;
			int outputEntries = 0;
			int filterEntries = 0;

			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is InputEntry)
					{  inputEntries++;  }
				else if (entry is OutputEntry)
					{  outputEntries++;  }
				else if (entry is FilterEntry)
					{  filterEntries++;  }
				}
				
			bool commandLineHasInputEntries = (inputEntries > 0);
			bool commandLineHasOutputEntries = (outputEntries > 0);
			bool commandLineHasFilterEntries = (filterEntries > 0);

			foreach (Entry configFileEntry in configFileData.Entries)
				{
				if ((configFileEntry is InputEntry && commandLineHasInputEntries == true) ||
					 (configFileEntry is OutputEntry && commandLineHasOutputEntries == true) ||
					 (configFileEntry is FilterEntry && commandLineHasFilterEntries == true) )
					{
					// Only merge secondary settings if the command line already has entries of this type.  
					// If it doesn't match a command line entry it's discarded.
					foreach (Entry entry in commandLineConfig.Entries)
						{
						if (entry.IsSameFundamentalEntry(configFileEntry))
							{  
							entry.CopyUnsetPropertiesFrom(configFileEntry);
							break;
							}
						}
					}
				else
					{
					// If the command line doesn't have entries of this type, copy it in.
					commandLineConfig.Entries.Add(configFileEntry);

					if (configFileEntry is InputEntry)
						{  inputEntries++;  }
					else if (configFileEntry is OutputEntry)
						{  outputEntries++;  }
					else if (configFileEntry is FilterEntry)
						{  filterEntries++;  }
					}
				}


			// Merge output entry numbers from Project.nd into the settings.  These are not stored in Project.txt because they're
			// pretty expendible.

			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is OutputEntry)
					{
					foreach (Entry binaryConfigFileEntry in binaryConfigFileData.Entries)
						{
						if (entry.IsSameFundamentalEntry(binaryConfigFileEntry))
							{
							(entry as OutputEntry).Number = (binaryConfigFileEntry as OutputEntry).Number;
							break;
							}
						}
					}
				}

				
			// Entry validation
						
			if (inputEntries < 1)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.NoInputEntries") );
				success = false;
				}
			if (outputEntries < 1)
				{
				errorList.Add( Locale.Get("NaturalDocs.Engine", "Error.NoOutputEntries") );
				success = false;
				}

			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry.Validate(errorList) == false)
					{  success = false;  }
				}

			if (success == false)
				{  return false;  }


			// Determine the entry numbers that are already used and reset duplicates.
			
			IDObjects.NumberSet usedSourceNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet usedImageNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet usedOutputNumbers = new IDObjects.NumberSet();
			IDObjects.NumberSet outputNumbersToPurge = new IDObjects.NumberSet();
			
			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is InputEntry)
					{
					InputEntry inputEntry = (InputEntry)entry;
					
					if (inputEntry.Number != 0)
						{
						if (inputEntry.InputType == Files.InputType.Source)
							{
							if (usedSourceNumbers.Contains(inputEntry.Number))
								{  inputEntry.Number = 0;  }
							else
								{  usedSourceNumbers.Add(inputEntry.Number);  }
							}
							
						else if (inputEntry.InputType == Files.InputType.Image)
							{
							if (usedImageNumbers.Contains(inputEntry.Number))
								{  inputEntry.Number = 0;  }
							else
								{  usedImageNumbers.Add(inputEntry.Number);  }
							}
						}
					}

				else if (entry is OutputEntry)
					{
					OutputEntry outputEntry = (OutputEntry)entry;
					
					if (outputEntry.Number != 0)
						{
						if (usedOutputNumbers.Contains(outputEntry.Number))
							{  
							outputEntry.Number = 0;

							// Since we don't know which of the two entries generated working data under this number, purge it to be safe.
							outputNumbersToPurge.Add(outputEntry.Number);
							}
						else
							{  usedOutputNumbers.Add(outputEntry.Number);  }
						}
					}
				}
				

			// Assign numbers to the entries that don't already have them and generate default input folder names.

			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is InputEntry)
					{
					InputEntry inputEntry = (InputEntry)entry;
					
					if (inputEntry.InputType == Files.InputType.Source)
						{
						if (inputEntry.Number == 0)
							{
							inputEntry.Number = usedSourceNumbers.LowestAvailable;
							usedSourceNumbers.Add(inputEntry.Number);
							}
							
						if (inputEntry.Name == null && inputEntries > 1)
							{
							inputEntry.GenerateDefaultName();
							}
						}
						
					else if (inputEntry.InputType == Files.InputType.Image)
						{
						if (inputEntry.Number == 0)
							{
							inputEntry.Number = usedImageNumbers.LowestAvailable;
							usedImageNumbers.Add(inputEntry.Number);
							}
						}
					}

				else if (entry is OutputEntry)
					{
					OutputEntry outputEntry = (OutputEntry)entry;

					if (outputEntry.Number == 0)
						{
						outputEntry.Number = usedOutputNumbers.LowestAvailable;
						usedOutputNumbers.Add(outputEntry.Number);

						// If we're assigning it for the first time, purge it on the off chance that there's data left over from another
						// builder.
						outputNumbersToPurge.Add(outputEntry.Number);
						}
					}
				}


			// Rebuild everything if there's output entries that didn't exist on the last run.

			int outputEntryMatches = 0;
				
			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is OutputEntry)
					{
					foreach (Entry binaryConfigFileEntry in binaryConfigFileData.Entries)
						{
						if (binaryConfigFileEntry.IsSameFundamentalEntry(entry))
							{
							outputEntryMatches++;
							break;
							}
						}
					}
				}
					
			if (outputEntries > outputEntryMatches)
				{  
				RebuildAllOutput = true;
				}

				
			// Apply global settings.

			if (commandLineConfig.TabWidth != 0)
				{  tabWidth = commandLineConfig.TabWidth;  }
			else if (configFileData.TabWidth != 0)
				{  tabWidth = configFileData.TabWidth;  }
			else
				{  tabWidth = DefaultTabWidth;  }

			if (commandLineConfig.DocumentedOnly != null)
				{  documentedOnly = (bool)commandLineConfig.DocumentedOnly;  }
			else if (configFileData.DocumentedOnly != null)
				{  documentedOnly = (bool)configFileData.DocumentedOnly;  }
			else
				{  documentedOnly = false;  }

			if (commandLineConfig.AutoGroup != null)
				{  autoGroup = (bool)commandLineConfig.AutoGroup;  }
			else if (configFileData.AutoGroup != null)
				{  autoGroup = (bool)configFileData.AutoGroup;  }
			else
				{  autoGroup = true;  }

			if (tabWidth != binaryConfigFileData.TabWidth ||
				documentedOnly != binaryConfigFileData.DocumentedOnly ||
				autoGroup != binaryConfigFileData.AutoGroup)
				{
				ReparseEverything = true;
				}


			// Merge the rest of Project.txt's settings into the command line config and save it as the new Project.txt.
			
			commandLineConfig.ProjectInfo.CopyUnsetPropertiesFrom(configFileData.ProjectInfo);

			// Go back to the config file's version before saving because we don't want the command line settings to get tattooed
			// into place.  If adding --no-auto-group on the command line caused it to be added to Project.txt, removing it from the
			// command line would have no effect because it's still in Project.txt.
			commandLineConfig.TabWidth = configFileData.TabWidth;
			commandLineConfig.DocumentedOnly = configFileData.DocumentedOnly;
			commandLineConfig.AutoGroup = configFileData.AutoGroup;

			configFileParser.SaveFile(configFilePath, commandLineConfig, errorList);

			// Now apply the correct config again because we'll be saving this later as Project.nd.
			commandLineConfig.TabWidth = tabWidth;
			commandLineConfig.DocumentedOnly = documentedOnly;
			commandLineConfig.AutoGroup = autoGroup;


			// Use the default project info to fill in anything that wasn't overridden in each output entry

			if (commandLineConfig.ProjectInfo.StyleName == null)
				{  commandLineConfig.ProjectInfo.StyleName = "Default";  }

			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is OutputEntry)
					{
					(entry as OutputEntry).ProjectInfo.CopyUnsetPropertiesFrom(commandLineConfig.ProjectInfo);
					}
				}


			// Save the final configuration in Project.nd

			configFileParser.SaveBinaryFile(binaryConfigFilePath, commandLineConfig);
			
			
			// Create file sources and filters for Files.Manager
	
			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is InputEntry)
					{  Engine.Instance.Files.AddFileSource(CreateFileSource((InputEntry)entry));  }
				else if (entry is FilterEntry)
					{  Engine.Instance.Files.AddFilter(CreateFilter((FilterEntry)entry));  }

				// Some people may put the output folder in their source folder.  Exclude it automatically.
				else if (entry is OutputEntry)
					{  Engine.Instance.Files.AddFilter(CreateOutputFilter((OutputEntry)entry));  }
				}
				
				
			// Create more default filters

			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(ProjectConfigFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(WorkingDataFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemConfigFolder) );
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolder(SystemStyleFolder) );
			
			Engine.Instance.Files.AddFilter( new Engine.Files.Filters.IgnoredSourceFolderRegex(new Regex.Config.DefaultIgnoredSourceFolderRegex()) );
			
			
			// Check all input folder entries against the filters.
			
			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is Entries.InputFolder)
					{
					Entries.InputFolder inputFolder = (Entries.InputFolder)entry;
					
					if (inputFolder.InputType == Files.InputType.Source &&
						Engine.Instance.Files.SourceFolderIsIgnored(inputFolder.Folder))
						{
						errorList.Add(
							Locale.Get("NaturalDocs.Engine", "Error.SourceFolderIsIgnored(sourceFolder)", inputFolder.Folder)
							);
							
						success = false;
						}
					}
				}
			

			// Create builders for Output.Manager
			
			foreach (Entry entry in commandLineConfig.Entries)
				{
				if (entry is OutputEntry)
					{  
					Engine.Instance.Output.AddBuilder( CreateBuilder((OutputEntry)entry) );
					}
				}


			// Purge stray output working data, since otherwise it will be left behind if an output entry is removed.

			Regex.Config.OutputPathNumber outputPathNumberRegex = new Regex.Config.OutputPathNumber();
			bool raisedPossiblyLongOperationEvent = false;

			string[] outputDataFolders = System.IO.Directory.GetDirectories(workingDataFolder, "Output*", 
																																System.IO.SearchOption.TopDirectoryOnly);
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

			string[] outputDataFiles = System.IO.Directory.GetFiles(workingDataFolder, "Output*.nd", 
																												System.IO.SearchOption.TopDirectoryOnly);
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



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: CreateConfigFileParser
		 * Creates and returns a <ConfigFileParser> object for use with <Project.txt> and <Project.nd>.  This is
		 * a separate function to allow a substitute class to be used in derived modules.
		 */
		protected virtual ConfigFileParser CreateConfigFileParser ()
			{
			return new ConfigFileParser();
			}

		/* Function: CreateFileSource
		 * Creates and returns a <Files.FileSource> from the passed <InputEntry>.
		 */
		protected virtual Files.FileSource CreateFileSource (InputEntry entry)
			{
			if (entry is Entries.InputFolder)
				{  return new Files.FileSources.Folder((Entries.InputFolder)entry);  }
			else
				{  throw new Exception("Can't create file source from " + entry.GetType().Name);  }
			}

		/* Function: CreateFilter
		 * Creates and returns a <Files.Filter> from the passed <FilterEntry>.
		 */
		protected virtual Files.Filter CreateFilter (FilterEntry entry)
			{
			if (entry is Entries.IgnoredSourceFolder)
				{  return new Files.Filters.IgnoredSourceFolder( (entry as Entries.IgnoredSourceFolder).Folder );  }
			else if (entry is Entries.IgnoredSourceFolderPattern)
				{  return new Files.Filters.IgnoredSourceFolderPattern( (entry as Entries.IgnoredSourceFolderPattern).Pattern );  }
			else
				{  throw new Exception("Can't create filter from " + entry.GetType().Name);  }
			}

		/* Function: CreateOutputFilter
		 * Creates and returns a <Files.Filter> from the passed <OutputEntry> so that the output folder is excluded from being
		 * scanned with the source.
		 */
		protected virtual Files.Filter CreateOutputFilter (OutputEntry entry)
			{
			return new Files.Filters.IgnoredSourceFolder(entry.Folder);
			}

		/* Function: CreateBuilder
		 * Creates and returns an <Output.Builder> from the passed <OutputEntry>.
		 */
		protected virtual Output.Builder CreateBuilder (OutputEntry entry)
			{
			if (entry is Entries.HTMLOutputFolder)
				{  return new Output.Builders.HTML((Entries.HTMLOutputFolder)entry);  }
			else if (entry is Entries.XMLOutputFolder)
				{  return new Output.Builders.XML((Entries.XMLOutputFolder)entry);  }
			else
				{  throw new Exception("Can't create builder from " + entry.GetType().Name);  }
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
			set
				{  
				if (value == null || value.IsAbsolute)
					{  projectConfigFolder = value;  }				
				else
					{  projectConfigFolder = System.Environment.CurrentDirectory + "/" + value;  }
				}
			}
			
		
		/* Property: SystemConfigFolder
		 * The system configuration folder's absolute <Path>.
		 */
		public Path SystemConfigFolder
			{
			get
				{  
				// Including /../ because it returns the file name as well.  The Path class will simplify it out.
				return Path.GetExecutingAssembly() + "/../Config";  
				}
			}
			
			
		/* Property: SystemStyleFolder
		 * The system style folder's absolute <Path>.  All styles will be subfolders of this one.
		 */
		public Path SystemStyleFolder
			{
			get
				{  
				// Including /../ because it returns the file name as well.  The Path class will simplify it out.
				return Path.GetExecutingAssembly() + "/../Styles";  
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
			set
				{  
				if (value == null || value.IsAbsolute)
					{  workingDataFolder = value;  }				
				else
					{  workingDataFolder = System.Environment.CurrentDirectory + "/" + value;  }
				}
			}
			

		/* Property: CommandLineConfig
		 * The command line settings equivalent to those found in <Project.txt>.  Edit its properties prior to calling 
		 * <Start()>, the object will already exist.  Will be null afterwards.
		 */
		public ConfigData CommandLineConfig
			{
			get
				{  return commandLineConfig;  }
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
		
		/* var: commandLineConfig
		 * Prior to <Start()> this represents all settings passed in from the command line.  Afterwards it will be null.
		 */
		protected ConfigData commandLineConfig;

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


		// Group: Constants
		// __________________________________________________________________________
		
		public const int DefaultTabWidth = 4;
		
		}
	}