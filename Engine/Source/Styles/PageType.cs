/*
 * Enum: CodeClear.NaturalDocs.Engine.Styles.PageType
 * ____________________________________________________________________________
 *
 * Used for specifying the type of HTML page a style property applies to.
 *
 *		All - Applies to all page types.
 *		Frame - Applies to index.html.
 *		Content - Applies to page content for a source file or class.
 *		Home - Applies to the default home page.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2023 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


namespace CodeClear.NaturalDocs.Engine.Styles
	{
	public enum PageType : byte {
  		All = 0,
		Frame = 1,
		Content = 2,
		Home = 3
		}

	}
