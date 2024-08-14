﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.P2P.E2E;

namespace Waher.Networking.XMPP.Test.E2eTests.HttpxTests
{
    [TestClass]
    public class NistP384Tests : XmppHttpxTests
    {
        public override IE2eEndpoint GenerateEndpoint(IE2eSymmetricCipher Cipher)
        {
            return new NistP384Endpoint(Cipher);
        }

    }
}
