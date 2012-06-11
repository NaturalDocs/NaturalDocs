/* 
 * Class: GregValure.NaturalDocs.Engine.Links.UnresolvedTargetTopicID
 * ____________________________________________________________________________
 * 
 * Values for <Link.TargetTopicID> for unresolved links.  Using one of these values allows the reason it's
 * unresolved to be given which aids in processing.  Every value will be a negative number or zero.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;


namespace GregValure.NaturalDocs.Engine.Links
	{
	public static class UnresolvedTargetTopicID
		{

		/* Constant: NewLink
		 * The link was just created and it hasn't been initially resolved yet.
		 */
		public const int NewLink = 0;
		
		/* Constant: NoTarget
		 * We attempted to resolve the link but no suitable target was found.
		 */
		public const int NoTarget = -1;

		/* Constant: TargetDeleted
		 * The link was previously resolved to a <Topic> that has been deleted and the link hasn't been
		 * reresolved yet.
		 */
		public const int TargetDeleted = -2;

		}
	}