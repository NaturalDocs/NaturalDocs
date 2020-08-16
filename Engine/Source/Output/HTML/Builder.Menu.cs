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


			// Build file menu

			lock (accessLock)
				{
				foreach (int fileID in buildState.SourceFilesWithContent)
					{
					if (cancelDelegate())
						{  return;  }

					menu.AddFile(EngineInstance.Files.FromID(fileID));
					}
				}


			// Build class and database menu

			List<KeyValuePair<int, ClassString>> classes = null;

			accessor.GetReadOnlyLock();
			try
				{  classes = accessor.GetClassesByID(buildState.ClassFilesWithContent, cancelDelegate);  }
			finally
				{  accessor.ReleaseLock();  }

			foreach (KeyValuePair<int, ClassString> classEntry in classes)
				{
				if (cancelDelegate())
					{  return;  }

				menu.AddClass(classEntry.Value);
				}


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

			List<string> stringTypesToDelete = null;

			lock (accessLock)
				{
				foreach (var menuDataFileInfo in buildState.UsedMenuDataFiles)
					{
					var stringType = menuDataFileInfo.Key;
					var fileNumbers = menuDataFileInfo.Value;

					Hierarchy hierarchy;

					if (stringType == "files")
						{  hierarchy = Hierarchy.File;  }
					else if (stringType == "classes")
						{  hierarchy = Hierarchy.Class;  }
					else if (stringType == "database")
						{  hierarchy = Hierarchy.Database;  }
					else
						{  throw new NotImplementedException();  }

					IDObjects.NumberSet newFileNumbers = newMenuDataFiles[hierarchy];

					IDObjects.NumberSet deletedFileNumbers = new IDObjects.NumberSet(fileNumbers);

					if (newFileNumbers != null)
						{  deletedFileNumbers.Remove(newFileNumbers);  }

					foreach (int deletedFileNumber in deletedFileNumbers)
						{
						try
							{  System.IO.File.Delete(Paths.Menu.OutputFile(this.OutputFolder, hierarchy, deletedFileNumber));  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}

					if (newFileNumbers == null)
						{
						if (stringTypesToDelete == null)
							{  stringTypesToDelete = new List<string>();  }

						stringTypesToDelete.Add(stringType);
						}
					else
						{  fileNumbers.SetTo(newFileNumbers);  }
					}

				if (stringTypesToDelete != null)
					{
					foreach (var stringTypeToDelete in stringTypesToDelete)
						{  buildState.UsedMenuDataFiles.Remove(stringTypeToDelete);  }
					}


				// All the key/value pairs that already existed in the build state were updated or deleted.  Now we need to add any that 
				// didn't already exist.

				foreach (var newMenuDataFileInfo in newMenuDataFiles)
					{
					var hierarchy = newMenuDataFileInfo.Key;
					var newFileNumbers = newMenuDataFileInfo.Value;

					string stringType;

					if (hierarchy == Hierarchy.File)
						{  stringType = "files";  }
					else if (hierarchy == Hierarchy.Class)
						{  stringType = "classes";  }
					else if (hierarchy == Hierarchy.Database)
						{  stringType = "database";  }
					else
						{  throw new NotImplementedException();  }

					if (!buildState.UsedMenuDataFiles.ContainsKey(stringType))
						{  buildState.UsedMenuDataFiles.Add(stringType, newFileNumbers);  }
					}
				}
			}

		}
	}

