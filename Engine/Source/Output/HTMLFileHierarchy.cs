/* 
 * Class: GregValure.NaturalDocs.Engine.Output.HTMLFileHierarchy
 * ____________________________________________________________________________
 * 
 * A class for generating a tree of all the files to be used in output.  Extra fields are added to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using GregValure.NaturalDocs.Engine.Output.FileHierarchyEntries;


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class HTMLFileHierarchy : FileHierarchy
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFileHierarchy () : base ()
			{
			preparedJSON = false;
			}


		/* Function: PrepareJSON
		 * Performs all calculations related to figuring out the JSON tag lengths of each entry.  This must be called
		 * before using <AppendJSON()>.
		 */
		public void PrepareJSON (Builders.HTML builder)
			{
			ForEach(
				delegate (FileHierarchyEntries.Entry entry)
					{
					(entry as FileHierarchyEntries.IHTMLEntry).PrepareJSON(builder);
					}, 
				FileHierarchy.ForEachMethod.ChildrenFirst);

			preparedJSON = true;
			}

		/* Function: SegmentJSON
		 *	 Goes through the hierarchy and attempts to break the tree into segments that would create JSON data
		 *	 of no more than the passed length.  It will insert <HTMLRootFolders> to make folders dynamic and thus
		 *	 part of another file.  It's possible for a segment to be larger than the passed length if it's unavoidable.
		 *	 
		 * <PrepareJSON()> must be called before this can be called.
		 */
		public void SegmentJSON (int maxLength)
			{
			if (!preparedJSON)
				{  throw new Exception("Must call PrepareJSON before SegmentJSON");  }

			SegmentJSON(maxLength, (HTMLRootFolder)rootFolder);
			}

		/* Function: SegmentJSON
		 * A recursive helper to the public SegmentJSON.
		 */
		protected void SegmentJSON (int maxLength, HTMLRootFolder root)
			{
			List<Container> containersInThisLevel = new List<Container>();
			List<Container> containersInNextLevel = new List<Container>();

			int lengthRemaining = maxLength - root.JSONTagLength;


			// All entries in the root must be added even if it goes over the max.

			foreach (IHTMLEntry entry in root.Members)
				{
				lengthRemaining -= entry.JSONTagLength;

				if (entry is Container)
					{  containersInThisLevel.Add((Container)entry);  }
				}


			// If the root is merged with the file source, all its members have to be added as well.  Otherwise
			// you would have a root with another root's ID for its members, which is invalid.

			if (root.MergeWithFileSource)
				{
				HTMLFileSource fileSource = (HTMLFileSource)containersInThisLevel[0];
				containersInThisLevel.Clear();

				foreach (IHTMLEntry entry in fileSource.Members)
					{
					lengthRemaining -= entry.JSONTagLength;

					if (entry is Container)
						{  containersInThisLevel.Add((Container)entry);  }
					}
				}


			for (;;)
				{
				// While there's space remaining, add any child folders we can, smallest first.

				while (lengthRemaining > 0 && containersInThisLevel.Count > 0)
					{
					int smallestFolderLength = int.MaxValue;
					int smallestFolderIndex = -1;

					for (int i = 0; i < containersInThisLevel.Count; i++)
						{
						int childrenLength = 0;

						foreach (IHTMLEntry entry in containersInThisLevel[i].Members)
							{  childrenLength += entry.JSONTagLength;  }

						if (childrenLength < smallestFolderLength)
							{
							smallestFolderLength = childrenLength;
							smallestFolderIndex = i;
							}
						}

					if (smallestFolderLength <= lengthRemaining)
						{
						foreach (var entry in containersInThisLevel[smallestFolderIndex].Members)
							{
							if (entry is Container)
								{  containersInNextLevel.Add((Container)entry);  }
							}

						lengthRemaining -= smallestFolderLength;
						containersInThisLevel.RemoveAt(smallestFolderIndex);
						}
					else
						{  
						lengthRemaining = 0;
						break;  
						}
					}


				// If we managed to add all the folders and there's still space left over, go for the next level.

				if (lengthRemaining > 0 && containersInNextLevel.Count > 0)
					{
					containersInThisLevel = containersInNextLevel;
					containersInNextLevel = new List<Container>();
					}
				else
					{  break;  }
				}  // loop


			// If there's anything left in foldersInThisLevel or foldersInNextLevel, we ran out of space and they didn't make 
			// the cut.  If there were simply no more subfolders they would both be empty.

			if (containersInNextLevel.Count > 0)
				{  containersInThisLevel.AddRange(containersInNextLevel);  }

			foreach (Container entry in containersInThisLevel)
				{
				HTMLRootFolder newRoot = new HTMLRootFolder();

				newRoot.ID = rootFolderIDs.LowestAvailable;
				rootFolderIDs.Add(newRoot.ID);

				newRoot.Members = entry.Members;
				entry.Members = new List<Entry>(1);
				entry.Members.Add(newRoot);

				SegmentJSON(maxLength, newRoot);
				}
			}

		/* Function: AppendJSON
		 * Generates JSON for the root folder of the hierarchy and appends it to the StringBuilder.  If it finds any
		 * <FileHierarchyEntries.HTMLRootFolders> in the tree, they will be added to the passed root folders list
		 * and not included in the JSON.
		 * 
		 * <PrepareJSON()> must be called before this can be called.
		 */
		public void AppendJSON (StringBuilder output, Stack<FileHierarchyEntries.HTMLRootFolder> rootFolders)
			{
			if (!preparedJSON)
				{  throw new Exception("Must call PrepareJSON before AppendJSON");  }

			(rootFolder as FileHierarchyEntries.HTMLRootFolder).AppendJSON(output, rootFolders);
			}

		#if DONT_SHRINK_FILES
		/* Function: AppendJSONIndent
		 * Appends spaces to the passed StringBuilder based on how many parents it has until the next root folder.
		 * This function only does anything exists if <DONT_SHRINK_FILES> is defined.
		 */
		public static void AppendJSONIndent (FileHierarchyEntries.Entry entry, StringBuilder output)
			{
			while ((entry is FileHierarchyEntries.RootFolder) == false)
				{
				output.Append("   ");
				entry = entry.Parent;
				}
			}
		#endif

		override protected FileHierarchyEntries.RootFolder MakeRootFolderEntry ()
			{
			return new FileHierarchyEntries.HTMLRootFolder();
			}

		override protected FileHierarchyEntries.FileSource MakeFileSourceEntry (Files.FileSource fileSource)
			{
			return new FileHierarchyEntries.HTMLFileSource(fileSource);
			}

		override protected FileHierarchyEntries.Folder MakeFolderEntry (Path pathSegment)
			{
			return new FileHierarchyEntries.HTMLFolder(pathSegment);
			}

		override protected FileHierarchyEntries.File MakeFileEntry (Path filename)
			{
			return new FileHierarchyEntries.HTMLFile(filename);
			}

		protected bool preparedJSON;
		}
	}