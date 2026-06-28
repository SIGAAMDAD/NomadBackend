/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Nomad.Core.Util;
using NUnit.Framework;

namespace Nomad.Core.Tests
{
    [TestFixture]
    [Category("Nomad.Core")]
    [Category("Util")]
    [Category("Unit")]
    [Category("UnitTests")]
    public sealed class ResultTests
    {
        [Test]
        public void Result_SuccessAndFailure_CreateExpectedStates()
        {
            var error = InternalError.Create("broken");
            Result success = Result.Success();
            Result failure = Result.Failure(error);

            success.Deconstruct(out bool successFlag, out IError? successError);
            failure.Deconstruct(out bool failureFlag, out IError? failureError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(success.IsSuccess, Is.True);
                Assert.That(success.IsFailure, Is.False);
                Assert.That(successFlag, Is.True);
                Assert.That(successError, Is.Null);
                Assert.That(failure.IsSuccess, Is.False);
                Assert.That(failure.IsFailure, Is.True);
                Assert.That(failureFlag, Is.False);
                Assert.That(failureError, Is.SameAs(error));
            }
        }
    }
}
