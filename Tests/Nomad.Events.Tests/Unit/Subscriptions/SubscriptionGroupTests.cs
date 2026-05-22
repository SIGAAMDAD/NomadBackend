/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.Events;
using NSubstitute;
using NUnit.Framework;

namespace Nomad.Events.Tests
{
	[TestFixture]
	[Category("Nomad.Events")]
	[Category("Subscriptions")]
	[Category("Unit")]
	public class SubscriptionGroupTests
	{
		private IGameEventRegistryService _registry;

		[SetUp]
		public void Setup()
		{
			var logger = new MockLogger();
			_registry = new GameEventRegistry(logger);
		}

		[TearDown]
		public void TearDown()
		{
			_registry?.Dispose();
		}

		[Test]
		public void CreateSubscription_WithSameName_ReturnsSameInstance()
		{
			var group1 = _registry.GetGroup("GroupName");
			var group2 = _registry.GetGroup("GroupName");

			Assert.That(group1, Is.SameAs(group2));
		}

		[Test]
		public void CreateSubscription_WithDifferentNames_AreNotSameInstance()
		{
			var group1 = _registry.GetGroup("Group1");
			var group2 = _registry.GetGroup("Group2");

			Assert.That(group1, Is.Not.SameAs(group2));
		}

		[Test]
		public void AddSubscription_AddsSubscriptionToGroup()
		{
			var group = _registry.GetGroup("TestGroup");
			var evt = _registry.GetEvent<EmptyEventArgs>("TestEvent", "TestNamespace");

			group.Add(evt, (in EmptyEventArgs args) => {});

			Assert.That(group.Subscriptions, Has.Count.GreaterThan(0));
		}

		[Test]
		public void UnsubscribeAll_AfterAddingManySubscriptions_RemovesAllSubscriptions()
		{
			var group = _registry.GetGroup("TestGroup");
			var evt = _registry.GetEvent<EmptyEventArgs>("TestEvent", "TestNamespace");

			void Callback1(in EmptyEventArgs args) { }
			void Callback2(in EmptyEventArgs args) { }
			void Callback3(in EmptyEventArgs args) { }

			group.Add(evt, Callback1);
			group.Add(evt, Callback2);
			group.Add(evt, Callback3);

			Assert.That(group.Subscriptions, Has.Count.EqualTo(3));
			group.UnsubscribeAll();
			Assert.That(group.Subscriptions, Has.Count.Zero);
		}
	}
}
