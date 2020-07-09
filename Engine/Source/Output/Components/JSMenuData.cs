/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Components.JSMenuData
 * ____________________________________________________________________________
 * 
 * A helper class to build the JavaScript menu data for <Output.Builders.HTML>.  See <JavaScript Menu Data> 
 * for the output format.
 * 
 * 
 * Usage:
 * 
 *		- Fill in the menu data as described in the documentation for <Menu>.
 *		- Call <Build()>.
 *		- Ta da.
 *	
 * 
 * Threading: Not Thread Safe
 * 
 *		This class is only designed to be used by one thread at a time.  It has an internal state that is used during a call to
 *		<Build()>, and another <Build()> should not be started until it's completed.  Instead each thread should create its 
 *		own object.
 *		
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Output.HTML.Components.MenuEntries;


namespace CodeClear.NaturalDocs.Engine.Output.Components
	{
	public class JSMenuData : Output.HTML.Components.Menu
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSMenuData
		 */
		public JSMenuData (Builders.HTML htmlBuilder) : base (htmlBuilder)
			{
			}


		/* Function: Build
		 * Generates JSON files for all entries in the menu.  It returns a <StringTable> mapping the file type strings ("files", 
		 * "classes", etc.) to a <IDObjects.NumberSet> representing all the files that were generated.  So "files.js", "files2.js",
		 * and "files3.js" would map to "files" -> {1-3}.
		 */
		public StringTable<IDObjects.NumberSet> Build ()
			{
			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory(HTML.Paths.Menu.OutputFolder(HTMLBuilder.OutputFolder));  
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)", 
									HTML.Paths.Menu.OutputFolder(HTMLBuilder.OutputFolder), e.Message) 
					);
				}


			// Build menu files

			StringTable<IDObjects.NumberSet> outputFiles = new StringTable<IDObjects.NumberSet>();

			if (RootFileMenu != null)
				{  
				GenerateJSON(RootFileMenu);
				SegmentMenu(RootFileMenu, "files", ref outputFiles);
				BuildOutput(RootFileMenu);
				}

			if (RootClassMenu != null)
				{
				GenerateJSON(RootClassMenu);
				SegmentMenu(RootClassMenu, "classes", ref outputFiles);
				BuildOutput(RootClassMenu);
				}

			if (RootDatabaseMenu != null)
				{
				GenerateJSON(RootDatabaseMenu);
				SegmentMenu(RootDatabaseMenu, "database", ref outputFiles);
				BuildOutput(RootDatabaseMenu);
				}


			// Build tab information file

			StringBuilder tabInformation = new StringBuilder("NDMenu.OnTabsLoaded([");
			if (!EngineInstance.Config.ShrinkFiles)
				{  tabInformation.Append('\n');  }

			List<HTML.Components.MenuEntries.Container> tabContainers = new List<HTML.Components.MenuEntries.Container>();
			List<string> tabTypes = new List<string>();

			// DEPENDENCY: tabTypes must use the same strings as the NDLocation JavaScript class.
			// DEPENDENCY: tabTypes must use strings safe for including in CSS names.

			if (RootFileMenu != null)
				{
				tabContainers.Add(RootFileMenu);
				tabTypes.Add("File");
				}
			if (RootClassMenu != null)
				{
				tabContainers.Add(RootClassMenu);
				tabTypes.Add("Class");
				}
			if (RootDatabaseMenu != null)
				{
				tabContainers.Add(RootDatabaseMenu);
				tabTypes.Add("Database");
				}

			for (int i = 0; i < tabContainers.Count; i++)
				{
				ContainerExtraData extraData = (ContainerExtraData)tabContainers[i].ExtraData;

				if (!EngineInstance.Config.ShrinkFiles)
					{  tabInformation.Append(' ', IndentSpaces);  }

				tabInformation.Append("[\"");
				tabInformation.Append(tabTypes[i]);
				tabInformation.Append("\",");

				if (tabContainers[i].CondensedTitles == null)
					{
					tabInformation.Append('"');
					tabInformation.StringEscapeAndAppend( tabContainers[i].Title.ToHTML() );
					tabInformation.Append('"');
					}
				else
					{
					tabInformation.Append("[\"");
					tabInformation.StringEscapeAndAppend( tabContainers[i].Title.ToHTML() );
					tabInformation.Append('"');

					foreach (var condensedTitle in tabContainers[i].CondensedTitles)
						{
						tabInformation.Append(",\"");
						tabInformation.StringEscapeAndAppend( condensedTitle.ToHTML() );
						tabInformation.Append('"');
						}

					tabInformation.Append(']');
					}

				tabInformation.Append(',');

				if (extraData.HashPath != null)
					{
					tabInformation.Append('"');
					tabInformation.StringEscapeAndAppend(extraData.HashPath);
					tabInformation.Append('"');
					}
				// Otherwise leave an empty spot before the comma.  We don't have to write out "undefined".

				tabInformation.Append(",\"");
				tabInformation.StringEscapeAndAppend(extraData.DataFileName);
				tabInformation.Append("\"]");

				if (i < tabContainers.Count - 1)
					{  tabInformation.Append(',');  }

				if (!EngineInstance.Config.ShrinkFiles)
					{  tabInformation.Append('\n');  }
				}

			if (!EngineInstance.Config.ShrinkFiles)
				{  tabInformation.Append(' ', IndentSpaces);  }

			tabInformation.Append("]);");

			System.IO.File.WriteAllText( HTML.Paths.Menu.OutputFile(HTMLBuilder.OutputFolder, "tabs"), tabInformation.ToString() );


			return outputFiles;
			}


		/* Function: GenerateJSON
		 * Generates JSON for all the entries in the passed container.
		 */
		protected void GenerateJSON (HTML.Components.MenuEntries.Container container)
			{
			ContainerExtraData containerExtraData = new ContainerExtraData(container);
			container.ExtraData = containerExtraData;

			containerExtraData.GenerateJSON(HTMLBuilder, this);

			foreach (var member in container.Members)
				{
				if (member is HTML.Components.MenuEntries.Container)
					{
					GenerateJSON((HTML.Components.MenuEntries.Container)member);
					}
				else
					{
					TargetExtraData targetExtraData = new TargetExtraData((HTML.Components.MenuEntries.Entry)member);
					member.ExtraData = targetExtraData;

					targetExtraData.GenerateJSON(HTMLBuilder, this);
					}
				}
			}


		/* Function: SegmentMenu
		 * Segments the menu into smaller pieces and generates data file names.
		 */
		protected void SegmentMenu (HTML.Components.MenuEntries.Container container, string dataFileType, 
															  ref StringTable<IDObjects.NumberSet> usedDataFiles)
			{
			// Generate the data file name for this container.

			IDObjects.NumberSet usedDataFileNumbers = usedDataFiles[dataFileType];

			if (usedDataFileNumbers == null)
				{
				usedDataFileNumbers = new IDObjects.NumberSet();
				usedDataFiles.Add(dataFileType, usedDataFileNumbers);
				}
			
			int dataFileNumber = usedDataFileNumbers.LowestAvailable;
			usedDataFileNumbers.Add(dataFileNumber);

			ContainerExtraData extraData = (ContainerExtraData)container.ExtraData;
			extraData.DataFileName = HTML.Paths.Menu.OutputFile(HTMLBuilder.OutputFolder, dataFileType, dataFileNumber, fileNameOnly: true);


			// The data file has to include all the members in this container no matter what.

			int containerJSONSize = extraData.JSONBeforeMembers.Length + extraData.JSONAfterMembers.Length + 
														extraData.JSONLengthOfMembers;

			List<HTML.Components.MenuEntries.Container> subContainers = null;

			foreach (var member in container.Members)
				{
				if (member is HTML.Components.MenuEntries.Container)
					{
					if (subContainers == null)
						{  subContainers = new List<HTML.Components.MenuEntries.Container>();  }

					subContainers.Add((HTML.Components.MenuEntries.Container)member);
					}
				}


			// Now start including the contents of subcontainers until we reach the size limit.  We're going breadth-first instead of
			// depth first.

			List<HTML.Components.MenuEntries.Container> nextSubContainers = null;

			for (;;)
				{
				if (subContainers == null || subContainers.Count == 0)
					{
					if (nextSubContainers == null || nextSubContainers.Count == 0)
						{  break;  }
					else
						{
						subContainers = nextSubContainers;
						nextSubContainers = null;
						}
					}

				// Add subcontainers to the file in the order from smallest to largest.  This prevents one very large container early
				// in the list from causing all the other ones to be broken out into separate files.
				// DEPENDENCY: ContainerExtraData.JSONLengthOfMembers must cache its value for this algorithm to be efficient.

				int smallestSubContainerIndex = 0;
				int smallestSubContainerSize = (subContainers[0].ExtraData as ContainerExtraData).JSONLengthOfMembers;

				for (int i = 1; i < subContainers.Count; i++)
					{
					if ((subContainers[i].ExtraData as ContainerExtraData).JSONLengthOfMembers < smallestSubContainerSize)
						{
						smallestSubContainerIndex = i;
						smallestSubContainerSize = (subContainers[i].ExtraData as ContainerExtraData).JSONLengthOfMembers;
						}
					}

				containerJSONSize += smallestSubContainerSize;

				if (containerJSONSize > SegmentLength)
					{  break;  }

				foreach (var member in subContainers[smallestSubContainerIndex].Members)
					{
					if (member is HTML.Components.MenuEntries.Container)
						{
						if (nextSubContainers == null)
							{  nextSubContainers = new List<HTML.Components.MenuEntries.Container>();  }

						nextSubContainers.Add((HTML.Components.MenuEntries.Container)member);
						}
					}

				subContainers.RemoveAt(smallestSubContainerIndex);
				}


			// Now recurse through any remaining subcontainers so they get their own files.

			if (subContainers != null)
				{
				foreach (var subContainer in subContainers)
					{  SegmentMenu(subContainer, dataFileType, ref usedDataFiles);  }
				}

			if (nextSubContainers != null)
				{
				foreach (var subContainer in nextSubContainers)
					{  SegmentMenu(subContainer, dataFileType, ref usedDataFiles);  }
				}
			}


		/* Function: BuildOutput
		 * Generates the output file for the container.  It must have <ContainerExtraData.DataFileName> set.  If it finds
		 * any sub-containers that also have that set, it will recursively generate files for them as well.
		 */
		protected void BuildOutput (HTML.Components.MenuEntries.Container container)
			{
			#if DEBUG
			if (container.ExtraData == null || (container.ExtraData as ContainerExtraData).StartsNewDataFile == false)
				{  throw new Exception ("BuildOutput() can only be called on containers with DataFileName set.");  }
			#endif

			Stack<HTML.Components.MenuEntries.Container> containersToBuild = new Stack<HTML.Components.MenuEntries.Container>();
			containersToBuild.Push(container);

			while (containersToBuild.Count > 0)
				{
				HTML.Components.MenuEntries.Container containerToBuild = containersToBuild.Pop();
				string fileName = (containerToBuild.ExtraData as ContainerExtraData).DataFileName;
				
				StringBuilder output = new StringBuilder();
				output.Append("NDMenu.OnSectionLoaded(\"");
				output.StringEscapeAndAppend(fileName);
				output.Append("\",[");

				if (!EngineInstance.Config.ShrinkFiles)
					{  output.AppendLine();  }
				
				AppendMembers(containerToBuild, output, 1, containersToBuild);

				if (!EngineInstance.Config.ShrinkFiles)
					{  output.Append(' ', IndentSpaces);  }

				output.Append("]);");

				System.IO.File.WriteAllText(HTML.Paths.Menu.OutputFolder(HTMLBuilder.OutputFolder) + "/" + fileName, output.ToString());
				}
			}


		/* Function: AppendMembers
		 * A support function for <BuildOutput()>.  Appends the output of the container's members to the string, recursively 
		 * going through sub-containers as well.  This will not include the surrounding brackets, only the comma-separated
		 * member entries.  If it finds any sub-containers that start a new data file, it will add them to containersToBuild.
		 */
		protected void AppendMembers (HTML.Components.MenuEntries.Container container, StringBuilder output, int indent, 
																  Stack<HTML.Components.MenuEntries.Container> containersToBuild)
			{
			for (int i = 0; i < container.Members.Count; i++)
				{
				var member = container.Members[i];

				if (!EngineInstance.Config.ShrinkFiles)
					{  output.Append(' ', indent * IndentSpaces);  }

				if (member is HTML.Components.MenuEntries.Container)
					{
					ContainerExtraData containerExtraData = (ContainerExtraData)member.ExtraData;
					output.Append(containerExtraData.JSONBeforeMembers);

					if (containerExtraData.StartsNewDataFile)
						{
						output.Append('"');
						output.StringEscapeAndAppend(containerExtraData.DataFileName);
						output.Append('"');

						containersToBuild.Push((HTML.Components.MenuEntries.Container)member);
						}
					else
						{
						output.Append('[');

						if (!EngineInstance.Config.ShrinkFiles)
							{  output.AppendLine();  }

						AppendMembers((HTML.Components.MenuEntries.Container)member, output, indent + 1, containersToBuild);

						if (!EngineInstance.Config.ShrinkFiles)
							{  output.Append(' ', (indent + 1) * IndentSpaces);  }

						output.Append(']');
						}

					output.Append(containerExtraData.JSONAfterMembers);
					}
				else // not a container
					{
					TargetExtraData targetExtraData = (TargetExtraData)member.ExtraData;
					output.Append(targetExtraData.JSON);
					}

				if (i < container.Members.Count - 1)
					{  output.Append(',');  }

				if (!EngineInstance.Config.ShrinkFiles)
					{  output.AppendLine();  }
				}
			}



		// Group: Properties
		// __________________________________________________________________________
			

		/* var: HTMLBuilder
		 * The <Builders.HTML> object associated with this menu.
		 */
		public Builders.HTML HTMLBuilder
			{
			get
				{  return (Builders.HTML)builder;  }
			}



		// Group: Constants
		// __________________________________________________________________________

		/* Constant: IndentSpaces
		 * The number of spaces to indent each level by when building the output without file shrinking.
		 */
		protected const int IndentSpaces = 3;

		/* const: SegmentLength
		 * The amount of data to try to fit in each JSON file before splitting it off into another one.  This will be
		 * artificially low in debug builds to better test the loading mechanism.
		 */
		#if DEBUG
			protected const int SegmentLength = 1024*3;
		#else
			protected const int SegmentLength = 1024*32;
		#endif



		/* ____________________________________________________________________________
		 * 
		 * Class: CodeClear.NaturalDocs.Engine.Output.Components.JSMenuData.TargetExtraData
		 * ____________________________________________________________________________
		 * 
		 * A class used to store extra information needed by <JSMenuData> in each menu entry via the 
		 * ExtraData property.
		 * 
		 */
		private class TargetExtraData
			{

			// Group: Functions
			// _________________________________________________________________________

			/* Function: TargetExtraData
			 */
			public TargetExtraData (HTML.Components.MenuEntries.Entry menuEntry)
				{
				this.menuEntry = menuEntry;
				this.json = null;
				}

			/* Function: GenerateJSON
			 */
			public void GenerateJSON (Builders.HTML htmlBuilder, JSMenuData menu)
				{
				StringBuilder output = new StringBuilder();

				output.Append("[1,\"");

				string htmlTitle = menuEntry.Title.ToHTML();
				output.StringEscapeAndAppend(htmlTitle);

				output.Append('"');

				string hashPath = null;
				
				if (menuEntry is HTML.Components.MenuEntries.File)
					{  
					HTML.Components.MenuEntries.File fileMenuEntry = (HTML.Components.MenuEntries.File)menuEntry;
					Files.File file = fileMenuEntry.WrappedFile;
					Files.FileSource fileSource = htmlBuilder.EngineInstance.Files.FileSourceOf(file);

					hashPath = Output.HTML.Paths.SourceFile.HashPath(fileSource.Number, fileSource.MakeRelative(file.FileName));  
					}
				else if (menuEntry is HTML.Components.MenuEntries.Class)
					{  
					HTML.Components.MenuEntries.Class classMenuEntry = (HTML.Components.MenuEntries.Class)menuEntry;
					Symbols.ClassString classString = classMenuEntry.WrappedClassString;

					if (classString.Hierarchy == Hierarchy.Class)
						{
						Languages.Language language = htmlBuilder.EngineInstance.Languages.FromID(classString.LanguageID);
						hashPath = Output.HTML.Paths.Class.HashPath(language.SimpleIdentifier, classString.Symbol);  
						}
					else if (classString.Hierarchy == Hierarchy.Database)
						{
						hashPath = Output.HTML.Paths.Database.HashPath(classString.Symbol);
						}
					else
						{  throw new NotImplementedException();  }
					}
				#if DEBUG
				else
					{  throw new Exception ("Don't know how to generate JSON for target \"" + menuEntry.Title + "\".");  }
				#endif

				// Make the hashpath relative to the parent.
				ContainerExtraData parent = (ContainerExtraData)menuEntry.Parent.ExtraData;

				if (hashPath.StartsWith(parent.HashPath))
					{  hashPath = hashPath.Substring(parent.HashPath.Length);  }

				if (hashPath != htmlTitle)
					{
					output.Append(",\"");
					output.StringEscapeAndAppend(hashPath);
					output.Append('"');
					}

				output.Append(']');

				json = output.ToString();
				}


			// Group: Properties
			// _________________________________________________________________________

			/* Property: JSON
			 * After <GenerateJSON()> is called, this is the JSON output for this entry.
			 */
			public string JSON
				{
				get
					{  return json;  }
				}


			// Group: Variables
			// _________________________________________________________________________

			/* var: menuEntry
			 * The menu entry associated with this object.
			 */
			protected HTML.Components.MenuEntries.Entry menuEntry;

			/* var: json
			 * The generated JSON for this entry.
			 */
			protected string json;

			}



		/* ____________________________________________________________________________
		 * 
		 * Class: CodeClear.NaturalDocs.Engine.Output.Components.JSMenuData.ContainerExtraData
		 * ____________________________________________________________________________
		 * 
		 * A class used to store extra information needed by <JSMenuData> in each menu entry via the 
		 * ExtraData property.
		 * 
		 */
		private class ContainerExtraData
			{

			// Group: Functions
			// _________________________________________________________________________

			public ContainerExtraData (HTML.Components.MenuEntries.Container menuEntry)
				{
				this.menuEntry = menuEntry;
				this.jsonBeforeMembers = null;
				this.jsonAfterMembers = null;
				this.jsonLengthOfMembers = -1;
				this.dataFileName = null;
				this.hashPath = null;
				}

			/* Function: GenerateJSON
			 */
			public void GenerateJSON (Builders.HTML htmlBuilder, JSMenuData menu)
				{
				StringBuilder output = new StringBuilder();

				output.Append("[2,");


				// Title

				if (menuEntry.CondensedTitles == null)
					{
					if (menuEntry.Title != null)
						{
						output.Append('"');
						output.StringEscapeAndAppend(menuEntry.Title.ToHTML());
						output.Append('"');
						}
					// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".
					}
				else
					{
					output.Append("[\"");
					output.StringEscapeAndAppend(menuEntry.Title.ToHTML());
					output.Append('"');

					foreach (string condensedTitle in menuEntry.CondensedTitles)
						{
						output.Append(",\"");
						output.StringEscapeAndAppend(condensedTitle.ToHTML());
						output.Append('"');
						}

					output.Append(']');
					}


				// Hash path

				output.Append(',');

				if (menuEntry is HTML.Components.MenuEntries.FileSource)
					{
					HTML.Components.MenuEntries.FileSource fileSourceEntry = (HTML.Components.MenuEntries.FileSource)menuEntry;
					hashPath = Output.HTML.Paths.SourceFile.FolderHashPath(fileSourceEntry.WrappedFileSource.Number,
																										 fileSourceEntry.CondensedPathFromFileSource );
					}
				else if (menuEntry is HTML.Components.MenuEntries.Folder)
					{
					HTML.Components.MenuEntries.Container container = menuEntry.Parent;

					#if DEBUG
					if (container == null)
						{  throw new Exception ("Parent must be defined when generating JSON for menu folder \"" + menuEntry.Title + "\".");  }
					#endif

					while ((container is HTML.Components.MenuEntries.FileSource) == false)
						{
						container = container.Parent;

						#if DEBUG
						if (container == null)
							{  throw new Exception ("Couldn't find a file source among the folder \"" + menuEntry.Title + "\"'s parents when generating JSON.");  }
						#endif
						}

					HTML.Components.MenuEntries.Folder folderEntry = (HTML.Components.MenuEntries.Folder)menuEntry;
					HTML.Components.MenuEntries.FileSource fileSourceEntry = (HTML.Components.MenuEntries.FileSource)container;

					hashPath = Output.HTML.Paths.SourceFile.FolderHashPath(fileSourceEntry.WrappedFileSource.Number, 
																										 folderEntry.PathFromFileSource );
					}
				else if (menuEntry is HTML.Components.MenuEntries.Language)
					{
					HTML.Components.MenuEntries.Language languageEntry = (HTML.Components.MenuEntries.Language)menuEntry;

					hashPath = Output.HTML.Paths.Class.QualifierHashPath(languageEntry.WrappedLanguage.SimpleIdentifier,
																									 languageEntry.CondensedScopeString);
					}
				else if (menuEntry is HTML.Components.MenuEntries.Scope)
					{
					HTML.Components.MenuEntries.Container container = menuEntry;

					#if DEBUG
					if (container == null)
						{  throw new Exception ("Parent must be defined when generating JSON for menu scope \"" + menuEntry.Title + "\".");  }
					#endif

					while ((container is HTML.Components.MenuEntries.Language) == false && container != menu.RootDatabaseMenu)
						{
						container = container.Parent;

						#if DEBUG
						if (container == null)
							{  throw new Exception ("Couldn't find a language among the scope \"" + menuEntry.Title + "\"'s parents when generating JSON.");  }
						#endif
						}

					HTML.Components.MenuEntries.Scope scopeEntry = (HTML.Components.MenuEntries.Scope)menuEntry;

					if (container == menu.RootDatabaseMenu)
						{
						hashPath = Output.HTML.Paths.Database.QualifierHashPath(scopeEntry.WrappedScopeString);
						}
					else
						{
						HTML.Components.MenuEntries.Language languageEntry = (HTML.Components.MenuEntries.Language)container;
						hashPath = Output.HTML.Paths.Class.QualifierHashPath(languageEntry.WrappedLanguage.SimpleIdentifier,
																										 scopeEntry.WrappedScopeString);
						}
					}
				else if (menuEntry == menu.RootFileMenu || menuEntry == menu.RootClassMenu)
					{
					// If we're at the root file or class menu and the entry is not also a language or file source, it means there are multiple languages and/or
					// file sources beneath it and thus there is no shared hash path.  "CSharpClass:" and "PerlClass:", "Files:" and "Files2:", etc.
					hashPath = null;
					}
				else if (menuEntry == menu.RootDatabaseMenu)
					{
					// If we're at the root database menu and the entry is not also a scope, it means there are multiple scopes beneath it.  However, unlike
					// files and classes, there is still the shared "Database:" hash path.
					hashPath = Output.HTML.Paths.Database.QualifierHashPath();
					}
				#if DEBUG
				else
					{  throw new Exception ("Don't know how to generate JSON for container \"" + menuEntry.Title + "\".");  }
				#endif

				if (hashPath != null)
					{  
					output.Append('"');
					output.StringEscapeAndAppend(hashPath);
					output.Append('"');
					}
				// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".

				output.Append(',');

				jsonBeforeMembers = output.ToString();
				jsonAfterMembers = "]";
				}


			// Group: Properties
			// _________________________________________________________________________

			/* Property: StartsNewDataFile
			 * Whether this container starts a new data file.  This property is read-only.  If you need to change
			 * it, set <DataFileName> instead.
			 */
			public bool StartsNewDataFile
				{
				get
					{  return (dataFileName != null);  }
				}

			/* Property: DataFileName
			 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
			 * not include a path.  If this container doesn't start a new data file, this will be null.
			 */
			public string DataFileName
				{
				get
					{  return dataFileName;  }
				set
					{  dataFileName = value;  }
				}

			/* Property: JSONBeforeMembers
			 * After <GenerateJSON()> is called, this will be the JSON output of this entry up to the point where its members
			 * would appear.
			 */
			public string JSONBeforeMembers
				{
				get
					{  return jsonBeforeMembers;  }
				}

			/* Property: JSONAfterMembers
			 * After <GenerateJSON()> is called, this will be the JSON output of this entry after the point where its members
			 * would appear.
			 */
			public string JSONAfterMembers
				{
				get
					{  return jsonAfterMembers;  }
				}

			/* Property: JSONLengthOfMembers
			 * The calculated total JSON length of all members stored directly in this container.  It does NOT recurse into deeper
			 * containers.
			 */
			public int JSONLengthOfMembers
				{
				get
					{
					// DEPENDENCY: JSMenuData.SegmentMenu expects this value to only be calculated once despite repeated calls for 
					// its algorithm to be efficient.

					if (jsonLengthOfMembers != -1)
						{  return jsonLengthOfMembers;  }

					jsonLengthOfMembers = 0;

					foreach (var member in menuEntry.Members)
						{
						if (member is HTML.Components.MenuEntries.Container)
							{
							ContainerExtraData extraData = (ContainerExtraData)member.ExtraData;
							jsonLengthOfMembers += extraData.JSONBeforeMembers.Length + extraData.JSONAfterMembers.Length;
							}
						else
							{
							jsonLengthOfMembers += (member.ExtraData as TargetExtraData).JSON.Length;
							}
						}

					return jsonLengthOfMembers;
					}
				}

			/* Property: HashPath
			 * The hash path of the container, or null if none.  This will only be available after <GenerateJSON()> is called.
			 */
			public string HashPath
				{
				get
					{  return hashPath;  }
				}



			// Group: Variables
			// _________________________________________________________________________

			/* var: menuEntry
			 * The menu entry associated with this object.
			 */
			protected HTML.Components.MenuEntries.Container menuEntry;

			/* var: jsonBeforeMembers
			 * The generated JSON for this entry, up to the point where its members would be inserted.
			 */
			protected string jsonBeforeMembers;

			/* var: jsonAfterMembers
			 * The generated JSON for this entry, after the point where its members would be inserted.
			 */
			protected string jsonAfterMembers;

			/* var: jsonLengthOfMembers
			 * The calculated total JSON length of all members directly stored in this container, or -1 if it hasn't been
			 * calculated yet.  It does NOT recurse into deeper levels.
			 */
			protected int jsonLengthOfMembers;

			/* var: dataFileName
			 * If this container starts a new data file this will be its file name, such as "files2.js" or "classes.js".  It will
			 * not include a path.  If this container doesn't start a new data file, this will be null.
			 */
			protected string dataFileName;

			/* var: hashPath
			 * The hash path of the container, or null if none.  This will only be available after <GenerateJSON()> is called.
			 */
			protected string hashPath;

			}

		}
	}

