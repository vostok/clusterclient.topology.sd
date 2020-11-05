using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Topology.SD.Helpers;
using Vostok.ServiceDiscovery.Abstractions.Models;

namespace Vostok.Clusterclient.Topology.SD.Tests.Helpers
{
    [TestFixture]
    internal class RequestParametersExtensions_Tests
    {
        private readonly Func<TagCollection, bool> filterFunc = collection => collection.ContainsKey("tag1");
        private RequestParameters parameters;

        [SetUp]
        public void Setup()
        {
            parameters = new RequestParameters();
        }

        [Test]
        public void AddTagsFilter_should_set_correct_func()
        {
            parameters = parameters.SetTagsFilter(filterFunc);
            parameters.Properties.ContainsKey(RequestParametersExtensions.RequestParametersTagsFilterKey).Should().BeTrue();
            parameters.Properties[RequestParametersExtensions.RequestParametersTagsFilterKey].Should().BeOfType<Func<TagCollection, bool>>();
        }

        [Test]
        public void AddTagsFilter_should_rewrite_previous_call_func()
        {
            Func<TagCollection, bool> firstFilter = collection => collection.ContainsKey("tag2");
            parameters = parameters.SetTagsFilter(firstFilter);
            parameters = parameters.SetTagsFilter(filterFunc);
            parameters.GetTagsFilter().Should().BeEquivalentTo(filterFunc);
        }

        [Test]
        public void GetTagsFilter_should_return_null_when_func_does_not_set()
        {
            parameters.GetTagsFilter().Should().BeNull();
        }

        [Test]
        public void GetTagsFilter_should_return_correct_func_when_it_was_set()
        {
            parameters = parameters.SetTagsFilter(filterFunc);
            parameters.GetTagsFilter().Should().NotBeNull();
            parameters.GetTagsFilter().Should().BeEquivalentTo(filterFunc);
        }

        [TestCaseSource(nameof(GetTagsFilter_should_return_null_when_value_has_not_valid_type_test_cases))]
        public void GetTagsFilter_should_return_null_when_value_has_not_valid_type(object obj)
        {
            parameters = parameters.WithProperty(RequestParametersExtensions.RequestParametersTagsFilterKey, obj);
            parameters.GetTagsFilter().Should().BeNull();
        }

        private static IEnumerable<object> GetTagsFilter_should_return_null_when_value_has_not_valid_type_test_cases()
        {
            yield return "123";
            yield return 123;
            yield return (Func<bool, TagCollection>)(b => new TagCollection());
            yield return (Func<bool>)(() => true);
            yield return (Func<TagCollection>)(() => new TagCollection());
            yield return (Action<TagCollection>)(collection => {});
            yield return (Action<bool>)(b => {});
            yield return (Action<TagCollection, bool>)((c, b) => {});
        }
    }
}