/* 
 * Class: GregValure.NaturalDocs.Engine.Comments.Manager
 * ____________________________________________________________________________
 * 
 * A module which will handle comment parsing.
 * 
 * 
 * Usage:
 * 
 *		- Call <Engine.Instance.Start()> which will start this module.
 */

// This file is part of Natural Docs, which is Copyright © 2003-2011 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;


namespace GregValure.NaturalDocs.Engine.Comments
	{
	public class Manager
		{
		
		// Group: Functions
		// __________________________________________________________________________
		
		
		/* Function: Manager
		 */
		public Manager ()
			{
			lineFinder = new Parsers.LineFinder();
			naturalDocsParser = new Parsers.NaturalDocs();
			}
			
			
		/* Function: Start
		 * 
		 * Dependencies:
		 * 
		 *		- <Config.Manager> and <TopicTypes.Manager> must be started before using the rest of the class.
		 */
		public bool Start (Errors.ErrorList errors)
			{
			return (
				lineFinder.Start(errors) &&
				naturalDocsParser.Start(errors)
				);
			}
			
			
		/* Function: Parse
		 * Parses the passed comment for documentation.  If successful it will return true and add <Topics> to the list.
		 */
		public bool Parse (PossibleDocumentationComment comment, List<Topic> topics)
			{
			// Apply to all comments, not just Natural Docs'.  Javadoc comments may use a left line of stars which would
			// need to be taken out.
			lineFinder.MarkTextBoxes(comment);
			
			if (comment.Type == Type.Plain)
				{
				return naturalDocsParser.Parse(comment, topics, true);
				}
				
			else if (comment.Type == Type.XML)
				{
				// XXX - Parse with XML
				}
				
			else if (comment.Type == Type.Javadoc)
				{
				// If the first line is a header it's still Natural Docs content.
				if (naturalDocsParser.Parse(comment, topics, true) == true)
					{  return true;  }
					
				// XXX - Otherwise if the topic contains @tags parse with Javadoc.
				
				// Otherwise treat as a headerless Natural Docs topic.
				return naturalDocsParser.Parse(comment, topics, false);
				}
				
			return false;
			}
			
			
			
			
		// Group: Properties
		// __________________________________________________________________________
		
		
		/* Property: LineFinder
		 * A reference to <Parsers.LineFinder> so that other parsers may use it to detect horizontal lines.
		 */
		public Parsers.LineFinder LineFinder
			{
			get
				{  return lineFinder;  }
			}
			
			
		// Group: Variables
		// __________________________________________________________________________


		protected Parsers.LineFinder lineFinder;		
		
		protected Parsers.NaturalDocs naturalDocsParser;
		
		}
	}