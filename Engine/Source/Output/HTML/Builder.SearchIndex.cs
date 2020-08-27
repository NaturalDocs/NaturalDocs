/* 
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Builder
 * ____________________________________________________________________________
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2020 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public partial class Builder
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: BuildSearchPrefixIndex
		 */
		protected void BuildSearchPrefixIndex (CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(this);
			Components.JSONSearchIndex searchData = new Components.JSONSearchIndex(context);
			searchData.BuildIndexDataFile();
			}


		/* Function: BuildSearchPrefixDataFile
		 */
		protected void BuildSearchPrefixDataFile (string prefix, CodeDB.Accessor accessor, CancelDelegate cancelDelegate)
			{
			Context context = new Context(this);
			Components.JSONSearchIndex searchData = new Components.JSONSearchIndex(context);
			searchData.BuildPrefixDataFile(prefix, accessor, cancelDelegate);
			}

		}
	}

