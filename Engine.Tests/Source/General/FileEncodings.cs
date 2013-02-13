
// This file is part of Natural Docs, which is Copyright © 2003-2012 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;


namespace GregValure.NaturalDocs.Engine.Tests.General
	{
	[TestFixture]
	public class FileEncodings : Framework.TestTypes.CommentDetection
		{

		[Test]
		public void All ()
			{
			TestFolder("General/File Encodings", "Shared ND Config/Basic Language Support");
			}

		}
	}