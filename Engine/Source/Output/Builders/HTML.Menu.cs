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
			JSMenuData jsMenuData = new JSMenuData(this);


			// Build file menu

			lock (accessLock)
				{
				foreach (int fileID in buildState.SourceFilesWithContent)
					{
					if (cancelDelegate())
						{  return;  }

					jsMenuData.AddFile(EngineInstance.Files.FromID(fileID));
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

				jsMenuData.AddClass(classEntry.Value);
				}


			// Condense, sort, and build

			jsMenuData.Condense();
			
			if (cancelDelegate())
				{  return;  }

			jsMenuData.Sort();

			if (cancelDelegate())
				{  return;  }

			StringTable<IDObjects.NumberSet> newMenuDataFiles = jsMenuData.Build();

			if (cancelDelegate())
				{  return;  }


			// Clear out any old menu files that are no longer in use.

			lock (accessLock)
				{
				foreach (var usedMenuDataFileInfo in buildState.UsedMenuDataFiles)
					{
					string type = usedMenuDataFileInfo.Key;
					IDObjects.NumberSet oldNumbers = usedMenuDataFileInfo.Value;
					IDObjects.NumberSet newNumbers = newMenuDataFiles[type];

					if (newNumbers != null)
						{  
						// It's okay that we're altering the original NumberSet, we're throwing it out later.
						oldNumbers.Remove(newNumbers);  
						}

					foreach (int oldNumber in oldNumbers)
						{
						try
							{  System.IO.File.Delete(Output.HTML.Paths.Menu.OutputFile(this.OutputFolder, type, oldNumber));  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}

				buildState.UsedMenuDataFiles = newMenuDataFiles;
				}
			}

		}
	}

