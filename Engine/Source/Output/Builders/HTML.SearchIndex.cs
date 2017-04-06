/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.Builders.HTML
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2017 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using System.Text;
using CodeClear.NaturalDocs.Engine.Collections;
using CodeClear.NaturalDocs.Engine.Topics;


namespace CodeClear.NaturalDocs.Engine.Output.Builders
	{
	public partial class HTML
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildPrefixIndex
		 */
		protected void BuildPrefixIndex (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Components.JSSearchData searchData = new Components.JSSearchData(this);
			searchData.BuildPrefixIndex();
			}


		/* Function: BuildPrefixDataFile
		 */
		protected void BuildPrefixDataFile (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Components.JSSearchData searchData = new Components.JSSearchData(this);
			searchData.BuildPrefixDataFile(prefix, accessor, cancelDelegate);
			}



		// Group: SearchIndex.IChangeWatcher Functions
		// __________________________________________________________________________


		public void OnAddPrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.NeedToBuildPrefixIndex = true;
				buildState.PrefixesToRebuild.Add(prefix);  
				}
			}

		public void OnUpdatePrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.PrefixesToRebuild.Add(prefix);
				}
			}

		public void OnDeletePrefix (string prefix, CodeDB.EventAccessor accessor)
			{
			lock (accessLock)
				{
				buildState.NeedToBuildPrefixIndex = true;
				buildState.PrefixesToRebuild.Add(prefix);  
				}
			}



		// Group: Path Functions
		// __________________________________________________________________________


		/* Property: SearchIndex_DataFolder
		 * The folder that holds all the search index JavaScript files.
		 */
		public Path SearchIndex_DataFolder
			{
			get
				{  return OutputFolder + "/search";  }
			}

		/* Function: SearchIndex_PrefixIndexFileNameOnly
		 * Returns the file name of the prefix index data file.
		 */
		public Path SearchIndex_PrefixIndexFileNameOnly
			{
			get
				{  return "index.js";  }
			}

		/* Function: SearchIndex_PrefixIndexFile
		 * Returns the full path of the prefix index data file.
		 */
		public Path SearchIndex_PrefixIndexFile
			{
			get
				{  return OutputFolder + "/search/index.js";  }
			}

		/* Function: SearchIndex_PrefixDataFileNameOnly
		 * Returns the file name of the search index prefix data file.
		 */
		public Path SearchIndex_PrefixDataFileNameOnly (string prefix)
			{
			#if DEBUG
			if (prefix.Length > 3)
				{  throw new Exception ("SearchIndex_PrefixFileNameOnly assumes the prefix will be 3 characters or less.");  }
			#endif

			if (prefix.Length == 1)
			    {  
				return string.Format("{0:x4}.js", (uint)char.ToLower(prefix[0]));  
				}
			else if (prefix.Length == 2)
			    {  
				return string.Format("{0:x4}{1:x4}.js", 
					(uint)char.ToLower(prefix[0]), 
					(uint)char.ToLower(prefix[1]));  
				}
			else
			    {  
				return string.Format("{0:x4}{1:x4}{2:x4}.js", 
					(uint)char.ToLower(prefix[0]), 
					(uint)char.ToLower(prefix[1]), 
					(uint)char.ToLower(prefix[2]));
				}
			}

		/* Function: SearchIndex_PrefixDataFile
		 * Returns the full path of the search index prefix data file.
		 */
		public Path SearchIndex_PrefixDataFile (string prefix)
			{
			return OutputFolder + "/search/keywords/" + SearchIndex_PrefixDataFileNameOnly(prefix);
			}

		}
	}

