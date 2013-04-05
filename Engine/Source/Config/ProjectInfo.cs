/* 
 * Class: GregValure.NaturalDocs.Engine.Config.ProjectInfo
 * ____________________________________________________________________________
 * 
 * A class representing project information.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Config
	{
	public class ProjectInfo
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		public ProjectInfo ()
			{
			title = null;
			subtitle = null;
			copyright = null;
			timeStampCode = null;
			styleName = null;
			}


		/* Function: CopyUnsetPropertiesFrom
		 * Anything that is undefined will be copied from the passed object.
		 */
		public void CopyUnsetPropertiesFrom (ProjectInfo other)
			{
			if (title == null)
				{  title = other.title;  }
			if (subtitle == null)
				{  subtitle = other.subtitle;  }
			if (copyright == null)
				{  copyright = other.copyright;  }
			if (timeStampCode == null)
				{  timeStampCode = other.timeStampCode;  }
			if (styleName == null)
				{  styleName = other.styleName;  }
			}
			

		/* Property: MakeTimeStamp
		 * Generates a time stamp from <TimeStampCode> and the current date.  If <TimeStampCode> is null this will also
		 * return null.
		 */
		public string MakeTimeStamp ()
			{
			if (timeStampCode == null)
				{  return null;  }

			DateTime now = DateTime.Now.ToLocalTime();
			Tokenization.Tokenizer tokenizer = new Tokenization.Tokenizer(timeStampCode);
			Tokenization.TokenIterator tokenIterator = tokenizer.FirstToken;
			System.Text.StringBuilder output = new System.Text.StringBuilder();

			while (tokenIterator.IsInBounds)
				{
				if (tokenIterator.MatchesToken("m", true))
					{  output.Append(now.Month);  }
				else if (tokenIterator.MatchesToken("mm", true))
					{
					if (now.Month < 10)
						{  output.Append('0');  }
					output.Append(now.Month);
					}
				else if (tokenIterator.MatchesToken("mon", true))
					{  output.Append( Locale.Get("NaturalDocs.Engine", "TimeStamp.ShortMonth" + now.Month) );  }
				else if (tokenIterator.MatchesToken("month", true))
					{  output.Append( Locale.Get("NaturalDocs.Engine", "TimeStamp.Month" + now.Month) );  }
				else if (tokenIterator.MatchesToken("d", true))
					{  output.Append(now.Day);  }
				else if (tokenIterator.MatchesToken("dd", true))
					{
					if (now.Day < 10)
						{  output.Append('0');  }
					output.Append(now.Day);
					}
				else if (tokenIterator.MatchesToken("day", true))
					{
					output.Append(now.Day);
					if (now.Day == 1 || now.Day == 21 || now.Day == 31)
						{  output.Append("st");  }
					else if (now.Day == 2 || now.Day == 22)
						{  output.Append("nd");  }
					else if (now.Day == 3 || now.Day == 23)
						{  output.Append("rd");  }
					else
						{  output.Append("th");  }
					}
				else if (tokenIterator.MatchesToken("yy", true))
					{
					int year = now.Year % 100;
					if (year < 10)
						{  output.Append('0');  }
					output.Append(year);
					}
				else if (tokenIterator.MatchesToken("yyyy", true) || tokenIterator.MatchesToken("year", true))
					{
					output.Append(now.Year);
					}
				else
					{
					tokenIterator.AppendTokenTo(output);
					}

				tokenIterator.Next();
				}

			return output.ToString();
			}


			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: Title
		 * The title of the project, or null if it's not set.
		 */
		public string Title
			{
			get
				{  return title;  }
			set
				{  title = value;  }
			}
			
		/* Property: Subtitle
		 * The project subtitle, or null if it's not set.
		 */
		public string Subtitle
			{
			get
				{  return subtitle;  }
			set
				{  subtitle = value;  }
			}
			
		/* Property: Copyright
		 *	The copyright line, or null if it's not set.
		 */
		public string Copyright
			{
			get
				{  return copyright;  }
			set
				{  copyright = value;  }
			}
			
		/* Property: TimeStampCode
		 * The time stamp code, or null if it's not set.
		 */
		public string TimeStampCode
			{
			get
				{  return timeStampCode;  }
			set
				{  timeStampCode = value;  }
			}
			
		/* Property: StyleName
		 * The style identifier, or null if it's not set.
		 */
		public string StyleName
			{
			get
				{  return styleName;  }
			set
				{  styleName = value;  }
			}
			
	
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected string title;
		protected string subtitle;
		protected string copyright;
		protected string timeStampCode;
		protected string styleName;
		
		}
	}