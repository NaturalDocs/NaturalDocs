/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.JSONMenu
 * ____________________________________________________________________________
 * 
 * A helper class to build a JSON representation of <Menu> for output.  It can also save the representation to JavaScript files as 
 * documented in <JavaScript Menu Data>.
 * 
 * Usage:
 * 
 *		- Call <ConvertToJSON(Menu)> to create the JSON representation of the <Menu>.
 *		- If desired, call <BuildDataFiles()> to create the output files.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Hierarchies;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class JSONMenu : Component
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: JSONMenu
		 */
		public JSONMenu (Context context) : base (context)
			{
			rootFileMenu = null;
			rootClassMenu = null;
			rootDatabaseMenu = null;

			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);
			}


		/* Function: ConvertToJSON
		 * Converts the passed <Menu> to a JSON menu structure, accessible from <RootFileMenu>, <RootClassMenu>, and 
		 * <RootDatabaseMenu>.
		 */
		public void ConvertToJSON (Menu menu)
			{
			rootFileMenu = (menu.RootFileMenu != null ? ConvertToJSON(menu.RootFileMenu) : null);
			rootClassMenu = (menu.RootClassMenu != null ? ConvertToJSON(menu.RootClassMenu) : null);
			rootDatabaseMenu = (menu.RootDatabaseMenu != null ? ConvertToJSON(menu.RootDatabaseMenu) : null);

			addWhitespace = (EngineInstance.Config.ShrinkFiles == false);
			}


		/* Function: BuildDataFiles
		 * 
		 * Takes the JSON menu created by <ConvertToJSON()> and saves it as a series of JavaScript files as documented in 
		 * <JavaScript Menu Data>.  This includes the tab information file.  Note that you have to call <ConvertToJSON()> prior to 
		 * calling this function or no data files will be generated.
		 * 
		 * Returns:
		 * 
		 *		A table mapping each <HierarchyTypes> to the data file numbers used for it, such as Files -> {1-4}.
		 */
		public NumberSetTable<HierarchyType> BuildDataFiles ()
			{
			try
				{  
				// This will create multiple subdirectories if needed, and will not throw an exception if it already exists.
				System.IO.Directory.CreateDirectory( Paths.Menu.OutputFolder(context.Target.OutputFolder) );
				}
			catch (Exception e)
				{
				throw new Exceptions.UserFriendly( 
					Locale.Get("NaturalDocs.Engine", "Error.CouldNotCreateOutputFolder(name, exception)", 
									Paths.Menu.OutputFolder(context.Target.OutputFolder), e.Message) 
					);
				}

			var usedDataFiles = AssignDataFiles();

			if (rootFileMenu != null)
				{  BuildDataFiles(rootFileMenu);  }
			if (rootClassMenu != null)
				{  BuildDataFiles(rootClassMenu);  }
			if (rootDatabaseMenu != null)
				{  BuildDataFiles(rootDatabaseMenu);  }

			BuildTabDataFile();

			return usedDataFiles;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: ConvertToJSON
		 * Converts a menu entry to JSON.  If you pass a container it will automatically forward it to <ConvertToJSON(container)>.
		 */
		protected JSONMenuEntries.Entry ConvertToJSON (MenuEntries.Entry menuEntry, string parentHashPath = null)
			{

			// Forward containers to the other function

			if (menuEntry is MenuEntries.Container)
				{  return ConvertToJSON(menuEntry as MenuEntries.Container);  }


			// Build a target entry

			var jsonEntry = new JSONMenuEntries.Target(menuEntry);
			StringBuilder json = new StringBuilder();

			json.Append("[1,\"");

			string htmlTitle = menuEntry.Title.ToHTML();
			json.StringEscapeAndAppend(htmlTitle);

			json.Append('"');

			string hashPath = null;
				
			if (menuEntry is MenuEntries.Files.File)
				{  
				var fileMenuEntry = (MenuEntries.Files.File)menuEntry;
				var file = fileMenuEntry.WrappedFile;
				var fileSource = EngineInstance.Files.FileSourceOf(file);

				hashPath = Paths.SourceFile.HashPath(fileSource.Number, fileSource.MakeRelative(file.FileName));  
				}
			else if (menuEntry is MenuEntries.Classes.Class)
				{  
				var classMenuEntry = (MenuEntries.Classes.Class)menuEntry;
				var classString = classMenuEntry.WrappedClassString;

				if (classString.HierarchyType == HierarchyType.Class)
					{
					var language = EngineInstance.Languages.FromID(classString.LanguageID);
					hashPath = Paths.Class.HashPath(language.SimpleIdentifier, classString.Symbol);  
					}
				else if (classString.HierarchyType == HierarchyType.Database)
					{
					hashPath = Paths.Database.HashPath(classString.Symbol);
					}
				else
					{  throw new NotImplementedException();  }
				}
			else
				{  throw new NotImplementedException();  }


			// Make the hashpath relative to the parent.

			if (parentHashPath != null && hashPath.StartsWith(parentHashPath))
				{  hashPath = hashPath.Substring(parentHashPath.Length);  }

			if (hashPath != htmlTitle)
				{
				json.Append(",\"");
				json.StringEscapeAndAppend(hashPath);
				json.Append('"');
				}

			json.Append(']');

			jsonEntry.JSON = json.ToString();

			return jsonEntry;
			}


		/* Function: ConvertToJSON
		 * Converts a Container menu entry to JSON, along with all of its members.  This is a recursive function so it will convert
		 * the entire tree inside the container.
		 */
		protected JSONMenuEntries.Container ConvertToJSON (MenuEntries.Container menuContainer)
			{
			JSONMenuEntries.Container jsonContainer = new JSONMenuEntries.Container(menuContainer);
			StringBuilder jsonBeforeMembers = new StringBuilder();

			jsonBeforeMembers.Append("[2,");


			// Title

			if (menuContainer.CondensedTitles == null)
				{
				if (menuContainer.Title != null)
					{
					jsonBeforeMembers.Append('"');
					jsonBeforeMembers.StringEscapeAndAppend(menuContainer.Title.ToHTML());
					jsonBeforeMembers.Append('"');
					}
				// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".
				}
			else
				{
				jsonBeforeMembers.Append("[\"");
				jsonBeforeMembers.StringEscapeAndAppend(menuContainer.Title.ToHTML());
				jsonBeforeMembers.Append('"');

				foreach (string condensedTitle in menuContainer.CondensedTitles)
					{
					jsonBeforeMembers.Append(",\"");
					jsonBeforeMembers.StringEscapeAndAppend(condensedTitle.ToHTML());
					jsonBeforeMembers.Append('"');
					}

				jsonBeforeMembers.Append(']');
				}


			// Hash path

			jsonBeforeMembers.Append(',');

			string hashPath = null;

			if (menuContainer is MenuEntries.Files.FileSource)
				{
				var fileSourceEntry = (MenuEntries.Files.FileSource)menuContainer;
				hashPath = Paths.SourceFile.FolderHashPath(fileSourceEntry.WrappedFileSource.Number,
																				 fileSourceEntry.CondensedPathFromFileSource);
				}

			else if (menuContainer is MenuEntries.Files.Folder)
				{
				var folderEntry = (MenuEntries.Files.Folder)menuContainer;

				// Walk up the tree until you find the FileSource
				MenuEntries.Container parentEntry = menuContainer.Parent;

				#if DEBUG
				if (parentEntry == null)
					{  throw new Exception ("Parent must be defined when generating JSON for menu folder \"" + (folderEntry.Title ?? "") + "\".");  }
				#endif

				while ((parentEntry is MenuEntries.Files.FileSource) == false)
					{
					parentEntry = parentEntry.Parent;

					#if DEBUG
					if (parentEntry == null)
						{  throw new Exception ("Couldn't find a file source among the folder \"" + (folderEntry.Title ?? "") + "\"'s parents when generating JSON.");  }
					#endif
					}

				var fileSourceEntry = (MenuEntries.Files.FileSource)parentEntry;
				hashPath = Paths.SourceFile.FolderHashPath(fileSourceEntry.WrappedFileSource.Number, 
																				 folderEntry.PathFromFileSource );
				}

			else if (menuContainer is MenuEntries.Classes.Language)
				{
				var languageEntry = (MenuEntries.Classes.Language)menuContainer;
				hashPath = Paths.Class.QualifierHashPath(languageEntry.WrappedLanguage.SimpleIdentifier,
																			  languageEntry.CondensedScopeString);
				}

			else if (menuContainer is MenuEntries.Classes.Scope)
				{
				var scopeEntry = (MenuEntries.Classes.Scope)menuContainer;

				if (scopeEntry.HierarchyType == HierarchyType.Class)
					{
					// Walk up the tree until you find the language
					MenuEntries.Container parentEntry = menuContainer.Parent;

					#if DEBUG
					if (parentEntry == null)
						{  throw new Exception ("Parent must be defined when generating JSON for menu scope \"" + (scopeEntry.Title ?? "") + "\".");  }
					#endif

					while ((parentEntry is MenuEntries.Classes.Language) == false)
						{
						parentEntry = parentEntry.Parent;

						#if DEBUG
						if (parentEntry == null)
							{  throw new Exception ("Couldn't find a language among the scope \"" + (scopeEntry.Title ?? "") + "\"'s parents when generating JSON.");  }
						#endif
						}

					var languageEntry = (MenuEntries.Classes.Language)parentEntry;
					hashPath = Paths.Class.QualifierHashPath(languageEntry.WrappedLanguage.SimpleIdentifier, 
																				 scopeEntry.WrappedScopeString);
					}
				else if (scopeEntry.HierarchyType == HierarchyType.Database)
					{
					hashPath = Paths.Database.QualifierHashPath(scopeEntry.WrappedScopeString);
					}
				else
					{  throw new NotImplementedException();  }
				}

			// If we're at one of the menu roots
			else if (menuContainer.Parent == null)
				{
				if (menuContainer.HierarchyType == HierarchyType.File || menuContainer.HierarchyType == HierarchyType.Class)
					{
					// If we're at a root file or class container that is not also a language or file source, it means there are multiple 
					// languages and/or file sources beneath it and thus there is no shared hash path.  "CSharpClass:" and "PerlClass:",
					// "Files:" and "Files2:", etc.
					hashPath = null;
					}
				else if (menuContainer.HierarchyType == HierarchyType.Database)
					{
					// If we're at the root database menu and the entry is not also a scope, it means there are multiple scopes beneath it.
					// However, unlike files and classes, there is still the shared "Database:" hash path.
					hashPath = Paths.Database.QualifierHashPath();
					}
				else
					{  throw new NotImplementedException();  }
				}

			else
				{  throw new NotImplementedException();  }


			if (hashPath != null)
				{  
				jsonBeforeMembers.Append('"');
				jsonBeforeMembers.StringEscapeAndAppend(hashPath);
				jsonBeforeMembers.Append('"');
				}
			// Otherwise leave an empty space before the comma.  We don't have to write out "undefined".

			jsonBeforeMembers.Append(',');

			jsonContainer.JSONBeforeMembers = jsonBeforeMembers.ToString();
			jsonContainer.JSONAfterMembers = "]";
			jsonContainer.HashPath = hashPath;


			// Now recurse into members

			foreach (var member in menuContainer.Members)
				{  jsonContainer.Members.Add( ConvertToJSON(member, jsonContainer.HashPath) );  }

			return jsonContainer;
			}


		/* Function: AssignDataFiles
		 * 
		 * Segments the menu into smaller pieces and generates data file names.
		 * 
		 * Returns:
		 * 
		 *		A table mapping each <HierarchyType> to the data file numbers used for it, such as Files -> {1-4}.
		 */
		protected NumberSetTable<HierarchyType> AssignDataFiles ()
			{
			NumberSetTable<HierarchyType> usedDataFiles = new NumberSetTable<HierarchyType>();

			if (rootFileMenu != null)
				{  AssignDataFiles(rootFileMenu, ref usedDataFiles);  }
			if (rootClassMenu != null)
				{  AssignDataFiles(rootClassMenu, ref usedDataFiles);  }
			if (rootDatabaseMenu != null)
				{  AssignDataFiles(rootDatabaseMenu, ref usedDataFiles);  }

			return usedDataFiles;
			}


		/* Function: AssignDataFiles
		 * 
		 * Segments the menu into smaller pieces and generates data file names.
		 * 
		 * Parameters:
		 * 
		 *		container - The container to segment.  This will always be assigned a data file name.
		 *		usedDataFiles - A table mapping each <HierarchyType> to the data file numbers already in use for it, such as Files -> {1-4}.
		 *							   It will be used to determine which numbers are available to assign, and new numbers will be added to it
		 *							   as they are assigned by this function.
		 */
		protected void AssignDataFiles (JSONMenuEntries.Container container, ref NumberSetTable<HierarchyType> usedDataFiles)
			{
			// Generate the data file name for this container.

			HierarchyType hierarchy = container.MenuEntry.HierarchyType;

			int dataFileNumber = usedDataFiles.LowestAvailable(hierarchy);
			usedDataFiles.Add(hierarchy, dataFileNumber);

			container.DataFileName = Paths.Menu.OutputFile(Target.OutputFolder, hierarchy, dataFileNumber, fileNameOnly: true);


			// The data file has to include all the members in this container no matter what, so we don't check the size against the limit
			// yet.

			int containerJSONSize = container.JSONBeforeMembers.Length + container.JSONAfterMembers.Length + 
												container.JSONLengthOfMembers;


			// Now find all the subcontainers, which are now candidates for inlining.

			List<JSONMenuEntries.Container> inliningCandidates = null;

			foreach (var member in container.Members)
				{
				if (member is JSONMenuEntries.Container)
					{
					var containerMember = (JSONMenuEntries.Container)member;

					if (inliningCandidates == null)
						{  inliningCandidates = new List<JSONMenuEntries.Container>();  }

					inliningCandidates.Add(containerMember);
					}
				}


			// If there's no subcontainers we're done.

			if (inliningCandidates == null)
				{  return;  }


			// Go through all our candidates and inline them smallest to largest.  This prevents one very large container early in the list
			// from causing all the other ones to be broken out into separate files.

			// Keep track of which containers were inlined so we can possibly inline their members as well.
			List<JSONMenuEntries.Container> inlinedContainers = new List<JSONMenuEntries.Container>();

			while (inliningCandidates.Count > 0)
				{
				// Find the smallest of the candidates

				int smallestInliningCandidateIndex = 0;
				int smallestInliningCandidateSize = inliningCandidates[0].JSONLengthOfMembers;

				for (int i = 1; i < inliningCandidates.Count; i++)
					{
					if (inliningCandidates[i].JSONLengthOfMembers < smallestInliningCandidateSize)
						{
						smallestInliningCandidateIndex = i;
						smallestInliningCandidateSize = inliningCandidates[i].JSONLengthOfMembers;
						}
					}


				// If the smallest candidate fits into the segment length limits, inline it

				if (containerJSONSize + smallestInliningCandidateSize <= SegmentLength)
					{
					containerJSONSize += smallestInliningCandidateSize;
					inlinedContainers.Add(inliningCandidates[smallestInliningCandidateIndex]);
					inliningCandidates.RemoveAt(smallestInliningCandidateIndex);
					}


				// If the smallest candidate doesn't fit, that means it and all the remaining candidates need to get their own files

				else
					{
					foreach (var inliningCandidate in inliningCandidates)
						{  AssignDataFiles(inliningCandidate, ref usedDataFiles);  }

					inliningCandidates.Clear();
					}


				// If there's no more candidates, go through the list of inlined containers and add their subcontainers to the candidates
				// list.  This allows us to continue inlining for multiple levels as long as we have space for it.
				
				// This algorithm causes inlining to happen breadth-first instead of depth-first, which we want, but it also allows lower
				// depths to continue to be inlined even if the parent level couldn't be done completely.  It's possible that when there's
				// no room for all the top-level containers a few more lower level ones could still be squeezed in.

				if (inliningCandidates.Count == 0 && inlinedContainers.Count > 0)
					{
					foreach (var inlinedContainer in inlinedContainers)
						{
						foreach (var member in inlinedContainer.Members)
							{
							if (member is JSONMenuEntries.Container)
								{
								inliningCandidates.Add( (JSONMenuEntries.Container)member );
								}
							}
						}

					inlinedContainers.Clear();
					}
				}
			}


		/* Function: BuildDataFiles
		 * Generates the output data file for the container.  It must have <JSONContainer.DataFileName> set.  If it finds any 
		 * sub-containers that also have that set, it will recursively generate files for them as well.
		 */
		protected void BuildDataFiles (JSONMenuEntries.Container container)
			{
			#if DEBUG
			if (container.StartsNewDataFile == false)
				{  throw new Exception ("BuildOutput() can only be called on containers with DataFileName set.");  }
			#endif

			Stack<JSONMenuEntries.Container> containersToBuild = new Stack<JSONMenuEntries.Container>();
			containersToBuild.Push(container);

			while (containersToBuild.Count > 0)
				{
				var containerToBuild = containersToBuild.Pop();
				string fileName = containerToBuild.DataFileName;
				
				StringBuilder output = new StringBuilder();
				output.Append("NDMenu.OnSectionLoaded(\"");
				output.StringEscapeAndAppend(fileName);
				output.Append("\",[");

				if (addWhitespace)
					{  output.AppendLine();  }
				
				AppendMembers(containerToBuild, output, 1, containersToBuild);

				if (addWhitespace)
					{  output.Append(' ', IndentWidth);  }

				output.Append("]);");

				WriteTextFile(Paths.Menu.OutputFolder(Target.OutputFolder) + "/" + fileName, output.ToString());
				}
			}


		/* Function: AppendMembers
		 * A support function for <BuildDataFile()>.  Appends the output of the container's members to the string, recursively going
		 * through sub-containers as well.  This will not include the surrounding brackets, only the comma-separated member entries.
		 * If it finds any sub-containers that start a new data file, it will add them to containersToBuild.
		 */
		protected void AppendMembers (JSONMenuEntries.Container container, StringBuilder output, int indent, 
													   Stack<JSONMenuEntries.Container> containersToBuild)
			{
			for (int i = 0; i < container.Members.Count; i++)
				{
				var member = container.Members[i];

				if (addWhitespace)
					{  output.Append(' ', indent * IndentWidth);  }

				if (member is JSONMenuEntries.Container)
					{
					var memberContainer = (JSONMenuEntries.Container)member;

					output.Append(memberContainer.JSONBeforeMembers);

					if (memberContainer.StartsNewDataFile)
						{
						output.Append('"');
						output.StringEscapeAndAppend(memberContainer.DataFileName);
						output.Append('"');

						containersToBuild.Push(memberContainer);
						}
					else
						{
						output.Append('[');

						if (addWhitespace)
							{  output.AppendLine();  }

						AppendMembers(memberContainer, output, indent + 1, containersToBuild);

						if (addWhitespace)
							{  output.Append(' ', (indent + 1) * IndentWidth);  }

						output.Append(']');
						}

					output.Append(memberContainer.JSONAfterMembers);
					}
				else // not a container
					{
					var memberTarget = (JSONMenuEntries.Target)member;
					output.Append(memberTarget.JSON);
					}

				if (i < container.Members.Count - 1)
					{  output.Append(',');  }

				if (addWhitespace)
					{  output.AppendLine();  }
				}
			}


		/* Function: BuildTabDataFile
		 * Generates the output data file containing tab information.  This should only be called after <AssignDataFiles()> has been called.
		 */
		protected void BuildTabDataFile ()
			{
			StringBuilder tabInformation = new StringBuilder("NDMenu.OnTabsLoaded([");

			if (addWhitespace)
				{  tabInformation.Append('\n');  }

			List<JSONMenuEntries.Container> tabContainers = new List<JSONMenuEntries.Container>();
			List<string> tabTypes = new List<string>();

			// DEPENDENCY: tabTypes must use the same strings as the NDLocation JavaScript class.
			// DEPENDENCY: tabTypes must use strings safe for including in CSS names.

			if (rootFileMenu != null)
				{
				tabContainers.Add(rootFileMenu);
				tabTypes.Add("File");
				}
			if (rootClassMenu != null)
				{
				tabContainers.Add(rootClassMenu);
				tabTypes.Add("Class");
				}
			if (rootDatabaseMenu != null)
				{
				tabContainers.Add(rootDatabaseMenu);
				tabTypes.Add("Database");
				}

			for (int i = 0; i < tabContainers.Count; i++)
				{
				if (addWhitespace)
					{  tabInformation.Append(' ', IndentWidth);  }

				tabInformation.Append("[\"");
				tabInformation.Append(tabTypes[i]);
				tabInformation.Append("\",");

				var condensedTitles = (tabContainers[i].MenuEntry as MenuEntries.Container).CondensedTitles;

				if (condensedTitles == null)
					{
					tabInformation.Append('"');
					tabInformation.StringEscapeAndAppend( tabContainers[i].MenuEntry.Title.ToHTML() );
					tabInformation.Append('"');
					}
				else
					{
					tabInformation.Append("[\"");
					tabInformation.StringEscapeAndAppend( tabContainers[i].MenuEntry.Title.ToHTML() );
					tabInformation.Append('"');

					foreach (var condensedTitle in condensedTitles)
						{
						tabInformation.Append(",\"");
						tabInformation.StringEscapeAndAppend( condensedTitle.ToHTML() );
						tabInformation.Append('"');
						}

					tabInformation.Append(']');
					}

				tabInformation.Append(',');

				if (tabContainers[i].HashPath != null)
					{
					tabInformation.Append('"');
					tabInformation.StringEscapeAndAppend(tabContainers[i].HashPath);
					tabInformation.Append('"');
					}
				// Otherwise leave an empty spot before the comma.  We don't have to write out "undefined".

				tabInformation.Append(",\"");
				tabInformation.StringEscapeAndAppend(tabContainers[i].DataFileName);
				tabInformation.Append("\"]");

				if (i < tabContainers.Count - 1)
					{  tabInformation.Append(',');  }

				if (addWhitespace)
					{  tabInformation.Append('\n');  }
				}

			if (addWhitespace)
				{  tabInformation.Append(' ', IndentWidth);  }

			tabInformation.Append("]);");

			WriteTextFile( Paths.Menu.TabOutputFile(Target.OutputFolder), tabInformation.ToString() );
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: RootFileMenu
		 * The root container of all file-based menu entries, or null if none.
		 */
		public JSONMenuEntries.Container RootFileMenu
			{
			get
				{  return rootFileMenu;  }
			}
			

		/* Property: RootClassMenu
		 * The root container of all class-based menu entries, or null if none.
		 */
		public JSONMenuEntries.Container RootClassMenu
			{
			get
				{  return rootClassMenu;  }
			}


		/* Property: RootDatabaseMenu
		 * The root container of all database-based menu entries, or null if none.
		 */
		public JSONMenuEntries.Container RootDatabaseMenu
			{
			get
				{  return rootDatabaseMenu;  }
			}



		// Group: Constants
		// __________________________________________________________________________

		/* Constant: IndentWidth
		 * The number of spaces to indent each level by when building the output with extra whitespace.
		 */
		protected const int IndentWidth = 3;

		/* const: SegmentLength
		 * The amount of data to try to fit in each JSON file before splitting it off into another one.  This will be
		 * artificially low in debug builds to better test the loading mechanism.
		 */
		#if DEBUG
			protected const int SegmentLength = 1024*3;
		#else
			protected const int SegmentLength = 1024*32;
		#endif



		// Group: Variables
		// __________________________________________________________________________


		/* var: rootFileMenu
		 * The root container of all file-based menu entries, or null if none.
		 */
		protected JSONMenuEntries.Container rootFileMenu;

		/* var: rootClassMenu
		 * The root container of all class-based menu entries, or null if none.
		 */
		protected JSONMenuEntries.Container rootClassMenu;

		/* var: rootDatabaseMenu
		 * The root container of all database-based menu entries, or null if none.
		 */
		protected JSONMenuEntries.Container rootDatabaseMenu;

		/* var: addWhitespace
		 * Whether additional whitespace and line breaks should be added to the JSON output to make it more readable.
		 */
		protected bool addWhitespace;

		}
	}

