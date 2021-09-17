/* 
 * Class: CodeClear.NaturalDocs.Engine.Config.ProjectInfo
 * ____________________________________________________________________________
 * 
 * A class representing project information.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2021 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using CodeClear.NaturalDocs.Engine.Config;
using CodeClear.NaturalDocs.Engine.Errors;


namespace CodeClear.NaturalDocs.Engine.Config
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
			timestampCode = null;
			styleName = null;
			homePage = null;

			titlePropertyLocation = PropertySource.NotDefined;
			subtitlePropertyLocation = PropertySource.NotDefined;
			copyrightPropertyLocation = PropertySource.NotDefined;
			timestampCodePropertyLocation = PropertySource.NotDefined;
			styleNamePropertyLocation = PropertySource.NotDefined;
			homePagePropertyLocation = PropertySource.NotDefined;
			}

		public ProjectInfo (ProjectInfo toCopy)
			{
			title = toCopy.title;
			subtitle = toCopy.subtitle;
			copyright = toCopy.copyright;
			timestampCode = toCopy.timestampCode;
			styleName = toCopy.styleName;
			homePage = toCopy.homePage;

			titlePropertyLocation = toCopy.titlePropertyLocation;
			subtitlePropertyLocation = toCopy.subtitlePropertyLocation;
			copyrightPropertyLocation = toCopy.copyrightPropertyLocation;
			timestampCodePropertyLocation = toCopy.timestampCodePropertyLocation;
			styleNamePropertyLocation = toCopy.styleNamePropertyLocation;
			homePagePropertyLocation = toCopy.homePagePropertyLocation;
			}
			

		/* Functoin: MakeTimestamp
		 * Generates a time stamp from <TimestampCode> and the current date.  If <TimestampCode> is null this will also
		 * return null.
		 */
		public string MakeTimestamp ()
			{
			return MakeTimestamp(timestampCode);
			}



		// Group: Static Functions
		// __________________________________________________________________________


		/* Function: MakeTimestamp
		 */
		public static string MakeTimestamp (string timestampCode)
			{
			return MakeTimestamp (timestampCode, DateTime.Now.ToLocalTime());
			}


		/* Function: MakeTimestamp
		 */
		public static string MakeTimestamp (string timestampCode, DateTime date)
			{
			if (timestampCode == null)
				{  return null;  }

			if (date == null)
				{  date = DateTime.Now.ToLocalTime();  }

			Tokenization.Tokenizer tokenizer = new Tokenization.Tokenizer(timestampCode);
			Tokenization.TokenIterator tokenIterator = tokenizer.FirstToken;
			System.Text.StringBuilder output = new System.Text.StringBuilder();

			while (tokenIterator.IsInBounds)
				{
				if (tokenIterator.MatchesToken("m", true))
					{  output.Append(date.Month);  }
				else if (tokenIterator.MatchesToken("mm", true))
					{
					if (date.Month < 10)
						{  output.Append('0');  }
					output.Append(date.Month);
					}
				else if (tokenIterator.MatchesToken("mon", true))
					{  output.Append( Locale.Get("NaturalDocs.Engine", "Timestamp.ShortMonth" + date.Month) );  }
				else if (tokenIterator.MatchesToken("month", true))
					{  output.Append( Locale.Get("NaturalDocs.Engine", "Timestamp.Month" + date.Month) );  }
				else if (tokenIterator.MatchesToken("d", true))
					{  output.Append(date.Day);  }
				else if (tokenIterator.MatchesToken("dd", true))
					{
					if (date.Day < 10)
						{  output.Append('0');  }
					output.Append(date.Day);
					}
				else if (tokenIterator.MatchesToken("day", true))
					{
					output.Append(date.Day);
					if (date.Day == 1 || date.Day == 21 || date.Day == 31)
						{  output.Append("st");  }
					else if (date.Day == 2 || date.Day == 22)
						{  output.Append("nd");  }
					else if (date.Day == 3 || date.Day == 23)
						{  output.Append("rd");  }
					else
						{  output.Append("th");  }
					}
				else if (tokenIterator.MatchesToken("yy", true))
					{
					int year = date.Year % 100;
					if (year < 10)
						{  output.Append('0');  }
					output.Append(year);
					}
				else if (tokenIterator.MatchesToken("yyyy", true) || tokenIterator.MatchesToken("year", true))
					{
					output.Append(date.Year);
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
			
		/* Property: TimestampCode
		 * The time stamp code, or null if it's not set.
		 */
		public string TimestampCode
			{
			get
				{  return timestampCode;  }
			set
				{  timestampCode = value;  }
			}

		/* Property: StyleName
		 * The name of the style to be applied, or null if it's not set.
		 */
		public string StyleName
			{
			get
				{  return styleName;  }
			set
				{  styleName = value;  }
			}

		/* Property: HomePage
		 * The file that should serve as the home page, or null if it's not set.
		 */
		public AbsolutePath HomePage
			{
			get
				{  return homePage;  }
			set
				{  homePage = value;  }
			}
			
	
		
		// Group: Property Locations
		// __________________________________________________________________________
		
		
		/* Property: TitlePropertyLocation
		 * Where the <Title> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation TitlePropertyLocation
			{
			get
				{  return titlePropertyLocation;  }
			set
				{  titlePropertyLocation = value;  }
			}
			
		/* Property: SubtitlePropertyLocation
		 * Where the <Subtitle> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation SubtitlePropertyLocation
			{
			get
				{  return subtitlePropertyLocation;  }
			set
				{  subtitlePropertyLocation = value;  }
			}
			
		/* Property: CopyrightPropertyLocation
		 * Where the <Copyright> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation CopyrightPropertyLocation
			{
			get
				{  return copyrightPropertyLocation;  }
			set
				{  copyrightPropertyLocation = value;  }
			}
			
		/* Property: TimestampCodePropertyLocation
		 * Where the <TimestampCode> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation TimestampCodePropertyLocation
			{
			get
				{  return timestampCodePropertyLocation;  }
			set
				{  timestampCodePropertyLocation = value;  }
			}

		/* Property: StyleNamePropertyLocation
		 * Where the <StyleName> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation StyleNamePropertyLocation
			{
			get
				{  return styleNamePropertyLocation;  }
			set
				{  styleNamePropertyLocation = value;  }
			}
			
		/* Property: HomePagePropertyLocation
		 * Where the <HomePage> property is defined, or <PropertySource.NotDefined> if it isn't.
		 */
		public PropertyLocation HomePagePropertyLocation
			{
			get
				{  return homePagePropertyLocation;  }
			set
				{  homePagePropertyLocation = value;  }
			}
			
	
		
		// Group: Variables
		// __________________________________________________________________________
		
		protected string title;
		protected string subtitle;
		protected string copyright;
		protected string timestampCode;
		protected string styleName;
		protected AbsolutePath homePage;

		protected PropertyLocation titlePropertyLocation;
		protected PropertyLocation subtitlePropertyLocation;
		protected PropertyLocation copyrightPropertyLocation;
		protected PropertyLocation timestampCodePropertyLocation;
		protected PropertyLocation styleNamePropertyLocation;
		protected PropertyLocation homePagePropertyLocation;
		
		}
	}