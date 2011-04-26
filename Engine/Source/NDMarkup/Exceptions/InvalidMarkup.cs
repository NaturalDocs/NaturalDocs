/* 
 * Class: GregValure.NaturalDocs.Engine.NDMarkup.Exceptions.InvalidMarkup
 * ____________________________________________________________________________
 * 
 * Thrown when something is wrong with a string's <NDMarkup> formatting.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Text;


namespace GregValure.NaturalDocs.Engine.NDMarkup.Exceptions
	{
	public class InvalidMarkup : Exception
		{
		public InvalidMarkup (string content, int index) : base ()
			{
			this.content = content;
			this.index = index;
			}

		public override string Message
			{
			get
				{
				int startContentIndex = index - 20;
				int endContentIndex = index + 20;

				if (startContentIndex < 0)
					{  startContentIndex = 0;  }
				if (endContentIndex > content.Length)
					{  endContentIndex = content.Length;  }

				StringBuilder message = new StringBuilder("Invalid NDMarkup at position in brackets: ");

				if (startContentIndex < index)
					{  message.Append(content, startContentIndex, index - startContentIndex);  }

				message.Append('[');
				message.Append(content[index]);
				message.Append(']');

				if (index + 1 < endContentIndex)
					{  message.Append(content, index + 1, endContentIndex - (index + 1));  }

				return message.ToString();
				}
			}

		protected string content;
		protected int index;
		}
	}