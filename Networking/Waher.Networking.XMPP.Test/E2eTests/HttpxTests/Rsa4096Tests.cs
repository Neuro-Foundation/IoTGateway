﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.P2P.E2E;

namespace Waher.Networking.XMPP.Test.E2eTests.HttpxTests
{
    [TestClass]
    public class Rsa4096Tests : XmppHttpxTests
    {
        public override IE2eEndpoint GenerateEndpoint(IE2eSymmetricCipher Cipher)
        {
            return new RsaEndpoint(4096, Cipher);
        }

    }
}
