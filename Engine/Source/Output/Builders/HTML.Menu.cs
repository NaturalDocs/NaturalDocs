/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Output.Components;
using CodeClear.NaturalDocs.Engine.Symbols;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildMenu
		 */
		protected void BuildMenu (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Output.HTML.Context context = new Output.HTML.Context(this);
			Output.HTML.Components.Menu menu = new Output.HTML.Components.Menu(context);


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

			Output.HTML.Components.JSONMenu jsonMenu = new Output.HTML.Components.JSONMenu(context);
			jsonMenu.ConvertToJSON(menu);

			if (cancelDelegate())
				{  return;  }

			NumberSetTable<Hierarchy> usedMenuDataFiles = jsonMenu.BuildDataFiles();

			if (cancelDelegate())
				{  return;  }


			// Clear out any old menu files that are no longer in use.

			lock (accessLock)
				{
				foreach (var oldMenuDataFiles in buildState.UsedMenuDataFiles)
					{
					string stringType = oldMenuDataFiles.Key;
					Hierarchy hierarchy;

					if (stringType == "files")
						{  hierarchy = Hierarchy.File;  }
					else if (stringType == "classes")
						{  hierarchy = Hierarchy.Class;  }
					else if (stringType == "database")
						{  hierarchy = Hierarchy.Database;  }
					else
						{  throw new NotImplementedException();  }

					IDObjects.NumberSet oldNumbers = oldMenuDataFiles.Value;
					IDObjects.NumberSet newNumbers = usedMenuDataFiles[hierarchy];

					if (newNumbers != null)
						{  
						// It's okay that we're altering the old NumberSet, we're replacing it completely later.
						oldNumbers.Remove(newNumbers);  
						}

					foreach (int oldNumber in oldNumbers)
						{
						try
							{  System.IO.File.Delete(Output.HTML.Paths.Menu.OutputFile(this.OutputFolder, hierarchy, oldNumber));  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}


				// Update the build state.  We need to convert it back to the old format for now.

				StringTable<IDObjects.NumberSet> usedMenuDataFiles_OldFormat = new StringTable<IDObjects.NumberSet>();

				foreach (var keyValuePair in usedMenuDataFiles)
					{
					var hierarchy = keyValuePair.Key;
					var numberSet = keyValuePair.Value;
					string stringType;

					if (hierarchy == Hierarchy.File)
						{  stringType = "files";  }
					else if (hierarchy == Hierarchy.Class)
						{  stringType = "classes";  }
					else if (hierarchy == Hierarchy.Database)
						{  stringType = "database";  }
					else
						{  throw new NotImplementedException();  }

					usedMenuDataFiles_OldFormat[stringType] = numberSet;
					}

				buildState.UsedMenuDataFiles = usedMenuDataFiles_OldFormat;
				}
			}

		}
	}

