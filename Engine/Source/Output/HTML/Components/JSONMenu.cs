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
 *		- If desired, call <AssignDataFiles()> to determine how the menu will be divided into files.  This will be called automatically
 *		  by <BuildDataFiles()> if you do not do it manually.
 *		- If desired, call <BuildDataFiles()> to create the output files.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.IDObjects;


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
			fileRoot = null;
			hierarchyRoots = null;
			}


		/* Function: ConvertToJSON
		 * Converts the passed <Menu> to a JSON menu structure, accessible from <FileRoot> and <HierarchyRoots>.
		 */
		public void ConvertToJSON (Menu menu)
			{
			if (menu.FileRoot != null)
				{  fileRoot = (JSONMenuEntries.RootContainer)ConvertToJSON(menu.FileRoot, isRoot: true);  }
			else
				{  fileRoot = null;  }
			
			if (menu.HierarchyRoots != null)
				{
				hierarchyRoots = new List<JSONMenuEntries.RootContainer>( menu.HierarchyRoots.Count );

				foreach (var menuHierarchyRoot in menu.HierarchyRoots)
					{  hierarchyRoots.Add( (JSONMenuEntries.RootContainer)ConvertToJSON(menuHierarchyRoot, isRoot: true) );  }

				hierarchyRoots.Sort(
					delegate (JSONMenuEntries.RootContainer a, JSONMenuEntries.RootContainer b)
						{
						var hierarchyA = EngineInstance.Hierarchies.FromID( a.MenuEntry.HierarchyID );
						var hierarchyB = EngineInstance.Hierarchies.FromID( b.MenuEntry.HierarchyID );

						return (hierarchyA.SortValue - hierarchyB.SortValue);
						}
					);
				}
			else
				{  hierarchyRoots = null;  }
			}


		/* Function: AssignDataFiles
		 * 
		 * Takes the JSON menu created by <ConvertToJSON()> and determines how it will be saved into individual data files.  After
		 * this function is called you can retrieve the used data file numbers from <FileRoot> and <HierarchyRoots>.
		 * 
		 * You do not need to call this function manually.  <BuildDataFiles()> will do it for you automatically if you do not.  However,
		 * calling it manually allows you to inspect what the data files would be before they're actually created.
		 */
		public void AssignDataFiles ()
			{
			// DEPENDENCY: BuildDataFiles() assumes calling this function multiple times has no effect.

			if (fileRoot != null && fileRoot.DataFileName == null)
				{  AssignDataFiles(fileRoot);  }

			if (hierarchyRoots != null)
				{  
				foreach (var hierarchyRoot in hierarchyRoots)
					{
					if (hierarchyRoot.DataFileName == null)
						{  AssignDataFiles(hierarchyRoot);  }
					}
				}
			}


		/* Function: BuildDataFiles
		 * 
		 * Takes the JSON menu created by <ConvertToJSON()> and saves it as a series of JavaScript files as documented in 
		 * <JavaScript Menu Data>.  This includes the tab information file.  Note that you have to call <ConvertToJSON()> prior to 
		 * calling this function or no data files will be generated.
		 * 
		 * This function will call <AssignDataFiles()> if you did not do it yourself.
		 */
		public void BuildDataFiles ()
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

			// Assign data files in case it hasn't been done yet.
			// DEPENDENCY: This assumes duplicate calls to AssignDataFiles() have no effect.
			AssignDataFiles();

			if (fileRoot != null)
				{  BuildDataFiles(fileRoot);  }

			if (hierarchyRoots != null)
				{  
				foreach (var hierarchyRoot in hierarchyRoots)
					{  BuildDataFiles(hierarchyRoot);  }
				}

			BuildTabDataFile();
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
				var hierarchy = EngineInstance.Hierarchies.FromID(classMenuEntry.HierarchyID);
				var language = EngineInstance.Languages.FromID(classString.LanguageID);

				hashPath = Paths.Class.HashPath(hierarchy, language, classString.Symbol);  
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
		protected JSONMenuEntries.Container ConvertToJSON (MenuEntries.Container menuContainer, bool isRoot = false)
			{
			JSONMenuEntries.Container jsonContainer = (isRoot ? new JSONMenuEntries.RootContainer(menuContainer) :
																						   new JSONMenuEntries.Container(menuContainer));
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
				var hierarchy = EngineInstance.Hierarchies.FromID(languageEntry.HierarchyID);
				var language = languageEntry.WrappedLanguage;

				hashPath = Paths.Class.QualifierHashPath(hierarchy, language, languageEntry.CondensedScopeString);
				}

			else if (menuContainer is MenuEntries.Classes.Scope)
				{
				var scopeEntry = (MenuEntries.Classes.Scope)menuContainer;
				var hierarchy = EngineInstance.Hierarchies.FromID(scopeEntry.HierarchyID);

				if (hierarchy.IsLanguageSpecific)
					{
					// Walk up the tree until you find the language
					MenuEntries.Container parentEntry = menuContainer.Parent;

					while (parentEntry != null && (parentEntry is MenuEntries.Classes.Language) == false)
						{  parentEntry = parentEntry.Parent;  }

					#if DEBUG
					if (parentEntry == null)
						{  throw new Exception ("Couldn't find a language among the scope \"" + (scopeEntry.Title ?? "") + "\"'s parents when generating JSON.");  }
					#endif

					var languageEntry = (MenuEntries.Classes.Language)parentEntry;
					var language = languageEntry.WrappedLanguage;

					hashPath = Paths.Class.QualifierHashPath(hierarchy, language, scopeEntry.WrappedScopeString);
					}
				else // language-agnostic
					{
					hashPath = Paths.Class.QualifierHashPath(hierarchy, null, scopeEntry.WrappedScopeString);
					}
				}

			// If we're at one of the menu roots
			else if (menuContainer.Parent == null)
				{

				// If we're at the file root
				if (menuContainer.HierarchyID == 0)
					{
					// If we're at the file root container and it wasn't also a file source, it means there are multiple file sources beneath
					// it and thus there is no shared hash path.  "Files:" and "Files2:", etc.
					hashPath = null;
					}

				// if we're at one of the class hierarchy roots
				else
					{
					var hierarchy = EngineInstance.Hierarchies.FromID(menuContainer.HierarchyID);

					if (hierarchy.IsLanguageSpecific)
						{
						// If we're at a root language-specific class container that wasn't also a language, it means there are multiple
						// languages beneath it and thus there is no shared hash path.  "CSharpClass:" and "PerlClass:", etc.
						hashPath = null;
						}
					else // language-agnostic
						{
						// If we're at a root language-agnostic class container that wasn't also a scope, it means there are multiple scopes
						// beneath it.  However, unlike files and language-specific classes, there is still a shared hash path.  "Database:",
						// etc.
						hashPath = Paths.Class.QualifierHashPath(hierarchy, null);
						}
					}
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
		 * Segments the menu into smaller pieces and generates data file names.
		 */
		protected void AssignDataFiles(JSONMenuEntries.RootContainer root)
			{
			int hierarchyID = root.MenuEntry.HierarchyID;

			if (hierarchyID == 0) // file menu
				{  root.DataFileIdentifier = Paths.Menu.FileMenuDataFileIdentifier;  }
			else
				{  root.DataFileIdentifier = Paths.Menu.HierarchyMenuDataFileIdentifier( EngineInstance.Hierarchies.FromID(hierarchyID) );  }

			root.UsedDataFileNumbers = new NumberSet();
			AssignDataFiles(root, root);
			}


		/* Function: AssignDataFiles
		 * Segments the menu into smaller pieces and generates data file names.  The passed container will always be assigned
		 * a data file name, so this can be used recursively.
		 */
		protected void AssignDataFiles(JSONMenuEntries.Container container, JSONMenuEntries.RootContainer root)
			{
			// Generate the data file name and number for this container.

			int fileNumber = root.UsedDataFileNumbers.LowestAvailable;
			root.UsedDataFileNumbers.Add(fileNumber);

			container.DataFileName = Paths.Menu.MenuOutputFile(Target.OutputFolder, root.DataFileIdentifier, fileNumber, fileNameOnly: true);


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
						{  AssignDataFiles(inliningCandidate, root);  }

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
		protected void BuildDataFiles (JSONMenuEntries.RootContainer root)
			{
			#if DEBUG
			if (root.StartsNewDataFile == false)
				{  throw new Exception ("BuildOutput() can only be called on containers with DataFileName set.");  }
			#endif

			Stack<JSONMenuEntries.Container> containersToBuild = new Stack<JSONMenuEntries.Container>();
			containersToBuild.Push(root);

			bool addWhitespace = (EngineInstance.Config.ShrinkFiles == false);

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
			bool addWhitespace = (EngineInstance.Config.ShrinkFiles == false);

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

			// Collect hierarchy information

			List<string> simpleIdentifiers = new List<string>();
			List<JSONMenuEntries.Container> containers = new List<JSONMenuEntries.Container>();
			List<Hierarchies.Hierarchy> hierarchies = new List<Hierarchies.Hierarchy>();

			if (fileRoot != null)
				{
				simpleIdentifiers.Add("File");
				containers.Add(fileRoot);
				hierarchies.Add(null);
				}

			if (hierarchyRoots != null)
				{
				foreach (var hierarchyRoot in hierarchyRoots)
					{
					var hierarchy = EngineInstance.Hierarchies.FromID(hierarchyRoot.MenuEntry.HierarchyID);

					simpleIdentifiers.Add(hierarchy.SimpleIdentifier);
					containers.Add(hierarchyRoot);
					hierarchies.Add(hierarchy);
					}
				}


			// Collect source file home page information;

			Files.File sourceFileHomePage = null;

			if (Target.BuildState.CalculatedHomePage != null &&
				Target.BuildState.CalculatedHomePageIsSourceFile)
				{
				Files.File potentialSourceFileHomePage = EngineInstance.Files.FromPath(Target.BuildState.CalculatedHomePage);

				if (potentialSourceFileHomePage == null)
					{
					throw new Exceptions.UserFriendly(
						Locale.Get("NaturalDocs.Engine", "Error.HomePageSourceFileIsntInSourceFolders(file)", 
										 Target.BuildState.CalculatedHomePage) 
						);
					}
				else if (potentialSourceFileHomePage.Type != Files.FileType.Source)
					{
					throw new Exceptions.UserFriendly(
						Locale.Get("NaturalDocs.Engine", "Error.HomePageIsntASourceFileOrHTML(file)", 
										 Target.BuildState.CalculatedHomePage) 
						);
					}
				else if (!Target.BuildState.SourceFileHasContent(potentialSourceFileHomePage.ID))
					{
					throw new Exceptions.UserFriendly(
						Locale.Get("NaturalDocs.Engine", "Error.HomePageSourceFileDoesntHaveContent(file)", 
										 Target.BuildState.CalculatedHomePage) 
						);
					}
				else
					{
					sourceFileHomePage = potentialSourceFileHomePage;
					}
				}


			// Build the output

			StringBuilder output = new StringBuilder();
			bool addWhitespace = (EngineInstance.Config.ShrinkFiles == false);


			// NDFramePage.OnLocationsLoaded()

			output.Append("NDFramePage.OnLocationsLoaded([");

			if (addWhitespace)
				{  output.Append('\n');  }

			for (int i = 0; i < containers.Count; i++)
				{
				if (addWhitespace)
					{  output.Append(' ', IndentWidth);  }

				AppendLocationEntry(simpleIdentifiers[i], hierarchies[i], output);

				if (i < containers.Count - 1)
					{  output.Append(',');  }

				if (addWhitespace)
					{  output.Append('\n');  }
				}

			if (addWhitespace)
				{  output.Append(' ', IndentWidth);  }

			output.Append(']');

			if (sourceFileHomePage != null)
				{
				var fileSource = EngineInstance.Files.FileSourceOf(sourceFileHomePage);
				var relativePath = fileSource.MakeRelative(sourceFileHomePage.Name);

				output.Append(',');

				if (addWhitespace)
					{  output.Append("\n   ");  }

				output.Append('\"');
				output.Append( Paths.SourceFile.HashPath(fileSource.Number, relativePath) );
				output.Append('\"');
				}

			output.Append(");");

			if (addWhitespace)
				{  output.Append("\n\n");  }


			// NDMenu.OnTabsLoaded()

			output.Append("NDMenu.OnTabsLoaded([");

			if (addWhitespace)
				{  output.Append('\n');  }

			for (int i = 0; i < containers.Count; i++)
				{
				if (addWhitespace)
					{  output.Append(' ', IndentWidth);  }

				AppendTabEntry(simpleIdentifiers[i], containers[i], output);

				if (i < containers.Count - 1)
					{  output.Append(',');  }

				if (addWhitespace)
					{  output.Append('\n');  }
				}

			if (addWhitespace)
				{  output.Append(' ', IndentWidth);  }

			output.Append("]);");


			// Write the output to the file

			WriteTextFile( Paths.Menu.TabOutputFile(Target.OutputFolder), output.ToString() );
			}


		/* Function: AppendLocationEntry
		 * A function which appends a single array entry for <NDFramePage.OnLocationsLoaded()> to the output.
		 */
		protected void AppendLocationEntry (string simpleIdentifier, Hierarchies.Hierarchy hierarchy, StringBuilder output)
			{
			output.Append("[\"");
			output.Append(simpleIdentifier);
			output.Append("\",");

			output.Append('"');
			if (hierarchy == null)
				{  output.StringEscapeAndAppend("files");  }
			else
				{  output.StringEscapeAndAppend( hierarchy.PluralSimpleIdentifier.ToLowerInvariant() );  }
			output.Append("\",");

			if (hierarchy == null)
				{  
				output.Append("0,\"^");
				output.StringEscapeAndAppend(simpleIdentifier);
				output.Append("([0-9]*)$\"");
				}
			else if (hierarchy.IsLanguageSpecific)
				{  
				output.Append("1,\"^([A-Za-z]+)");
				output.StringEscapeAndAppend(simpleIdentifier);
				output.Append("$\"");
				}
			else // language-agnostic
				{  
				output.Append("2,\"^");
				output.StringEscapeAndAppend(simpleIdentifier);
				output.Append("$\"");
				}

			output.Append(']');
			}


		/* Function: AppendTabEntry
		 * A function which appends a single array entry for <NDMenu.OnTabsLoaded()> to the output.
		 */
		protected void AppendTabEntry (string simpleIdentifier, JSONMenuEntries.Container container, StringBuilder output)
			{
			output.Append("[\"");
			output.Append(simpleIdentifier);
			output.Append("\",");

			var condensedTitles = (container.MenuEntry as MenuEntries.Container).CondensedTitles;

			if (condensedTitles == null)
				{
				output.Append('"');
				output.StringEscapeAndAppend( container.MenuEntry.Title.ToHTML() );
				output.Append('"');
				}
			else
				{
				output.Append("[\"");
				output.StringEscapeAndAppend( container.MenuEntry.Title.ToHTML() );
				output.Append('"');

				foreach (var condensedTitle in condensedTitles)
					{
					output.Append(",\"");
					output.StringEscapeAndAppend( condensedTitle.ToHTML() );
					output.Append('"');
					}

				output.Append(']');
				}

			output.Append(',');

			if (container.HashPath != null)
				{
				output.Append('"');
				output.StringEscapeAndAppend(container.HashPath);
				output.Append('"');
				}
			// Otherwise leave an empty spot before the comma.  We don't have to write out "undefined".

			output.Append(",\"");
			output.StringEscapeAndAppend(container.DataFileName);
			output.Append("\"]");
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: FileRoot
		 * The root container of all file-based menu entries, or null if none.
		 */
		public JSONMenuEntries.RootContainer FileRoot
			{
			get
				{  return fileRoot;  }
			}
			

		/* Property HierarchyRoots
		 * The root container for each hierarchy menu, or null if there are none.  There will be one for each hierarchy ID in use.
		 */
		public IList<JSONMenuEntries.RootContainer> HierarchyRoots
			{
			get
				{  return hierarchyRoots;  }
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


		/* var: fileRoot
		 * The root container of all file-based menu entries, or null if there are no files.
		 */
		protected JSONMenuEntries.RootContainer fileRoot;

		/* var: hierarchyRoots
		 * The root container for each hierarchy menu, or null if there are none.  There will be one for each hierarchy ID in use.
		 */
		protected List<JSONMenuEntries.RootContainer> hierarchyRoots;

		}
	}

