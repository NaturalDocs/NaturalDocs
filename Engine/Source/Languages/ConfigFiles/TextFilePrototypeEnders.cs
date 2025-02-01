/*
 * Class: CodeClear.NaturalDocs.Engine.Languages.ConfigFiles.TextFilePrototypeEnders
 * ____________________________________________________________________________
 *
 * A class encapsulating information about a group of prototype enders parsed from a <ConfigFiles.TextFile>.
 *
 *
 * Multithreading: Not Thread Safe, Supports Multiple Readers
 *
 *		This object doesn't have any locking built in, and so it is up to the class managing it to provide thread safety if needed.
 *		However, it does support multiple concurrent readers.  This means it can be used in read-only mode with no locking or
 *		in read/write mode with a ReaderWriterLock.
 *
 */

// This file is part of Natural Docs, which is Copyright © 2003-2025 Code Clear LLC.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using System.Collections.Generic;
using CodeClear.NaturalDocs.Engine.Config;


namespace CodeClear.NaturalDocs.Engine.Languages.ConfigFiles
	{
	public class TextFilePrototypeEnders
		{

		// Group: Functions
		// __________________________________________________________________________


		public TextFilePrototypeEnders(string commentType, PropertyLocation propertyLocation)
			{
			this.propertyLocation = propertyLocation;
			this.commentType = commentType;

			this.enderStrings = new List<string>();
			}

		/* Function: Duplicate
		 * Creates an independent copy of the prototype enders and all their attributes.
		 */
		public TextFilePrototypeEnders Duplicate ()
			{
			TextFilePrototypeEnders copy = new TextFilePrototypeEnders(commentType, propertyLocation);

			copy.enderStrings = new List<string>( enderStrings.Count );
			copy.enderStrings.AddRange(enderStrings);

			return copy;
			}

		/* Function: AddEnderString
		 * Adds an ender string to the list.
		 */
		public void AddEnderString (string enderString)
			{
			enderStrings.Add(enderString);
			}

		/* Function: AddEnderStrings
		 * Adds several ender strings to the list.
		 */
		public void AddEnderStrings (IList<string> enderStrings)
			{
			this.enderStrings.AddRange(enderStrings);
			}


		// Group: Properties
		// __________________________________________________________________________

		/* Property: PropertyLocation
		 * The <PropertyLocation> where this statement is defined.
		 */
		public PropertyLocation PropertyLocation
			{
			get
				{  return propertyLocation;  }
			}

		/* Property: CommentType
		 * The name of the comment type associated with these enders.
		 */
		public string CommentType
			{
			get
				{  return commentType;  }
			set
				{  commentType = value;  }
			}

		/* Property: EnderStrings
			* A list of ender strings which may be symbols and/or "\n".
			*/
		public IList<string> EnderStrings
			{
			get
				{  return enderStrings;  }
			}



		// Group: Variables
		// __________________________________________________________________________

		/* var: propertyLocation
		 * The <PropertyLocation> where this statement is defined.
		 */
		protected PropertyLocation propertyLocation;

		/* var: commentType
		 * The name of the comment type associated with these enders.
		 */
		protected string commentType;

		/* var: enderStrings
		 * A list of ender strings which may be symbols and/or "\n".
		 */
		protected List<string> enderStrings;
		}

	}
