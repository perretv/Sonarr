using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    public class EpisodeHasAiredSpecificationFixture : CoreTest<EpisodeHasAiredSpecification>
    {
        private RemoteEpisode _remoteEpisode;

        [SetUp]
        public void Setup()
        {
            var series = new Series { Id = 1, Tags = new HashSet<int>() };
            _remoteEpisode = new RemoteEpisode
            {
                Series = series,
                Episodes = new List<Episode>
                {
                    new Episode { SeriesId = series.Id, AirDateUtc = DateTime.UtcNow.AddDays(-1) }
                },
                ParsedEpisodeInfo = new ParsedEpisodeInfo()
            };
        }

        private void WithAirDate(DateTime? airDate)
        {
            _remoteEpisode.Episodes[0].AirDateUtc = airDate;
        }

        private void WithMultipleEpisodes(DateTime? secondAirDate)
        {
            _remoteEpisode.Episodes.Add(new Episode { SeriesId = _remoteEpisode.Series.Id, AirDateUtc = secondAirDate });
        }

        [Test]
        public void should_accept_when_episode_has_aired()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_episode_has_not_aired()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            WithAirDate(DateTime.UtcNow.AddDays(1));
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_reject_when_any_episode_has_not_aired()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            WithMultipleEpisodes(DateTime.UtcNow.AddDays(1));
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_all_episodes_have_aired()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            WithMultipleEpisodes(DateTime.UtcNow.AddDays(-2));
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_reject_when_air_date_missing()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            WithAirDate(null);
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_accept_when_global_setting_disabled()
        {
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(false);
            WithAirDate(DateTime.UtcNow.AddDays(2));
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_accept_when_series_has_bypass_tag()
        {
            var tag = new Tag { Id = 5, Label = "preair-ok" };
            _remoteEpisode.Series.Tags.Add(tag.Id);
            Mocker.GetMock<ITagService>().Setup(s => s.GetTag("preair-ok")).Returns(tag);
            Mocker.GetMock<IConfigService>().SetupGet(s => s.BlockDownloadsBeforeAirdate).Returns(true);
            WithAirDate(DateTime.UtcNow.AddDays(2));
            Subject.IsSatisfiedBy(_remoteEpisode, new()).Accepted.Should().BeTrue();
        }
    }
}
