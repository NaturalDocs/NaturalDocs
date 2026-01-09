/*
 * Class: CodeClear.NaturalDocs.Engine.Output.HTML.Components.RepositoryLink
 * ____________________________________________________________________________
 *
 * A simple class to handle gathering and sorting repository links.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace CodeClear.NaturalDocs.Engine.Output.HTML.Components
	{
	public class RepositoryLink
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Constructor: RepositoryLink
		 */
		public RepositoryLink ()
			{
			Topic = null;
			File = null;
			LineNumber = 0;

			URL = null;
			Title = null;
			}



		// Group: Variables
		// __________________________________________________________________________


		public Engine.Topics.Topic Topic;
		public Engine.Files.File File;
		public int LineNumber;

		public string URL;
		public string Title;

		}
	}
