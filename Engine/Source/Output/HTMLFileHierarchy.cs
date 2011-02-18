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


namespace GregValure.NaturalDocs.Engine.Output
	{
	public class HTMLFileHierarchy : FileHierarchy
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFileHierarchy () : base ()
			{
			}


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