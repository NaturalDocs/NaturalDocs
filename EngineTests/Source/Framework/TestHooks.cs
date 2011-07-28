/* 
 * Class: GregValure.NaturalDocs.EngineTests.Framework.TestHooks
 * ____________________________________________________________________________
 * 
 * Provides access to the major testing points in the engine.  All functions assume the engine has already been started.
 * 
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using GregValure.NaturalDocs.Engine;
using GregValure.NaturalDocs.Engine.Languages;


namespace GregValure.NaturalDocs.EngineTests.Framework
	{
	public static class TestHooks
		{

		// Group: Functions
		// __________________________________________________________________________


		/* Function: SourceCodeToTopics
		 * Converts the code to <Topics> as if it were from a source file with the passed extension.
		 */
		public static IList<Topic> SourceCodeToTopics (string code, string extension)
			{
			Language language = Engine.Instance.Languages.FromExtension(extension);

			if (language == null)
				{  throw new Exception("Extension " + extension + " did not resolve to a language.");  }

			List<Topic> topics;
			language.Parse(code, -1, Engine.Delegates.NeverCancel, out topics);

			return topics;
			}

		}
	}