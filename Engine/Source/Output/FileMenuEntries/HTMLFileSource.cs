/* 
 * Class: GregValure.NaturalDocs.Engine.Output.FileMenuEntries.HTMLFileSource
 * ____________________________________________________________________________
 * 
 * Represents a file source in a <HTMLFileMenu>.  Extra fields are added to help output generation.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;


namespace GregValure.NaturalDocs.Engine.Output.FileMenuEntries
	{
	public class HTMLFileSource : FileSource, IHTMLEntry
		{

		// Group: Functions
		// __________________________________________________________________________

		public HTMLFileSource (Files.FileSource newFileSource) : base (newFileSource)
			{
			jsonName = null;
			hashPath = null;
			}

		public void PrepareJSON (Builders.HTML htmlBuilder)
			{
			// JSON Name

			if (!MergeWithRoot)
				{
				jsonName = '"' + WrappedFileSource.Name.ToHTML().StringEscape() + '"';
				}


			// Hash Path

			hashPath = htmlBuilder.Source_OutputFolderHashPath(WrappedFileSource.Number, pathFragment);
			hashPath = '"' + hashPath.StringEscape() + '"';
			}


		public void AppendJSON (StringBuilder output, Stack<FileMenuEntries.HTMLRootFolder> rootFolders)
			{
			#if DONT_SHRINK_FILES
				HTMLFileMenu.AppendJSONIndent(this, output);
			#endif

			// Sanity check
			if (MergeWithRoot && IsDynamicFolder)
				{  throw new Exception("File source can't merge with root and be dynamic.");  }

			output.Append('[');
			
			if (MergeWithRoot)
				{  
				output.Append((int)Builders.HTML.FileMenuEntryType.RootFolder);
				output.Append(',');
				output.Append( (Parent as FileMenuEntries.RootFolder).ID );
				}
			else
				{  
				if (IsDynamicFolder)
					{  output.Append((int)Builders.HTML.FileMenuEntryType.DynamicFolder);  }
				else
					{  output.Append((int)Builders.HTML.FileMenuEntryType.InlineFolder);  }

				output.Append(',');
				output.Append(jsonName);
				}

			output.Append(',');
			output.Append(hashPath);
			output.Append(',');

			if (IsDynamicFolder)
				{  
				output.Append(DynamicMembersID);  
				rootFolders.Push((HTMLRootFolder)Members[0]);
				}
			else // Inline or root
				{
				output.Append('[');

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

				output.Append(']');
				}
				
			output.Append(']');

			#if DONT_SHRINK_FILES
				if (MergeWithRoot)
					{  output.AppendLine();  }
			#endif
			}



		// Group: Properties
		// __________________________________________________________________________

		/* Property: MergeWithRoot
		 * If this is true, this class will generate a root folder JSON tag instead of an inline folder tag.  This is
		 * used when the root folder only has one file source and it's not worth making a separate entry for.
		 */
		public bool MergeWithRoot
			{
			get
				{  return ((Parent as HTMLRootFolder).MergeWithFileSource);  }
			}

		public int JSONTagLength
			{
			get
				{  
				if (!MergeWithRoot)
					{
					// [#,[name],[path],[members]] = 6 for id, commas, and brackets
					// +2 for member brackets, + separating commas
					return jsonName.Length + hashPath.Length + 8 + (Members.Count - 1);
					}
				else
					{
					// [#,#,[path],[members]] = 7 for id, root id, commas, and brackets
					// +2 for member brackets, + separating commas
					return hashPath.Length + 9 + (Members.Count - 1);
					}
				}
			}



		// Group: Variables
		// __________________________________________________________________________

		protected string jsonName;
		protected string hashPath;

		}
	}