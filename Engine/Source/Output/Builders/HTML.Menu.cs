/* 
 * Class: GregValure.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine.Collections;
using GregValure.NaturalDocs.Engine.Output.Components;
using GregValure.NaturalDocs.Engine.Symbols;


namespace GregValure.NaturalDocs.Engine.Output.Builders
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

			lock (writeLock)
				{
				foreach (int fileID in sourceFilesWithContent)
					{
					if (cancelDelegate())
						{  return;  }

					jsMenuData.AddFile(Instance.Files.FromID(fileID));
					}
				}


			// Build class and database menu

			List<KeyValuePair<int, ClassString>> classes = null;

			accessor.GetReadOnlyLock();
			try
				{  classes = accessor.GetClassesByID(classFilesWithContent, cancelDelegate);  }
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

			lock (writeLock)
				{
				foreach (var usedMenuDataFileInfo in usedMenuDataFiles)
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
							{  System.IO.File.Delete(Menu_DataFile(type, oldNumber));  }
						catch (Exception e)
							{
							if (!(e is System.IO.IOException || e is System.IO.DirectoryNotFoundException))
								{  throw;  }
							}
						}
					}

				usedMenuDataFiles = newMenuDataFiles;
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Property: Menu_DataFolder
		 * The folder that holds all the menu JavaScript files.
		 */
		public Path Menu_DataFolder
			{
			get
				{  return OutputFolder + "/menu";  }
			}

		/* Function: Menu_DataFileNameOnly
		 * Returns the file name of the JavaScript data file with the passed type and ID number.
		 */
		public Path Menu_DataFileNameOnly (string type, int id)
			{
			return type + (id == 1 ? "" : id.ToString()) + ".js";
			}

		/* Function: Menu_DataFile
		 * Returns the path of the JavaScript data file with the passed type and ID number.
		 */
		public Path Menu_DataFile (string type, int id)
			{
			return OutputFolder + "/menu/" + Menu_DataFileNameOnly(type, id);
			}

		}
	}

