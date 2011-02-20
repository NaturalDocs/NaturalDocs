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


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class HTMLFileHierarchy : FileHierarchy
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFileHierarchy () : base ()
			{
			}


		/* Function: AppendJSON
		 * Generates JSON for the root folder of the hierarchy and appends it to the StringBuilder.  If it finds any
		 * <FileHierarchyEntries.HTMLRootFolders> in the tree, they will be added to the passed root folders list
		 * and not included in the JSON.
		 */
		public void AppendJSON (StringBuilder output, List<FileHierarchyEntries.HTMLRootFolder> rootFolders)
			{
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

		}
	}