/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.Menu
 * ____________________________________________________________________________
 *
 * A class for generating a menu tree.
 *
 * Usage:
 *
 *		- Add files with <AddFile()>.
 *		- Add classes with <AddClass()>.
 *		- If desired, condense unnecessary folder levels with <Condense()>.  You cannot add more entries after calling
 *		  this.
 *		- Sort the members with <Sort()>.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Hierarchies;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class Menu : Component
		{

		// Group: Functions
		// __________________________________________________________________________

		public Menu (Context context) : base (context)
			{
			fileRoot = null;
			hierarchyRoots = null;
			isCondensed = false;
			}


		/* Function: AddFile
		 * Adds a file to the menu tree.
		 */
		public void AddFile (Files.File file)
			{
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a file to the menu once it's been condensed.");  }
			#endif


			// Find which file source owns this file and generate a relative path to it.

			var fileSourceEntry = FindOrCreateRoot(file);
			Path relativePath = fileSourceEntry.WrappedFileSource.MakeRelative(file.FileName);


			// Split off the file name and split the rest into individual folder names.

			string prefix;
			List<string> pathSegments;
			relativePath.Split(out prefix, out pathSegments);

			string fileName = pathSegments[pathSegments.Count - 1];
			pathSegments.RemoveAt(pathSegments.Count - 1);


			// Create the file entry and find out where it goes.  Create new folder levels as necessary.

			MenuEntries.Files.File fileEntry = new MenuEntries.Files.File(file);
			MenuEntries.Container container = fileSourceEntry;

			foreach (string pathSegment in pathSegments)
				{
				Path pathFromFileSource;

				if (container == fileSourceEntry)
					{  pathFromFileSource = pathSegment;  }
				else
					{  pathFromFileSource = (container as MenuEntries.Files.Folder).PathFromFileSource + '/' + pathSegment;  }

				MenuEntries.Files.Folder folderEntry = null;

				foreach (var member in container.Members)
					{
					if (member is MenuEntries.Files.Folder &&
						(member as MenuEntries.Files.Folder).PathFromFileSource == pathFromFileSource)
						{
						folderEntry = (MenuEntries.Files.Folder)member;
						break;
						}
					}

				if (folderEntry == null)
					{
					folderEntry = new MenuEntries.Files.Folder(pathFromFileSource);
					folderEntry.Parent = container;
					container.Members.Add(folderEntry);
					}

				container = folderEntry;
				}

			fileEntry.Parent = container;
			container.Members.Add(fileEntry);
			}


		/* Function: AddClass
		 * Adds a class to the menu tree.
		 */
		public void AddClass (Symbols.ClassString classString)
		   {
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot add a class to the menu once it's been condensed.");  }
			#endif

			var rootEntry = FindOrCreateRoot(classString);

			var hierarchy = EngineInstance.Hierarchies.FromID(classString.HierarchyID);
			bool caseSensitive;

			if (hierarchy.IsLanguageAgnostic)
				{  caseSensitive = hierarchy.IsCaseSensitive;  }
			else
				{  caseSensitive = (rootEntry as MenuEntries.Classes.Language).WrappedLanguage.CaseSensitive;  }


			// Create the class and find out where it goes.  Create new scope containers as necessary.

			MenuEntries.Classes.Class classEntry = new MenuEntries.Classes.Class(classString);
			MenuEntries.Container container = rootEntry;

			string[] classSegments = classString.Symbol.SplitSegments();
			string scopeSoFar = null;

			// We only want to walk through the scope levels so we use length - 1 to ignore the last segment, which is the class name.
			for (int i = 0; i < classSegments.Length - 1; i++)
				{
				string classSegment = classSegments[i];

				if (scopeSoFar == null)
					{  scopeSoFar = classSegment;  }
				else
					{  scopeSoFar += Symbols.SymbolString.SeparatorChar + classSegment;  }

				MenuEntries.Classes.Scope scopeEntry = null;

				foreach (var member in container.Members)
					{
					if (member is MenuEntries.Classes.Scope &&
						string.Compare((member as MenuEntries.Classes.Scope).WrappedScopeString, scopeSoFar, !caseSensitive) == 0)
						{
						scopeEntry = (MenuEntries.Classes.Scope)member;
						break;
						}
					}

				if (scopeEntry == null)
					{
					scopeEntry = new MenuEntries.Classes.Scope(Symbols.SymbolString.FromExportedString(scopeSoFar),
																					   classString.HierarchyID);
					scopeEntry.Parent = container;
					container.Members.Add(scopeEntry);
					}

				container = scopeEntry;
				}

			classEntry.Parent = container;
			container.Members.Add(classEntry);
			}


		/* Function: Condense
		 *	 Removes unnecessary levels in the menu.  Only call this function after everything has been added.
		 */
		public void Condense ()
			{
			if (fileRoot != null)
				{
				fileRoot.Condense();

				// If there's only one file source we can remove the top level container.
				if (fileRoot.Members.Count == 1)
					{
					MenuEntries.Files.FileSource fileSourceEntry = (MenuEntries.Files.FileSource)fileRoot.Members[0];

					// Overwrite the file source name with the tab title, especially since it might not be defined if there was only one.
					// We don't need an unnecessary level for a single file source.
					fileSourceEntry.Title = fileRoot.Title;

					// Get rid of unnecessary levels as there's no point in displaying them.
					fileSourceEntry.CondensedTitles = null;

					fileRoot = fileSourceEntry;
					}
				}

			if (hierarchyRoots != null)
				{
				for (int i = 0; i < hierarchyRoots.Count; i++)
					{
					var hierarchyRoot = hierarchyRoots[i];

					hierarchyRoot.Condense();

					if (EngineInstance.Hierarchies.FromID(hierarchyRoot.HierarchyID).IsLanguageSpecific)
						{
						// If there's only one language we can remove the top level container.
						if (hierarchyRoot.Members.Count == 1)
							{
							MenuEntries.Classes.Language languageEntry = (MenuEntries.Classes.Language)hierarchyRoot.Members[0];

							// We can overwrite the language name with the tab title.  We're not going to preserve an unnecessary level
							// for the language.
							languageEntry.Title = hierarchyRoot.Title;

							// However, we are going to keep CondensedTitles because we want all scope levels to be visible, even if
							// they're empty.

							hierarchyRoots[i] = languageEntry;
							}
						}

					else // language-agnostic
						{
						// If the only top level entry is a scope we can merge it
						if (hierarchyRoot.Members.Count == 1 && hierarchyRoot.Members[0] is MenuEntries.Classes.Scope)
							{
							MenuEntries.Classes.Scope scopeEntry = (MenuEntries.Classes.Scope)hierarchyRoot.Members[0];

							// Move the scope title into CondensedTitles since we want it to be visible.
							if (scopeEntry.CondensedTitles == null)
								{
								scopeEntry.CondensedTitles = new List<string>(1);
								scopeEntry.CondensedTitles.Add(scopeEntry.Title);
								}
							else
								{
								scopeEntry.CondensedTitles.Insert(0, scopeEntry.Title);
								}

							// Now overwrite the original title with the tab title.
							scopeEntry.Title = hierarchyRoot.Title;

							hierarchyRoots[i] = scopeEntry;
							}
						}
					}
			   }

			isCondensed = true;
			}


		/* Function: Sort
		 * Sorts the menu entries.  Should only be done after everything is added to the menu.
		 */
		public void Sort ()
			{
			if (fileRoot != null)
				{  fileRoot.Sort();  }

			if (hierarchyRoots != null)
				{
				foreach (var hierarchyRoot in hierarchyRoots)
					{  hierarchyRoot.Sort();  }
				}
			}


		/* Function: HierarchyRootOf
		 * Returns the root <MenuEntries.Container> for the passed hierarchy ID, or null if there isn't one.
		 */
		public MenuEntries.Container HierarchyRootOf (int hierarchyID)
			{
			if (hierarchyRoots != null)
				{
				foreach (var hierarchyMenu in hierarchyRoots)
					{
					if (hierarchyMenu.HierarchyID == hierarchyID)
						{  return hierarchyMenu;  }
					}
				}

			return null;
			}



		// Group: Support Functions
		// __________________________________________________________________________


		/* Function: FindOrCreateRoot
		 *
		 * Finds and returns the root container associated with the passed file, creating one if it doesn't exist.  This will be a
		 * <MenuEntries.Files.FileSource> for the file's associated source, which is the second level under the hierarchy root.
		 *
		 * This function cannot be used after <Condense()> is called.
		 */
		protected MenuEntries.Files.FileSource FindOrCreateRoot (Files.File file)
			{
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot use FindOrCreateRoot() after the menu has been condensed.");  }
			#endif

			if (fileRoot == null)
				{
				fileRoot = new MenuEntries.Container();
				fileRoot.Title = Engine.Locale.Get("NaturalDocs.Engine", "Menu.Files");
				}

			var fileSource = EngineInstance.Files.FileSourceOf(file);
			MenuEntries.Files.FileSource fileSourceContainer = null;

			foreach (MenuEntries.Files.FileSource member in fileRoot.Members)
				{
				if (member.WrappedFileSource == fileSource)
					{
					fileSourceContainer = member;
					break;
					}
				}

			if (fileSourceContainer == null)
				{
				fileSourceContainer = new MenuEntries.Files.FileSource(fileSource);
				fileSourceContainer.Parent = fileRoot;

				fileRoot.Members.Add(fileSourceContainer);
				}

			return fileSourceContainer;
			}


		/* Function: FindOrCreateRoot
		 *
		 * Finds and returns the root container associated with the passed ClassString, creating one if it doesn't exist.  If the
		 * hierarchy is language-specific this will be a <MenuEntries.Classes.Language> for the associated language, which is
		 * the second level under the hierarchy root.  If it is language-agnostic it will be the root <MenuEntries.Container>
		 * element.
		 *
		 * This function cannot be used after <Condense()> is called.
		 */
		protected MenuEntries.Container FindOrCreateRoot (Symbols.ClassString classString)
			{
			#if DEBUG
			if (isCondensed)
				{  throw new Exception("Cannot use FindOrCreateRoot() after the menu has been condensed.");  }
			#endif

			var hierarchy = EngineInstance.Hierarchies.FromID(classString.HierarchyID);
			var rootContainer = HierarchyRootOf(hierarchy.ID);

			if (rootContainer == null)
				{
				rootContainer = new MenuEntries.Container(hierarchy.ID);

				// See if we can get a title by treating the hierarchy name as a keyword.  This will let people rename the Classes
				// tab from Comments.txt.  If not, use the translation file.
				var hierarchyAsCommentType = EngineInstance.CommentTypes.FromKeyword(hierarchy.Name, 0);

				if (hierarchyAsCommentType != null)
					{  rootContainer.Title  = hierarchyAsCommentType.PluralDisplayName;  }
				else
					{  rootContainer.Title = Engine.Locale.SafeGet("NaturalDocs.Engine", "Menu." + hierarchy.PluralSimpleIdentifier, hierarchy.PluralName);  }

				if (hierarchyRoots == null)
					{
					// Most projects will only have one, but four should cover almost all cases without needing to reallocate.
					hierarchyRoots = new List<MenuEntries.Container>(4);
					}

				hierarchyRoots.Add(rootContainer);
				}

			if (hierarchy.IsLanguageAgnostic)
				{
				return rootContainer;
				}
			else // language-specific
				{
				int languageID = classString.LanguageID;
				MenuEntries.Classes.Language languageContainer = null;

				foreach (MenuEntries.Classes.Language member in rootContainer.Members)
					{
					if (member.WrappedLanguage.ID == languageID)
						{
						languageContainer = member;
						break;
						}
					}

				if (languageContainer == null)
					{
					languageContainer = new MenuEntries.Classes.Language(EngineInstance.Languages.FromID(languageID), hierarchy.ID);
					languageContainer.Parent = rootContainer;
					rootContainer.Members.Add(languageContainer);
					}

				return languageContainer;
				}
			}



		// Group: Properties
		// __________________________________________________________________________


		/* Property: FileRoot
		 * The root <MenuEntries.Container> for the file menu, or null if there isn't one.
		 */
		public MenuEntries.Container FileRoot
			{
			get
				{  return fileRoot;  }
			}

		/* Property: HierarchyRoots
		 * The root <MenuEntries.Container> for each hierarchy menu, or null if there are none.  There will be one for each
		 * hierarchy ID in use.  They are in no particular order.
		 */
		public IList<MenuEntries.Container> HierarchyRoots
			{
			get
				{  return hierarchyRoots;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: fileRoot
		 * The root <MenuEntries.Container> for the file menu, or null if there are no files.
		 */
		protected MenuEntries.Container fileRoot;

		/* var: hierarchyRoots
		 * The root <MenuEntries.Container> for each hierarchy menu, or null if there are none.  There will be one for each
		 * hierarchy ID in use.  They are in no particular order.
		 */
		protected List<MenuEntries.Container> hierarchyRoots;

		/* var: isCondensed
		 * Whether the menu tree has been condensed.
		 */
		protected bool isCondensed;

		}
	}
