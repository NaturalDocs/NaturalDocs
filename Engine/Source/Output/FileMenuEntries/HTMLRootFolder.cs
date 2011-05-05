/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.HTMLRootFolder
 * ____________________________________________________________________________
 * 
 * Represents the root folder in a <HTMLFileMenu>.  This may be the bottom root that contains the
 * entire menu, or additional roots added to create dynamic folders.  Extra fields are added to help 
 * output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	public class HTMLRootFolder : RootFolder, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLRootFolder () : base ()
			{
			hashPath = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			// The hash path is only relevant for root folders used to create dynamic folders.  It will remain null for the
			// bottom root folder.
			if (Parent != null)
				{
				Path path = new Path();
				Entry parent = Parent;

				// Build the full path by walking all the way down to the file source.  We have to handle both folders and root folder 
				// entries added to make dynamic folders.
				while ((parent is FileSource) == false)
					{
					if (parent is Folder)
						{  path = (parent as Folder).PathFragment + '/' + path;  }

					parent = parent.Parent;
					}

				FileSource fileSource = (FileSource)parent;
			
				if (fileSource.PathFragment != null)
					{  path = fileSource.PathFragment + '/' + path;  }

				hashPath = htmlBuilder.Source_OutputFolderHashPath(fileSource.WrappedFileSource.Number, path);
				hashPath = '"' + hashPath.StringEscape() + '"';
				}
			}


		public void AppendJSON (StringBuilder output, Stack<HTMLRootFolder> rootFolders)
			{
			if (!MergeWithFileSource)
				{
				#if DONT_SHRINK_FILES
					HTMLFileMenu.AppendJSONIndent(this, output);
				#endif

				output.Append('[');
				output.Append((int)Builders.HTML.FileMenuEntryType.RootFolder);
				output.Append(',');
				output.Append(ID);
				output.Append(',');

				if (hashPath != null)
					{  output.Append(hashPath);  }
				else
					{  output.Append("undefined");  }

				output.Append(",[");

				#if DONT_SHRINK_FILES
					output.AppendLine();
				#endif

				for (int i = 0; i < Members.Count; i++)
					{
					if (i > 0)
						{
						output.Append(',');

						#if DONT_SHRINK_FILES
							output.AppendLine();
						#endif
						}

					(Members[i] as IHTMLEntry).AppendJSON(output, rootFolders);
					}

				#if DONT_SHRINK_FILES
					output.AppendLine();
					HTMLFileMenu.AppendJSONIndent(Members[0], output);
				#endif

				output.Append("]]");

				#if DONT_SHRINK_FILES
					output.AppendLine();
				#endif
				}
			else
				{
				(Members[0] as FileMenuEntries.HTMLFileSource).AppendJSON(output, rootFolders);
				}
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: MergeWithFileSource
		 * If true, the folder only contains one entry and it's a <HTMLFileSource>, and thus this entry shouldn't generate
		 * a JSON tag.  The <HTMLFileSource> will so that the properties are combined.
		 */
		public bool MergeWithFileSource
			{
			get
				{  return (Members.Count == 1 && Members[0] is HTMLFileSource);  }
			}

		public int JSONTagLength
			{
			get
				{  
				if (MergeWithFileSource)
					{  return 0;  }
				else
					{
					// [#,#,[path],[members]] = 7 for id, root id, commas, and brackets
					// +2 for member brackets, + separating commas
					int length = 9 + (Members.Count - 1);

					if (hashPath != null)
						{  length += hashPath.Length;  }
					else
						{  
						// +9 for "undefined"
						length += 9;
						}

					return length;
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string hashPath;

		}
	}