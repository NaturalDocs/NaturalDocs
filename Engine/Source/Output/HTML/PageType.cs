/*
 * Enum: CodeClear.NaturalDocs.Engine.Output.HTML.PageType
 * ____________________________________________________________________________
 *
 * Used for specifying the type of page something applies to.
 *
 *		All - Applies to all page types.
 *		Frame - Applies to index.html.
 *		Content - Applies to page content for a source file or class.
 *		Home - Applies to the default home page.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2026 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


namespace CodeClear.NaturalDocs.Engine.Output.HTML
	{
	public enum PageType : byte {
  		All = 0,
		Frame = 1,
		Content = 2,
		Home = 3
		// DEPENDENCY: PageTypes.All and AllNames depend on the integer values and order
		}

	}
