/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Builder
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.IDObjects;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Builder
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildMenu
		 */
		protected void BuildMenu (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(this);
			Components.Menu menu = new Components.Menu(context);

			buildState.Lock();
			try
				{

				// Build the file menu

				foreach (int fileID in buildState.sourceFilesWithContent)
					{
					menu.AddFile(EngineInstance.Files.FromID(fileID));

					if (cancelDelegate())
						{  return;  }
					}


				// Build the class and database menus

				List<ClassString> classStrings = null;

				accessor.GetReadOnlyLock();
				try
					{  classStrings = accessor.GetClassesByID(buildState.classesWithContent, cancelDelegate);  }
				finally
					{  accessor.ReleaseLock();  }

				foreach (var classString in classStrings)
					{
					menu.AddClass(classString);

					if (cancelDelegate())
						{  return;  }
					}

				}
			finally
				{  buildState.Unlock();  }


			// Condense, sort, and build

			menu.Condense();
			
			if (cancelDelegate())
				{  return;  }

			menu.Sort();

			if (cancelDelegate())
				{  return;  }

			Components.JSONMenu jsonMenu = new Components.JSONMenu(context);
			jsonMenu.ConvertToJSON(menu);

			if (cancelDelegate())
				{  return;  }

			NumberSetTable<Hierarchy> newMenuDataFiles = jsonMenu.BuildDataFiles();

			if (cancelDelegate())
				{  return;  }


			// Clear out any old menu files that are no longer in use.

			buildState.Lock();
			try
				{
				foreach (var hierarchyMenuDataFiles in buildState.usedMenuDataFiles)
					{

					// Compare the old and new numbers to determine which ones to remove

					var hierarchy = hierarchyMenuDataFiles.Key;
					var oldFileNumbers = hierarchyMenuDataFiles.Value;

					NumberSet newFileNumbers = newMenuDataFiles[hierarchy];
					NumberSet removedFileNumbers;

					if (newFileNumbers != null)
						{  
						removedFileNumbers = oldFileNumbers.Duplicate();
						removedFileNumbers.Remove(newFileNumbers);  
						}
					else
						{  removedFileNumbers = oldFileNumbers;  }


					// Remove the data files associated with them

					foreach (int removedFileNumber in removedFileNumbers)
						{
						try
							{  System.IO.File.Delete(Paths.Menu.OutputFile(this.OutputFolder, hierarchy, removedFileNumber));  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}

				buildState.usedMenuDataFiles = newMenuDataFiles;
				}

			finally
				{  buildState.Unlock();  }
			}

		}
	}

