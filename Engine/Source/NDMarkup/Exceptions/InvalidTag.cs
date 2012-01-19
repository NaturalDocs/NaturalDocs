/* 
 * Class: GregValure.NaturalDocs.Engine.NDMarkup.Exceptions.InvalidTag
 * ____________________________________________________________________________
 * 
 * Thrown when something is wrong with an entire <NDMarkup> tag.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.NDMarkup.Exceptions
	{
	public class InvalidTag : Exception
		{
		public InvalidTag (string content, int index, int length) : base ()
			{
			this.content = content;
			this.index = index;
			this.length = length;
			}

		public override string Message
			{
			get
				{
				return "Invalid NDMarkup tag: " + content.Substring(index, length);
				}
			}

		protected string content;
		protected int index;
		protected int length;
		}
	}