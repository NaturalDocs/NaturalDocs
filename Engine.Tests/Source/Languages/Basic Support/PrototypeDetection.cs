
// This file is part of Natural Docs, which is Copyright © 2003-2013 Greg Valure.
// Natural Docs is licensed under version 3 of the GNU Affero General Public License (AGPL)
// Refer to License.txt for the complete details


using System;
using NUnit.Framework;


namespace GregValure.NaturalDocs.Engine.Tests.Languages.BasicSupport
	{
	[TestFixture]
	public class PrototypeDetection : Framework.TestTypes.PrototypeDetection
		{

		[Test]
		public void All ()
			{
			TestFolder("Languages/Basic Support/Prototype Detection", "Shared ND Config/Basic Language Support");
			}

		}
	}