/* 
 * Class: GregValure.NaturalDocs.Engine.Exceptions.UserFriendly
 * ____________________________________________________________________________
 * 
 * An exception thrown when the program has to close with an error, but the meaning of the error is
 * very clear without additional technical information.  For example, a configuration file could not be opened.
 * 
 * Requirements:
 * 
 *		- Because the program is in an error state, only <Engine.Locale.SafeGet()> should be used for messages.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


namespace GregValure.NaturalDocs.Engine.Exceptions
	{
	public class UserFriendly : System.Exception
		{
		public UserFriendly (string message) : base (message)
			{  }
		}
	}