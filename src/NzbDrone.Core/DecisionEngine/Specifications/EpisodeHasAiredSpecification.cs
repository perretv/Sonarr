using System;
using System.Linq;
using NLog;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.DecisionEngine.Specifications
{
    public class EpisodeHasAiredSpecification : IDownloadDecisionEngineSpecification
    {
        private readonly IConfigService _configService;
        private readonly ITagService _tagService;
        private readonly Logger _logger;
        private int? _bypassTagId;
        private const string BYPASS_TAG = "preair-ok";

        public EpisodeHasAiredSpecification(IConfigService configService, ITagService tagService, Logger logger)
        {
            _configService = configService;
            _tagService = tagService;
            _logger = logger;
        }

        public SpecificationPriority Priority => SpecificationPriority.Default;
        public RejectionType Type => RejectionType.Permanent;

        private bool HasBypassTag(RemoteEpisode subject)
        {
            if (_bypassTagId == null)
            {
                _bypassTagId = _tagService.GetTag(BYPASS_TAG)?.Id;
            }

            return _bypassTagId.HasValue && subject.Series != null && subject.Series.Tags.Contains(_bypassTagId.Value);
        }

        public DownloadSpecDecision IsSatisfiedBy(RemoteEpisode subject, ReleaseDecisionInformation information)
        {
            if (!_configService.BlockDownloadsBeforeAirdate && !(subject.ParsedEpisodeInfo?.FullSeason ?? false))
            {
                return DownloadSpecDecision.Accept();
            }

            if (HasBypassTag(subject))
            {
                return DownloadSpecDecision.Accept();
            }

            var episodes = subject.Episodes ?? Enumerable.Empty<NzbDrone.Core.Tv.Episode>();
            var unaired = episodes.FirstOrDefault(e => !e.AirDateUtc.HasValue || e.AirDateUtc.Value > DateTime.UtcNow);

            if (unaired != null)
            {
                if (episodes.Count() == 1)
                {
                    var airDate = unaired.AirDateUtc?.ToString("O") ?? "unknown";
                    _logger.Debug("Rejecting {0} because episode hasn't aired yet (airs {1})", subject, airDate);
                    return DownloadSpecDecision.Reject(DownloadRejectionReason.UnairedEpisode, "Episode hasn't aired yet (airs {0}).", airDate);
                }

                _logger.Debug("Rejecting {0} because one or more episodes haven't aired yet", subject);
                return DownloadSpecDecision.Reject(DownloadRejectionReason.UnairedEpisode, "One or more episodes haven't aired yet.");
            }

            return DownloadSpecDecision.Accept();
        }
    }
}
