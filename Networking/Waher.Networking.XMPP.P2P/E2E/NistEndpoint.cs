﻿using Waher.Networking.XMPP.P2P.SymmetricCiphers;
using Waher.Security.EllipticCurves;

namespace Waher.Networking.XMPP.P2P.E2E
{
	/// <summary>
	/// Abstract base class for NIST Curve endpoints
	/// </summary>
	public abstract class NistEndpoint : EllipticCurveEndpoint
    {
        /// <summary>
        /// Abstract base class for NIST Curve endpoints
        /// </summary>
        /// <param name="Curve">Curve instance</param>
        public NistEndpoint(EllipticCurve Curve)
			: base(Curve, new Aes256())
		{
		}

        /// <summary>
        /// Abstract base class for NIST Curve endpoints
        /// </summary>
        /// <param name="Curve">Curve instance</param>
        /// <param name="SymmetricCipher">Symmetric cipher to use by default.</param>
        public NistEndpoint(EllipticCurve Curve, IE2eSymmetricCipher SymmetricCipher)
            : base(Curve, SymmetricCipher)
        {
        }

        /// <summary>
        /// Abstract base class for NIST Curve endpoints
        /// </summary>
        /// <param name="PublicKey">Remote public key.</param>
        /// <param name="ReferenceCurve">Reference curve</param>
        public NistEndpoint(byte[] PublicKey, EllipticCurve ReferenceCurve)
			: base(PublicKey, ReferenceCurve, new Aes256())
		{
		}

        /// <summary>
        /// Abstract base class for NIST Curve endpoints
        /// </summary>
        /// <param name="PublicKey">Remote public key.</param>
        /// <param name="ReferenceCurve">Reference curve</param>
        /// <param name="SymmetricCipher">Symmetric cipher to use by default.</param>
        public NistEndpoint(byte[] PublicKey, EllipticCurve ReferenceCurve, 
            IE2eSymmetricCipher SymmetricCipher)
            : base(PublicKey, ReferenceCurve, SymmetricCipher)
        {
        }

        /// <summary>
        /// If endpoint is considered safe (i.e. there are no suspected backdoors)
        /// </summary>
        public override bool Safe => false;  // Ref: http://safecurves.cr.yp.to/

        /// <summary>
        /// If implementation is slow, compared to other options.
        /// </summary>
        public override bool Slow => true;

    }
}
