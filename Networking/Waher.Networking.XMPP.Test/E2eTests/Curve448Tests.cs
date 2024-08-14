﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Waher.Networking.XMPP.P2P.E2E;

namespace Waher.Networking.XMPP.Test.E2eTests
{
    [TestClass]
    public class Curve448Tests : XmppE2eTests 
    {
        public override IE2eEndpoint GenerateEndpoint(IE2eSymmetricCipher Cipher)
        {
            return new Curve448Endpoint(Cipher);
        }

    }
}
